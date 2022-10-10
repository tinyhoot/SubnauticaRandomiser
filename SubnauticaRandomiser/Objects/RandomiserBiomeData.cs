using System;
using static LootDistributionData;

namespace SubnauticaRandomiser.Objects
{
    /// <summary>
    /// A wrapper for the game's BiomeData class to make it serializable.
    /// </summary>
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
        
        /// <summary>
        /// Get the non-serializable in-game equivalent of this class.
        /// </summary>
        /// <returns>This class, converted to the game's equivalent.</returns>
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
