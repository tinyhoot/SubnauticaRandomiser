using System.Collections.Generic;
using SubnauticaRandomiser.Handlers;
using SubnauticaRandomiser.Objects;

namespace SubnauticaRandomiser.Serialization.Modules
{
    internal class FragmentSaveData : BaseModuleSaveData
    {
        public LootTable<TechType> FragmentMaterialYield = new LootTable<TechType>();
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
    }
}