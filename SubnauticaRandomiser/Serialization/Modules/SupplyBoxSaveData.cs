using SubnauticaRandomiser.Objects;

namespace SubnauticaRandomiser.Serialization.Modules
{
    internal class SupplyBoxSaveData : BaseModuleSaveData
    {
        public LootTable<TechType> LootTable;
    }
}