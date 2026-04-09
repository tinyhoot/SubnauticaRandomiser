using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using SubnauticaRandomiser.Interfaces;
using SubnauticaRandomiser.Logic.LogicObjects;
using SubnauticaRandomiser.Patches;
using SubnauticaRandomiser.Serialization;
using SubnauticaRandomiser.Serialization.Modules;
using SubnauticaRandomiser.Serialization.Modules.EntitySlots;

namespace SubnauticaRandomiser.Logic.Modules.Spawnables
{
    internal class FragmentModule : BaseLogicModule
    {
        public override Type HandledEntityType => typeof(LogicFragment);
        public override string LogPrefix => "[Fragment]";

        private List<LogicFragment> _fragments;
        private Dictionary<string, TechType> _spawnableClassIds;

        public override BaseModuleSaveData SetupSaveData()
        {
            return new FragmentSaveData();
        }

        public override void PrepareRandomisation(RandoContext context)
        {
            // Assemble list of classId <-> techtype from existing lootdata. Filter for only those techtypes that
            // are known in entities to prevent interfering with other mods.
            // Filter by only known biomes to avoid grabbing WorldEntities/Environment prefabs which spawn in biomes
            // named after their fragments.
            // Special handling needed for InCrate prefabs, and exosuit.
            // Seaglide, stasis, prop cannon do not have techtypes in WED.
            // > Do not forget to mirror this list in slotstrackersavedata!
            // Patch static vanilla spawns to remove them (exosuit, exosuit claws, that one seamoth).
            // -> Can you edit prefabs to add a "sentry" component to them that immediately deletes the whole thing if
            //    it should not exist?
            
            // Make hashset of all classIds of whatever is touched. Compare vanilla lootdata against it, and wipe
            // the entry if its classId is in that set. Make sure to save that set in SaveData.
            // Do not use nautilus to send changes. It'll just be annoying. LootData resets itself on quit.
            
            _fragments = context.EntityManager.GetAllEntities<LogicFragment>().ToList();
            // Keep track of all classIds that will have their spawns modified by this module.
            _spawnableClassIds = new Dictionary<string, TechType>();
            foreach (var fragment in _fragments)
            {
                foreach (var spawnable in fragment.Dependencies.OfType<LogicSpawnable>())
                {
                    // Whether we associate it with the fragment's or the spawnable's techtype honestly doesn't matter
                    // as long as that choice is consistent with whatever happens in the tracker.
                    spawnable.ClassIds.ForEach(id => _spawnableClassIds.Add(id, fragment.SpawnableTechType));
                }
            }
        }

        public override void PreEntityRandomisation(IRandomHandler rng, SaveData saveData)
        {
            // Relay the classIds we'll be touching to the tracker.
            var trackerData = saveData.GetModuleData<EntitySlotsTrackerSaveData>();
            trackerData.AddClassIds(_spawnableClassIds);
            
            // Randomise required number of scans.
            var fragmentData = saveData.GetModuleData<FragmentSaveData>();
            if (_config.RandomiseTotalScans.Value)
                RandomiseTotalScans(rng, fragmentData);
        }

        /// <summary>
        /// Randomise the number of scans required to unlock a new blueprint.
        /// </summary>
        private void RandomiseTotalScans(IRandomHandler rng, FragmentSaveData saveData)
        {
            foreach (var fragment in _fragments)
            {
                // Account for exclusive upper bound.
                int n = rng.Next(_config.MinFragmentsToUnlock.Value, _config.MaxFragmentsToUnlock.Value + 1);
                // Use this over Add() just in the unlikely case there's a duplicate fragment entity.
                saveData.TotalFragmentsToUnlock[fragment.SpawnableTechType] = n;
                _log.Debug($"Scans required to unlock {fragment.SpawnableTechType.AsString()}: {n}");
            }
        }

        public override void RandomiseEntity(IRandomHandler rng, SaveData saveData, LogicEntity entity)
        {
            // Ensure we don't change anything if the module is only active for e.g. scan number randomisation.
            if (!_config.RandomiseFragments.Value)
                return;
        }

        public override void PostEntityRandomisation(IRandomHandler rng, SaveData saveData)
        {
            base.PostEntityRandomisation(rng, saveData);
            // - Randomise rewards from scanning duplicates.
        }

        public override void RegisterHarmonyPatches(Harmony harmony, SaveData saveData)
        {
            harmony.PatchAll(typeof(FragmentPatcher));
        }

        public override void ApplySerializedState(SaveData saveData)
        {
            // Remove all lootdata with classIds matching _classIds.
            // Add our own lootdata
        }

        public override void UndoSerializedState(SaveData saveData)
        {
            // LootData resets itself on leaving the game. As long as nothing was registered with Nautilus,
            // spawn rates can be left as is.
        }
    }
}