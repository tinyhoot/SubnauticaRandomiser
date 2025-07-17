using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using SubnauticaRandomiser.Serialization.Modules;

namespace SubnauticaRandomiser.Patches
{
    /// <summary>
    /// Responsible for hooking into the game methods needed for the slots tracker to do its work.
    /// </summary>
    [HarmonyPatch]
    internal static class EntitySlotsTrackerPatcher
    {
        private static EntitySlotsTrackerSaveData _saveData;
        
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
        
        // Patches are needed to:
        // - Know which slot exactly was just processed
        // - Influence spawn chance while having slot info
        // - Know what actually ended up spawning and maybe force it to be different
        
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(EntitySlot), nameof(EntitySlot.SpawnVirtualEntities))]
        private static IEnumerable<CodeInstruction> InjectFillerWrapper(IEnumerable<CodeInstruction> instructions)
        {
            CodeMatcher matcher = new CodeMatcher(instructions);
            matcher.MatchForward(true, new CodeMatch(CodeInstruction.Call(typeof(EntitySlot), nameof(EntitySlot.GetFiller))));
            if (matcher.IsInvalid)
            {
                Initialiser._Log.Error("Transpiler failed to find critical instruction in EntitySlot!");
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
                Initialiser._Log.Error("Transpiler failed to find critical instruction in EntitySlotsPlaceholder!");
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
            var filler = EntitySlot.GetFiller(slot);
            // TODO: Override filler if necessary.
            
            _saveData.AddProcessedSlot(slot.biomeType, slot.allowedTypes);
            // TODO: Count up slot for chosen filler.
            return filler;
        }

        private static EntitySlot.Filler GetFillerWrapper(EntitySlotData slotData)
        {
            var filler = LargeWorldStreamer.main.cellManager.GetPrefabForSlot(slotData);
            // TODO: Override filler if necessary.
            
            _saveData.AddProcessedSlot(slotData.biomeType, slotData.allowedTypes);
            // TODO: Count up slot for chosen filler.
            return filler;
        }
    }
}