namespace SubnauticaRandomiser.Objects.Enums
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
        /// <summary>
        /// Checks whether this category is made up of base pieces.
        /// </summary>
        /// <returns>True if the category belongs to base pieces, falls otherwise.</returns>
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

        /// <summary>
        /// Checks whether this category is capable of showing up in the PDA as a craftable item.
        /// </summary>
        /// <returns>True if the category can be craftable, false otherwise.</returns>
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
