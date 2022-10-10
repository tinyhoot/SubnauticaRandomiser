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
    public class SpawnData
    {
        public readonly string ClassId;
        public int AccessibleDepth;                         // Approximate depth needed to encounter this
        public List<RandomiserBiomeData> BiomeDataList;     // All the biomes this can appear in

        public SpawnData(string classId, int depth = 0)
        {
            ClassId = classId;
            AccessibleDepth = depth;
            BiomeDataList = new List<RandomiserBiomeData>();
        }

        /// <summary>
        /// Add BiomeData to the SpawnData. Will throw out any duplicates.
        /// </summary>
        /// <param name="bd">The data to add.</param>
        public void AddBiomeData(RandomiserBiomeData bd)
        {
            if (BiomeDataList.Find(x => x.Biome.Equals(bd.Biome)) != null)
            {
                //LogHandler.Warn($"[SD] Tried to add duplicate biome {bd.Biome.AsString()} to SpawnData ID {ClassId}");
                return;
            }
            BiomeDataList.Add(bd);
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

            foreach (RandomiserBiomeData data in BiomeDataList)
            {
                list.Add(data.GetBaseBiomeData());
            }

            return list;
        }
    }
}
