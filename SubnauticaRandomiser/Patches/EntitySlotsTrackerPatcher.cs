using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using HootLib.Interfaces;
using SubnauticaRandomiser.Handlers;
using SubnauticaRandomiser.Interfaces;
using SubnauticaRandomiser.Serialization.Modules.EntitySlots;

namespace SubnauticaRandomiser.Patches
{
    /// <summary>
    /// Responsible for hooking into the game methods needed for the slots tracker to do its work.
    /// </summary>
    [HarmonyPatch]
    internal static class EntitySlotsTrackerPatcher
    {
        private static EntitySlotsTrackerSaveData _saveData;
        private static ILogHandler _log = PrefixLogHandler.Get("[SlotTrackerPatcher]");
        private static IRandomHandler _rng = new RandomHandler();
        
        public static void Setup(EntitySlotsTrackerSaveData saveData)
        {
            _saveData = saveData;
            Hooking.OnQuitToMainMenu += Teardown;
        }

        private static void Teardown()
        {
            _saveData = null;
            Hooking.OnQuitToMainMenu -= Teardown;
        }
        
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(EntitySlot), nameof(EntitySlot.SpawnVirtualEntities))]
        private static IEnumerable<CodeInstruction> InjectFillerWrapper(IEnumerable<CodeInstruction> instructions)
        {
            CodeMatcher matcher = new CodeMatcher(instructions);
            matcher.MatchForward(true, new CodeMatch(CodeInstruction.Call(typeof(EntitySlot), nameof(EntitySlot.GetFiller))));
            if (matcher.IsInvalid)
            {
                _log.Error("Transpiler failed to find critical instruction in EntitySlot!");
                return matcher.InstructionEnumeration();
            }

            // Replace the original call to the filler with our own wrapper.
            matcher.SetInstruction(CodeInstruction.Call(typeof(EntitySlotsTrackerPatcher), nameof(GetFillerWrapper),
                new[] { typeof(EntitySlot) }));

            return matcher.InstructionEnumeration();
        }
        
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(EntitySlotsPlaceholder), nameof(EntitySlotsPlaceholder.Spawn))]
        private static IEnumerable<CodeInstruction> InjectPlaceholderFillerWrapper(IEnumerable<CodeInstruction> instructions)
        {
            CodeMatcher matcher = new CodeMatcher(instructions);
            matcher.MatchForward(false,
                new CodeMatch(CodeInstruction.LoadField(typeof(LargeWorld), nameof(LargeWorld.main))),
                new CodeMatch(CodeInstruction.LoadField(typeof(LargeWorld), nameof(LargeWorld.streamer))),
                new CodeMatch(CodeInstruction.LoadField(typeof(LargeWorldStreamer),
                    nameof(LargeWorldStreamer.cellManager))),
                new CodeMatch(OpCodes.Ldloc_1),
                new CodeMatch(new CodeInstruction(OpCodes.Callvirt,
                    AccessTools.Method(typeof(CellManager), nameof(CellManager.GetPrefabForSlot)))));
            if (matcher.IsInvalid)
            {
                _log.Error("Transpiler failed to find critical instruction in EntitySlotsPlaceholder!");
                return matcher.InstructionEnumeration();
            }

            // Delete the instructions grabbing the cellmanager.
            matcher.SetAndAdvance(OpCodes.Nop, null);
            matcher.SetAndAdvance(OpCodes.Nop, null);
            matcher.SetAndAdvance(OpCodes.Nop, null);
            // Keep the instruction putting EntitySlotData on the stack, we need that.
            matcher.Advance(1);
            // Replace the original call to the cellmanager with our own wrapper.
            matcher.SetInstruction(CodeInstruction.Call(typeof(EntitySlotsTrackerPatcher), nameof(GetFillerWrapper),
                new[] { typeof(EntitySlotData) }));

            return matcher.InstructionEnumeration();
        }

        private static EntitySlot.Filler GetFillerWrapper(EntitySlot slot)
        {
            // Override the filler if the spawning systems have fallen so far behind that a forced spawn is necessary.
            var filler = GetForcedSpawn(slot.biomeType, SlotCounts.ConvertToPlaceholderType(slot.allowedTypes));
            if (String.IsNullOrEmpty(filler.classId))
                filler = EntitySlot.GetFiller(slot);
            
            _saveData.CountProcessedSlot(slot.biomeType, slot.allowedTypes);
            if (!String.IsNullOrEmpty(filler.classId))
                _saveData.CountSpawnedEntity(slot.biomeType, filler);
            return filler;
        }

        private static EntitySlot.Filler GetFillerWrapper(EntitySlotData slotData)
        {
            var filler = GetForcedSpawn(slotData.biomeType, slotData.allowedTypes);
            if (filler.classId is null)
                filler = LargeWorldStreamer.main.cellManager.GetPrefabForSlot(slotData);
            
            _saveData.CountProcessedSlot(slotData.biomeType, slotData.allowedTypes);
            if (!String.IsNullOrEmpty(filler.classId))
                _saveData.CountSpawnedEntity(slotData.biomeType, filler);
            return filler;
        }

        /// <summary>
        /// Check whether a forced spawn needs to occur for the given slot.
        /// </summary>
        /// <returns>The Filler data for the forced spawn, or default if none is necessary.</returns>
        private static EntitySlot.Filler GetForcedSpawn(BiomeType biome, EntitySlotData.EntitySlotType slotType)
        {
            if (!_saveData.EntityData.TryGetValue(biome, out var entityCounts))
            {
                // This is a biome without any custom spawn data, don't do anything.
                return default;
            }

            // Check whether we need to look out for a forced spawn at all. Don't do unnecessary work if we know
            // the vanilla systems are still doing well.
            float slotProgress = _saveData.SlotsData[biome].GetProgress(slotType);
            // Don't interfere before the vanilla systems have been given a chance at all.
            if (slotProgress < 0.25f)
                return default;
            // Pretend that more slots have spawned than really did so that we reach guaranteed 100% of entities spawned
            // *before* we run out of slots to do so.
            slotProgress *= 1.25f; // Works out to 100% fragment saturation after 80% of slots have been processed.
            
            if (slotProgress < entityCounts.NextCheckThreshold)
                return default;

            var techType = entityCounts.GetForcedSpawn(slotProgress);
            if (techType == TechType.None)
                return default;

            _log.Debug($"Forcing spawn of '{techType}' in biome '{biome}'");
            return new EntitySlot.Filler { classId = _saveData.Spawnables[techType].GetRandomPrefab(_rng), count = 1 };
        }
    }
}