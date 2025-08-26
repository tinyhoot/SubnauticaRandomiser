using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using static LootDistributionData;

namespace SubnauticaRandomiser.Objects
{
    /// <summary>
    /// A wrapper for the game's SpawnData class to make it serializable.
    /// </summary>
    [Serializable]
    internal class SpawnData
    {
        public readonly string ClassId;
        public List<BiomeDataWrapper> BiomeDataList;     // All the biomes this can appear in

        public SpawnData(string classId)
        {
            ClassId = classId;
            BiomeDataList = new List<BiomeDataWrapper>();
        }

        /// <summary>
        /// Add BiomeData to the SpawnData. Will throw out any duplicates.
        /// </summary>
        /// <param name="bd">The data to add.</param>
        public void AddBiomeData(BiomeDataWrapper bd)
        {
            if (BiomeDataList.Find(x => x.Biome.Equals(bd.Biome)) != null)
            {
                //LogHandler.Warn($"[SD] Tried to add duplicate biome {bd.Biome.AsString()} to SpawnData ID {ClassId}");
                return;
            }
            BiomeDataList.Add(bd);
        }

        public void AddBiomeData(BiomeType biome, int count, float probability, int minSpawns)
        {
            var biomeData = new BiomeDataWrapper
            {
                Biome = biome,
                SpawnCount = count,
                Probability = probability,
                MinSpawns = minSpawns
            };
            AddBiomeData(biomeData);
        }

        /// <summary>
        /// Check whether any of the BiomeData associated with this SpawnData are of the given biome type.
        /// </summary>
        /// <param name="biome">The biome to check for.</param>
        /// <returns>True if any BiomeData modifies the given biome, false if not.</returns>
        public bool ContainsBiome(BiomeType biome)
        {
            return BiomeDataList.Any(b => b.Biome.Equals(biome));
        }

        /// <summary>
        /// Get a list of this object's BiomeData converted to the game's base form.
        /// </summary>
        /// <returns>A list of BiomeData.</returns>
        [NotNull]
        public List<BiomeData> GetBaseBiomeData()
        {
            List<BiomeData> list = new List<BiomeData>();

            foreach (BiomeDataWrapper data in BiomeDataList)
            {
                list.Add(data.GetBaseBiomeData());
            }

            return list;
        }
        
        /// <summary>
        /// A wrapper for the game's BiomeData class to make it serializable.
        /// </summary>
        [Serializable]
        internal class BiomeDataWrapper
        {
            public BiomeType Biome;
            public int SpawnCount;
            public float Probability;
            public int MinSpawns;
        
            /// <summary>
            /// Get the non-serializable in-game equivalent of this class.
            /// </summary>
            /// <returns>This class, converted to the game's equivalent.</returns>
            public BiomeData GetBaseBiomeData()
            {
                BiomeData data = new BiomeData
                {
                    biome = Biome,
                    count = SpawnCount,
                    probability = Probability
                };

                return data;
            }
        }
    }
}
