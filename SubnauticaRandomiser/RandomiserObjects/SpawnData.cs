using System;
using System.Collections.Generic;
using SMLHelper.V2.Handlers;
using static LootDistributionData;

namespace SubnauticaRandomiser.RandomiserObjects
{
    [Serializable]
    public class SpawnData
    {
        private readonly string _classId;
        public int AccessibleDepth;             // Approximate depth needed to encounter this
        public List<BiomeData> BiomeData;     // All the biomes this can appear in

        // TODO:
        // - Grab list of all biomes this thing already spawns in from a fresh LootDistributionData
        //   - Edit everything except the biomes we change stuff *to* to 0

        public SpawnData(string classId, int depth = 0)
        {
            _classId = classId;
            AccessibleDepth = depth;
            BiomeData = new List<BiomeData>();
        }

        public void AddBiomeData(BiomeData bd)
        {
            if (BiomeData.Find(x => x.biome.Equals(bd.biome)) != null)
            {
                throw new ArgumentException("Tried to add duplicate biome " + bd.biome.AsString() + " to SpawnData ID " + _classId);
            }
            BiomeData.Add(bd);
        }

        public void EditBiomeData()
        {
            LootDistributionHandler.EditLootDistributionData(_classId, BiomeData);
        }
    }
}
