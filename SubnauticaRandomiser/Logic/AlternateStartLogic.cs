using System;
using System.Collections.Generic;
using System.Linq;
using SubnauticaRandomiser.Interfaces;
using SubnauticaRandomiser.Objects;
using SubnauticaRandomiser.Objects.Enums;

namespace SubnauticaRandomiser.Logic
{
    internal class AlternateStartLogic
    {
        private readonly Dictionary<EBiomeType, List<float[]>> _alternateStarts;
        private readonly RandomiserConfig _config;
        private readonly ILogHandler _log;
        private readonly IRandomHandler _random;

        public AlternateStartLogic(Dictionary<EBiomeType, List<float[]>> alternateStarts, RandomiserConfig config,
            ILogHandler log, IRandomHandler random)
        {
            _alternateStarts = alternateStarts;
            _config = config;
            _log = log;
            _random = random;
        }

        public void Randomise(EntitySerializer serializer)
        {
            serializer.StartPoint = GetRandomStart(_config.sSpawnPoint);
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
    }
}