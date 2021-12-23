using System;
namespace SubnauticaRandomiser.RandomiserObjects
{
    public enum ETechTypeCategory
    {
        None,
        Eggs,
        Fish,
        Seeds,
        RawMaterials,
        BasicMaterials,
        AdvancedMaterials,
        Electronics,
        Tools,
        Equipment,
        Tablets,
        Deployables,
        ScannerRoom,
        Vehicles,
        VehicleUpgrades,
        WorkBenchUpgrades,
        Rocket,
        Torpedos,
        BaseBasePieces,
        BaseExternalModules,
        BaseInternalModules,
        BaseInternalPieces,
        BaseGenerators,
        Fragments
    }

    public static class TTCategoryExtensions
    {
        public static bool IsBasePiece(this ETechTypeCategory category)
        {
            switch (category)
            {
                case (ETechTypeCategory.BaseBasePieces):
                case (ETechTypeCategory.BaseExternalModules):
                case (ETechTypeCategory.BaseInternalModules):
                case (ETechTypeCategory.BaseInternalPieces):
                case (ETechTypeCategory.BaseGenerators):
                    return true;
                default:
                    return false;
            }
        }

        public static bool CanHaveRecipe(this ETechTypeCategory category)
        {
            switch (category)
            {
                case (ETechTypeCategory.Eggs):
                case (ETechTypeCategory.Fish):
                case (ETechTypeCategory.Seeds):
                case (ETechTypeCategory.RawMaterials):
                case (ETechTypeCategory.Fragments):
                    return false;
                default:
                    return true;
            }
        }
    }
}
