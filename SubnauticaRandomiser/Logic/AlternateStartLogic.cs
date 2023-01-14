using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HarmonyLib;
using SubnauticaRandomiser.Interfaces;
using SubnauticaRandomiser.Objects;
using SubnauticaRandomiser.Objects.Enums;
using SubnauticaRandomiser.Patches;
using UnityEngine;
using ILogHandler = SubnauticaRandomiser.Interfaces.ILogHandler;

namespace SubnauticaRandomiser.Logic
{
    [RequireComponent(typeof(CoreLogic))]
    internal class AlternateStartLogic : MonoBehaviour, ILogicModule
    {
        private CoreLogic _coreLogic;
        private RandomiserConfig _config;
        private ILogHandler _log;
        private IRandomHandler _random;
        
        private Dictionary<EBiomeType, List<float[]>> _alternateStarts;

        public void Awake()
        {
            _coreLogic = GetComponent<CoreLogic>();
            _config = _coreLogic._Config;
            _log = _coreLogic._Log;
            _random = _coreLogic._Random;
            
            // Parse the list of valid alternate starts from a file.
            ParseDataFileAsync().Start();
        }

        public void Randomise(EntitySerializer serializer)
        {
            serializer.StartPoint = GetRandomStart(_config.sSpawnPoint);
        }

        public LogicEntity RandomiseEntity(LogicEntity entity)
        {
            throw new NotImplementedException();
        }

        public void SetupHarmonyPatches(Harmony harmony)
        {
            harmony.PatchAll(typeof(AlternateStart));
        }

        /// <summary>
        /// Convert the config value to a usable biome.
        /// </summary>
        /// <returns>The biome.</returns>
        private EBiomeType GetBiome(string startBiome)
        {
            switch (startBiome)
            {
                case "Random":
                    // Only use starts where you can actually reach the ground.
                    return _random.Choice(_alternateStarts.Keys.ToList()
                        .FindAll(biome => !biome.Equals(EBiomeType.None) && biome.GetAccessibleDepth() <= 100));
                case "Chaotic Random":
                    return _random.Choice(_alternateStarts.Keys);
                case "BulbZone":
                    return EBiomeType.KooshZone;
                case "Floating Island":
                    return EBiomeType.FloatingIsland;
                case "Void":
                    return EBiomeType.None;
            }

            return EnumHandler.Parse<EBiomeType>(startBiome);
        }

        /// <summary>
        /// Find a suitable random spawn point for the lifepod.
        /// </summary>
        /// <returns>The new spawn point.</returns>
        /// <exception cref="ArgumentException">Raised if the startBiome is invalid or not in the database.</exception>
        public RandomiserVector GetRandomStart(string startBiome)
        {
            if (startBiome.StartsWith("Vanilla"))
                return null;
            
            EBiomeType biome = GetBiome(startBiome);
            if (!_alternateStarts.ContainsKey(biome))
            {
                _log.Error("[AS] No information found on chosen starting biome " + biome);
                throw new ArgumentException($"Starting biome '{startBiome}' is invalid!");
            }

            // Choose one of the possible spawning boxes within the biome.
            float[] box = _random.Choice(_alternateStarts[biome]);
            // Choose the specific spawn point within the box.
            int x = _random.Next((int)box[0], (int)box[2] + 1);
            int z = _random.Next((int)box[3], (int)box[1] + 1);

            _log.Debug("[AS] Chosen new lifepod spawnpoint at x:" + x + " y:0" + " z:" + z);
            return new RandomiserVector(x, 0, z);
        }

        private async Task ParseDataFileAsync()
        {
            _alternateStarts = await CSVReader.ParseAlternateStartAsync(Initialiser._AlternateStartFile);
        }
    }
}