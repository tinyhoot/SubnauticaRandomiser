using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HarmonyLib;
using HootLib;
using SubnauticaRandomiser.Configuration;
using SubnauticaRandomiser.Handlers;
using SubnauticaRandomiser.Interfaces;
using SubnauticaRandomiser.Objects;
using SubnauticaRandomiser.Objects.Enums;
using SubnauticaRandomiser.Patches;
using SubnauticaRandomiser.Serialization;
using SubnauticaRandomiser.Serialization.Modules;
using UnityEngine;
using ILogHandler = HootLib.Interfaces.ILogHandler;

namespace SubnauticaRandomiser.Logic.Modules
{
    /// <summary>
    /// Provides a random starting location for the lifepod.
    /// </summary>
    [RequireComponent(typeof(CoreLogic))]
    internal class AlternateStartLogic : MonoBehaviour, ILogicModule
    {
        private CoreLogic _coreLogic;
        private Config _config;
        private ILogHandler _log;
        
        private Dictionary<BiomeRegion, List<float[]>> _alternateStarts;
        private readonly Vector3 _radiationCentre = new Vector3(1038, -3, -163);
        // Actually 950 ingame but this way there's a little buffer.
        private const int _radiationMaxRadius = 1100;

        public void Awake()
        {
            _coreLogic = GetComponent<CoreLogic>();
            _config = _coreLogic._Config;
            _log = PrefixLogHandler.Get("[AS]");
        }

        public IEnumerable<Task> LoadFiles()
        {
            return new[] { ParseDataFileAsync() };
        }

        public BaseModuleSaveData SetupSaveData()
        {
            return new AlternateStartSaveData();
        }

        public void ApplySerializedChanges(SaveData saveData) { }

        public void RandomiseOutOfLoop(IRandomHandler rng, SaveData saveData)
        {
            saveData.GetModuleData<AlternateStartSaveData>().StartPoint = GetRandomStart(rng, _config.SpawnPoint.Value);
        }

        public bool RandomiseEntity(IRandomHandler rng, ref LogicEntity entity)
        {
            // This module does not handle any entity types in the main loop.
            throw new NotImplementedException();
        }

        public void SetupHarmonyPatches(Harmony harmony, SaveData _)
        {
            harmony.PatchAll(typeof(AlternateStart));
        }

        /// <summary>
        /// Convert the config value to a usable biome.
        /// </summary>
        /// <returns>The biome.</returns>
        private BiomeRegion GetBiome(IRandomHandler rng, string startBiome)
        {
            switch (startBiome)
            {
                case "Random":
                    // Only use starts where you can actually reach the ground.
                    return rng.Choice(_alternateStarts.Keys.ToList()
                        .FindAll(biome => !biome.Equals(BiomeRegion.None) && biome.GetAccessibleDepth() <= 100));
                case "Chaotic Random":
                    return rng.Choice(_alternateStarts.Keys);
                case "BulbZone":
                    return BiomeRegion.KooshZone;
                case "Floating Island":
                    return BiomeRegion.FloatingIsland;
                case "Void":
                    return BiomeRegion.None;
            }

            return Hootils.ParseEnum<BiomeRegion>(startBiome);
        }

        /// <summary>
        /// Find a suitable random spawn point for the lifepod.
        /// </summary>
        /// <returns>The new spawn point.</returns>
        /// <exception cref="ArgumentException">Raised if the startBiome is invalid or not in the database.</exception>
        public RandomiserVector GetRandomStart(IRandomHandler rng, string startBiome)
        {
            if (startBiome.StartsWith("Vanilla"))
                return RandomiserVector.ZERO;
            
            BiomeRegion biome = GetBiome(rng, startBiome);
            if (!_alternateStarts.ContainsKey(biome))
            {
                _log.Error("No information found on chosen starting biome " + biome);
                throw new ArgumentException($"Starting biome '{startBiome}' is invalid!");
            }

            // Keep trying for a random spawnpoint in this biome until we get a valid one.
            Vector3 spawn;
            do
            {
                // Choose one of the possible spawning boxes within the biome.
                float[] box = rng.Choice(_alternateStarts[biome]);
                // Choose the specific spawn point within the box.
                int x = rng.Next((int)box[0], (int)box[2] + 1);
                int z = rng.Next((int)box[3], (int)box[1] + 1);
                spawn = new Vector3(x, 0, z);
            } while (!IsValidStart(spawn));

            _log.Debug("Chosen new lifepod spawnpoint at x:" + spawn.x + " y:0" + " z:" + spawn.z);
            return new RandomiserVector(spawn);
        }

        /// <summary>
        /// Checks whether the given spawnpoint is valid considering all config options.
        /// </summary>
        /// <param name="spawn">The chosen location for a possible spawn.</param>
        private bool IsValidStart(Vector3 spawn)
        {
            if (_config.AllowRadiatedStarts.Value)
                return true;
            
            // DistanceSqr returns a squared magnitude since Sqrt() is slow. Use radius^2 to compare.
            return spawn.DistanceSqrXZ(_radiationCentre) > Math.Pow(_radiationMaxRadius, 2);
        }

        /// <summary>
        /// Parse the list of valid start locations from a separate file.
        /// </summary>
        private async Task ParseDataFileAsync()
        {
            _alternateStarts = await CSVReader.ParseAlternateStartAsync(Initialiser._AlternateStartFile);
        }
    }
}