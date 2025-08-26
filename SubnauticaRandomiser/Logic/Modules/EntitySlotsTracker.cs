using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HarmonyLib;
using HootLib;
using SubnauticaRandomiser.Handlers;
using SubnauticaRandomiser.Interfaces;
using SubnauticaRandomiser.Objects;
using SubnauticaRandomiser.Patches;
using SubnauticaRandomiser.Serialization;
using SubnauticaRandomiser.Serialization.Modules;
using SubnauticaRandomiser.Serialization.Modules.EntitySlots;
using UnityEngine;
using ILogHandler = HootLib.Interfaces.ILogHandler;

namespace SubnauticaRandomiser.Logic.Modules
{
    /// <summary>
    /// A tracker that keeps tabs on the game's spawning behaviours to intervene and guarantee certain spawns if
    /// necessary.
    /// </summary>
    [RequireComponent(typeof(CoreLogic))]
    [DisallowMultipleComponent]
    internal class EntitySlotsTracker : MonoBehaviour, ILogicModule
    {
        private const string _slotsInfoFile = "entitySlots.csv";

        private ILogHandler _log = PrefixLogHandler.Get("[SlotsTracker]");
        private List<SlotCounts> _slotsData;

        public IEnumerable<Task> LoadFiles()
        {
            return new[] { ParseDataFileAsync() };
        }

        public BaseModuleSaveData SetupSaveData()
        {
            GetComponent<CoreLogic>().MainLoopCompleted += OnMainLoopCompleted;
            return new EntitySlotsTrackerSaveData();
        }

        public void ApplySerializedChanges(SaveData saveData) { }
        public void UndoSerializedChanges(SaveData saveData) { }

        public void RandomiseOutOfLoop(IRandomHandler rng, SaveData saveData)
        {
            // At this point the file is guaranteed to have finished loading.
            saveData.GetModuleData<EntitySlotsTrackerSaveData>().SetupSlots(_slotsData);
        }

        public bool RandomiseEntity(IRandomHandler rng, ref LogicEntity entity)
        {
            throw new NotImplementedException();
        }

        private void OnMainLoopCompleted(object sender, EventArgs args)
        {
            if (!Bootstrap.SaveData.TryGetModuleData<FragmentSaveData>(out FragmentSaveData saveData))
            {
                _log.Warn("Tracker was active despite FragmentLogic not doing anything.");
                return;
            }

            // Make a shallow copy of the randomised fragment counts.
            var minimumCounts = new Dictionary<TechType, int>(saveData.NumFragmentsToUnlock);
            // Also populate the counts with fragments that have not had their unlock numbers changed.
            foreach (TechType techType in saveData.SpawnDataDict.Keys)
            {
                // Vanilla fragments do not go above a maximum of 5 scans needed to unlock.
                if (!minimumCounts.ContainsKey(techType))
                    minimumCounts[techType] = 5;
            }

            // Set twice the number required to unlock as a minimum to allow the player to find them comfortably.
            // Keys are put into a list, otherwise C# complains that the collection was modified during access.
            foreach (TechType techType in minimumCounts.Keys.ToList())
            {
                minimumCounts[techType] *= 2;
            }
            
            // TODO: Hook up to actual randomisation results instead of obvious test cases
            var test = new Dictionary<BiomeType, List<(TechType, int)>>
            {
                { BiomeType.CrashHome, new List<(TechType, int)> { (TechType.Aerogel, 10) } },
                { BiomeType.SafeShallows_Grass, new List<(TechType, int)> { (TechType.AluminumOxide, 15) } },
                {
                    BiomeType.SafeShallows_ShellTunnel,
                    new List<(TechType, int)>
                    {
                        (TechType.BaseBioReactorFragment, 5), (TechType.CyclopsEngineFragment, 5)
                    }
                }
            };

            _log.Debug("Setting up tracker entities and spawnables.");
            Bootstrap.SaveData.GetModuleData<EntitySlotsTrackerSaveData>().SetupEntities(test);
            Bootstrap.SaveData.GetModuleData<EntitySlotsTrackerSaveData>().SetupSpawnables();
        }

        public void SetupHarmonyPatches(Harmony harmony, SaveData saveData)
        {
            EntitySlotsTrackerPatcher.Setup(saveData.GetModuleData<EntitySlotsTrackerSaveData>());
            harmony.PatchAll(typeof(EntitySlotsTrackerPatcher));
        }

        private async Task ParseDataFileAsync()
        {
            CsvParser parser = new CsvParser(Path.Combine(Hootils.GetModDirectory(), "Assets", _slotsInfoFile));
            _slotsData = await parser.ParseAllLinesAsync<SlotCounts>();
            _slotsData.ForEach(line =>
                _log.Debug($"Registered biome: {line.Biome}, {line.SmallMax}, {line.CreatureMax}"));
        }
    }
}