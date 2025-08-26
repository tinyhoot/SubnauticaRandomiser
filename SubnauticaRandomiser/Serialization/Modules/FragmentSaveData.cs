using System.Collections.Generic;
using HootLib;
using SubnauticaRandomiser.Handlers;
using SubnauticaRandomiser.Objects;

namespace SubnauticaRandomiser.Serialization.Modules
{
    internal class FragmentSaveData : BaseModuleSaveData
    {
        public LootTable<TechType> FragmentMaterialYield = new LootTable<TechType>();
        public int MaxMaterialYield = 2;
        public Dictionary<TechType, int> NumFragmentsToUnlock = new Dictionary<TechType, int>();
        public Dictionary<TechType, List<SpawnData>> SpawnDataDict = new Dictionary<TechType, List<SpawnData>>();
        
        public bool AddFragmentUnlockNum(TechType type, int number)
        {
            if (NumFragmentsToUnlock.ContainsKey(type))
            {
                PrefixLogHandler.Get("[SaveData]").Warn($"Tried to add duplicate key {type.AsString()} to "
                                                        + $"FragmentNum master dictionary!");
                return false;
            }
            NumFragmentsToUnlock.Add(type, number);
            return true;
        }
        
        public bool AddSpawnData(TechType type, List<SpawnData> data)
        {
            if (SpawnDataDict.ContainsKey(type))
            {
                PrefixLogHandler.Get("[SaveData]").Warn($"Tried to add duplicate key {type.AsString()} to "
                                                        + $"SpawnData master dictionary!");
                return false;
            }
            SpawnDataDict.Add(type, data);
            return true;
        }

        public Dictionary<BiomeType, List<(TechType, int)>> GetMinimumSpawns()
        {
            var data = new Dictionary<BiomeType, List<(TechType, int)>>();
            foreach (var (techType, spawnDatas) in SpawnDataDict)
            {
                foreach (var spawnData in spawnDatas)
                {
                    foreach (var biomeData in spawnData.BiomeDataList)
                    {
                        if (!data.ContainsKey(biomeData.Biome))
                            data.Add(biomeData.Biome, new List<(TechType, int)>());
                        data[biomeData.Biome].Add((techType, biomeData.MinSpawns));
                    }
                }
            }

            return data;
        }
    }
}