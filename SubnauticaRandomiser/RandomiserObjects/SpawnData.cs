using System;
using System.Collections.Generic;
using SMLHelper.V2.Handlers;
using static LootDistributionData;

namespace SubnauticaRandomiser.RandomiserObjects
{
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

        public void AddBiomeData(RandomiserBiomeData bd)
        {
            if (BiomeDataList.Find(x => x.Biome.Equals(bd.Biome)) != null)
            {
                LogHandler.Warn("Tried to add duplicate biome " + bd.Biome.AsString() + " to SpawnData ID " + ClassId);
                return;
            }
            BiomeDataList.Add(bd);
        }

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
