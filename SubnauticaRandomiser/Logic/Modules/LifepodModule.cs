using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using HootLib;
using Newtonsoft.Json;
using SubnauticaRandomiser.Interfaces;
using SubnauticaRandomiser.Patches;
using SubnauticaRandomiser.Serialization;
using SubnauticaRandomiser.Serialization.Modules;
using UnityEngine;

namespace SubnauticaRandomiser.Logic.Modules
{
    /// <summary>
    /// Provides a random starting location for the lifepod.
    /// </summary>
    internal class LifepodModule : BaseLogicModule
    {
        private const string LifepodStartFile = "lifepodStarts.json";
        
        private readonly Vector3 _radiationCentre = new Vector3(1038, -3, -163);
        // Actually 950 ingame but this way there's a little buffer.
        private const int _radiationMaxRadius = 1100;

        public override Type HandledEntityType => null;
        public override string LogPrefix => "[Lifepod]";

        public static List<LifepodStartData> LoadLifepodFile()
        {
            using var reader = new StreamReader(File.OpenRead(Path.Combine(Hootils.GetAssetHandle(LifepodStartFile))));
            var json = reader.ReadToEnd();
            var data = JsonConvert.DeserializeObject<List<LifepodStartData>>(json);
            data.Insert(0, new LifepodStartData { Name = "Random" });
            data.Insert(1, new LifepodStartData { Name = "Chaotic Random" });
            data.Insert(2, new LifepodStartData { Name = "Vanilla" });
            return data;
        }

        public override BaseModuleSaveData SetupSaveData()
        {
            return new LifepodSaveData();
        }
        
        public override void PreEntityRandomisation(IRandomHandler rng, SaveData saveData)
        {
            foreach (var data in _config.LifepodStarts)
            {
                _log.Debug($"{data.Name} - {data.BoundingBoxes?.Count}");
            }
            
            var idx = _config.LifepodStarts.FindIndex(ld => ld.Name == _config.SpawnPoint.Value);
            // On vanilla, do nothing.
            if (idx == 2)
                return;
            
            var start = GetRandomStart(rng, _config.LifepodStarts, idx);
            saveData.GetModuleData<LifepodSaveData>().StartPoint = start;
        }

        public override void RegisterHarmonyPatches(Harmony harmony, SaveData saveData)
        {
            harmony.PatchAll(typeof(LifepodPatcher));
        }

        public override void ApplySerializedState(SaveData saveData)
        {
        }

        public override void UndoSerializedState(SaveData saveData)
        {
        }

        /// <summary>
        /// Find a suitable random spawn point for the lifepod.
        /// </summary>
        /// <returns>The new spawn point.</returns>
        /// <exception cref="ArgumentException">Raised if the startBiome is invalid or not in the database.</exception>
        private Vector3 GetRandomStart(IRandomHandler rng, List<LifepodStartData> data, int idx)
        {
            var setting = data[idx];

            // Random: Take only locations marked as suitable.
            if (idx == 0)
                setting = rng.Choice(data.Skip(3).Where(d => d.IsValidForRandom).ToArray());
            // Chaotic Random: Take absolutely anything.
            if (idx == 1)
                setting = rng.Choice(data.Skip(3).ToArray());

            // Keep trying for a random spawnpoint in this biome until we get a valid one.
            Vector3 spawn;
            do
            {
                // Choose one of the possible spawning boxes within the biome.
                var box = rng.Choice(setting.BoundingBoxes);
                var rngVec = new Vector3(rng.NextFloat(), rng.NextFloat(), rng.NextFloat());
                rngVec.Scale(box.size);
                spawn = rngVec + box.min;
            } while (!IsValidStart(spawn));

            _log.Debug($"Chosen new lifepod spawnpoint at x:{spawn.x} y:{spawn.y} z:{spawn.z}");
            return spawn;
        }

        /// <summary>
        /// Checks whether the given spawnpoint is valid considering all config options.
        /// </summary>
        /// <param name="spawn">The chosen location for a possible spawn.</param>
        private bool IsValidStart(Vector3 spawn)
        {
            if (_config.AllowIrradiatedStarts.Value)
                return true;
            
            // DistanceSqr returns a squared magnitude since Sqrt() is slow. Use radius^2 to compare.
            return spawn.DistanceSqrXZ(_radiationCentre) > Math.Pow(_radiationMaxRadius, 2);
        }
    }

    [Serializable]
    internal struct LifepodStartData
    {
        public string Name;
        public bool IsValidForRandom;
        public List<Bounds> BoundingBoxes;
    }
}