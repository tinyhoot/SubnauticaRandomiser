using System.Linq;

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
        Fragments,
        Databoxes,
    }

    public static class TTCategoryExtensions
    {
        private static readonly ETechTypeCategory[] _basePieces = new[]
        {
            ETechTypeCategory.BaseBasePieces,
            ETechTypeCategory.BaseExternalModules,
            ETechTypeCategory.BaseInternalModules,
            ETechTypeCategory.BaseInternalPieces,
            ETechTypeCategory.BaseGenerators,
        };

        private static readonly ETechTypeCategory[] _notCraftables = new[]
        {
            ETechTypeCategory.None,
            ETechTypeCategory.Eggs,
            ETechTypeCategory.Fish,
            ETechTypeCategory.Seeds,
            ETechTypeCategory.RawMaterials,
            ETechTypeCategory.Fragments,
            ETechTypeCategory.Databoxes,
        };
        
        private static readonly ETechTypeCategory[] _notIngredients = new[]
        {
            ETechTypeCategory.BaseBasePieces,
            ETechTypeCategory.BaseExternalModules,
            ETechTypeCategory.BaseGenerators,
            ETechTypeCategory.BaseInternalModules,
            ETechTypeCategory.BaseInternalPieces,
            ETechTypeCategory.Deployables,
            ETechTypeCategory.None,
            ETechTypeCategory.Rocket,
            ETechTypeCategory.Vehicles,
            ETechTypeCategory.Fragments,
            ETechTypeCategory.Databoxes,
        };

        /// <summary>
        /// Check whether this category is made up of base pieces.
        /// </summary>
        public static bool IsBasePiece(this ETechTypeCategory category)
        {
            return _basePieces.Contains(category);
        }

        /// <summary>
        /// Check whether this category is capable of showing up in the PDA as a craftable item.
        /// </summary>
        public static bool IsCraftable(this ETechTypeCategory category)
        {
            return !_notCraftables.Contains(category);
        }

        /// <summary>
        /// Check whether this category can function as an ingredient in recipes.
        /// </summary>
        public static bool IsIngredient(this ETechTypeCategory category)
        {
            return !_notIngredients.Contains(category);
        }
    }
}
