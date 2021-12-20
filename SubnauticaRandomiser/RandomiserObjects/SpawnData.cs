using System;
using System.Collections.Generic;
using SMLHelper.V2.Handlers;
using static LootDistributionData;

namespace SubnauticaRandomiser.RandomiserObjects
{
    [Serializable]
    public class SpawnData
    {
        private List<BiomeData> _biomeData;     // All the biomes this can appear in
        private readonly string _classId;
        public int AccessibleDepth;             // Approximate depth needed to encounter this

        // TODO:
        // - Grab list of all biomes this thing already spawns in from a fresh LootDistributionData
        //   - Edit everything except the biomes we change stuff *to* to 0

        public SpawnData(string classId, int depth = 0)
        {
            _classId = classId;
            AccessibleDepth = depth;
            _biomeData = new List<BiomeData>();
        }

        public void EditBiomeData()
        {
            LootDistributionHandler.EditLootDistributionData(_classId, _biomeData);
        }
    }
}
