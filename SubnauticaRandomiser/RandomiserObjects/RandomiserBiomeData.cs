using System;
using static LootDistributionData;

namespace SubnauticaRandomiser.RandomiserObjects
{
    // This is a wrapper class around the original BiomeData to make it serializable.

    [Serializable]
    public class RandomiserBiomeData
    {
        public BiomeType Biome;
        public int Count;
        public float Probability;

        public RandomiserBiomeData()
        {
            Biome = default(BiomeType);
            Count = 0;
            Probability = 0f;
        }

        // Get the non-serializable equivalent.
        public BiomeData GetBaseBiomeData()
        {
            BiomeData data = new BiomeData
            {
                biome = Biome,
                count = Count,
                probability = Probability
            };

            return data;
        }
    }
}
