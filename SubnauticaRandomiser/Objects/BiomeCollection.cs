using System;
using System.Collections.Generic;
using SubnauticaRandomiser.Objects.Enums;

namespace SubnauticaRandomiser.Objects
{
    /// <summary>
    /// A class representing a biome as the average player might know it. E.g. BloodKelp being made up of 14 smaller,
    /// more detailed biomes.
    /// </summary>
    internal class BiomeCollection
    {
        public List<Biome> BiomeList = new List<Biome>();
        public int AverageDepth;
        public readonly EBiomeType BiomeType;

        public bool HasBiomes => BiomeList.Count > 0;

        public BiomeCollection(EBiomeType biomeType)
        {
            BiomeType = biomeType;
            AverageDepth = biomeType.GetAccessibleDepth();
        }
        
        /// <summary>
        /// Calculate the average depth of the biomes contained in this collection. Intended as a more fine-tuneable
        /// way of depth control, but largely unused due to the hardcoded depths of EBiomeType.
        /// </summary>
        /// <returns>The average depth.</returns>
        public int CalculateAverageDepth()
        {
            if (BiomeList is null || BiomeList.Count == 0)
            {
                AverageDepth = 0;
                return 0;
            }

            int total = 0;
            foreach (Biome biome in BiomeList)
            {
                total += biome.AverageDepth;
            }

            AverageDepth = total / BiomeList.Count;
            return AverageDepth;
        }
        
        /// <summary>
        /// Add a biome to the collection if it does not already exist.
        /// </summary>
        /// <param name="biome">The biome to add.</param>
        /// <returns>True if successful, false if the collection already contained the biome.</returns>
        public bool Add(Biome biome)
        {
            if (biome is null)
                throw new ArgumentNullException();
            if (BiomeList.Contains(biome) || BiomeList.Exists(b => b.Name.Equals(biome.Name)))
                return false;

            BiomeList.Add(biome);
            return true;
        }
    }
}
