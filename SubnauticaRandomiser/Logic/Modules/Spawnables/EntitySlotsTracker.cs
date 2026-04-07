using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using HarmonyLib;
using HootLib;
using SubnauticaRandomiser.Patches;
using SubnauticaRandomiser.Serialization;
using SubnauticaRandomiser.Serialization.Modules;
using SubnauticaRandomiser.Serialization.Modules.EntitySlots;

namespace SubnauticaRandomiser.Logic.Modules.Spawnables
{
    /// <summary>
    /// A tracker that keeps tabs on the game's spawning behaviours to intervene and guarantee certain spawns if
    /// necessary.
    /// </summary>
    internal class EntitySlotsTracker : BaseLogicModule
    {
        public override Type HandledEntityType => null;
        public override string LogPrefix => "[SlotsTracker]";
        
        private const string SlotsInfoFile = "entitySlots.csv";
        private List<SlotCounts> _slotsData;

        public override IEnumerable<Task> LoadFilesAsync()
        {
            return new[] { ParseDataFileAsync() };
        }

        public override BaseModuleSaveData SetupSaveData()
        {
            var save = new EntitySlotsTrackerSaveData();
            save.SetupSlots(_slotsData);
            return save;
        }
        
        public override void PrepareRandomisation(RandoContext context)
        {
            _monitor.RandomisingComplete += OnRandomisingCompleted;
        }

        public override void RegisterHarmonyPatches(Harmony harmony, SaveData saveData)
        {
            EntitySlotsTrackerPatcher.Setup(saveData.GetModuleData<EntitySlotsTrackerSaveData>());
            harmony.PatchAll(typeof(EntitySlotsTrackerPatcher));
        }

        public override void ApplySerializedState(SaveData saveData)
        {
        }

        public override void UndoSerializedState(SaveData saveData)
        {
        }

        private void OnRandomisingCompleted()
        {
            if (!Bootstrap.SaveData.TryGetModuleData<FragmentSaveData>(out FragmentSaveData fragmentSave))
            {
                _log.Warn("Tracker was active despite FragmentLogic not doing anything.");
                return;
            }

            _log.Debug("Setting up tracker entities and spawnables.");
            var save = Bootstrap.SaveData.GetModuleData<EntitySlotsTrackerSaveData>();
            save.SetupEntities(fragmentSave.GetMinimumSpawns());
            save.SetupSpawnables();
        }

        private async Task ParseDataFileAsync()
        {
            CsvParser parser = new CsvParser(Path.Combine(Hootils.GetModDirectory(), "Assets", SlotsInfoFile));
            _slotsData = await parser.ParseAllLinesAsync<SlotCounts>();
            _slotsData.ForEach(line =>
                _log.Debug($"Registered biome: {line.Biome}, {line.SmallMax}, {line.CreatureMax}"));
        }
    }
}