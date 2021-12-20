using System;
using System.Collections.Generic;

namespace SubnauticaRandomiser.RandomiserObjects
{
    public class BiomeCollection
    {
        public List<Biome> BiomeList = new List<Biome>();
        public int AverageDepth;
        public readonly EBiomeType BiomeType;

        public bool HasBiomes { get { return BiomeList.Count > 0; } }

        public BiomeCollection(EBiomeType biomeType)
        {
            BiomeType = biomeType;
            AverageDepth = biomeType.GetAccessibleDepth();
        }

        // Calculate the average depth of the biomes contained in this collection.
        // Intended as a more fine-tuneable way of depth control, but largely
        // unused due to the hardcoded depths of EBiomeType.
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

        // Ensure that no duplicates can be added to the collection.
        public bool Add(Biome biome)
        {
            if (BiomeList.Contains(biome))
                return false;

            BiomeList.Add(biome);
            return true;
        }
    }
}
