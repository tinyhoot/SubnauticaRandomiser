﻿using System.Linq;

namespace SubnauticaRandomiser.Objects.Enums
{
    /// <summary>
    /// Defines categories for TechTypes which roughly correspond to their in-game PDA entries.
    /// </summary>
    public enum TechTypeCategory
    {
        None,
        Eggs,
        EggsHatched,
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
        private static readonly TechTypeCategory[] _basePieces = new[]
        {
            TechTypeCategory.BaseBasePieces,
            TechTypeCategory.BaseExternalModules,
            TechTypeCategory.BaseInternalModules,
            TechTypeCategory.BaseInternalPieces,
            TechTypeCategory.BaseGenerators,
        };

        private static readonly TechTypeCategory[] _notCraftables = new[]
        {
            TechTypeCategory.None,
            TechTypeCategory.Eggs,
            TechTypeCategory.EggsHatched,
            TechTypeCategory.Fish,
            TechTypeCategory.Seeds,
            TechTypeCategory.RawMaterials,
            TechTypeCategory.Fragments,
            TechTypeCategory.Databoxes,
        };
        
        private static readonly TechTypeCategory[] _notIngredients = new[]
        {
            TechTypeCategory.BaseBasePieces,
            TechTypeCategory.BaseExternalModules,
            TechTypeCategory.BaseGenerators,
            TechTypeCategory.BaseInternalModules,
            TechTypeCategory.BaseInternalPieces,
            TechTypeCategory.Deployables,
            TechTypeCategory.None,
            TechTypeCategory.Rocket,
            TechTypeCategory.Vehicles,
            TechTypeCategory.Fragments,
            TechTypeCategory.Databoxes,
        };

        /// <summary>
        /// Check whether this category is made up of base pieces.
        /// </summary>
        public static bool IsBasePiece(this TechTypeCategory category)
        {
            return _basePieces.Contains(category);
        }

        /// <summary>
        /// Check whether this category is capable of showing up in the PDA as a craftable item.
        /// </summary>
        public static bool IsCraftable(this TechTypeCategory category)
        {
            return !_notCraftables.Contains(category);
        }

        /// <summary>
        /// Check whether this category is a spawnable databox.
        /// </summary>
        public static bool IsDatabox(this TechTypeCategory category)
        {
            return category.Equals(TechTypeCategory.Databoxes);
        }

        /// <summary>
        /// Check whether this category is a spawnable fragment..
        /// </summary>
        public static bool IsFragment(this TechTypeCategory category)
        {
            return category.Equals(TechTypeCategory.Fragments);
        }

        /// <summary>
        /// Check whether this category can function as an ingredient in recipes.
        /// </summary>
        public static bool IsIngredient(this TechTypeCategory category)
        {
            return !_notIngredients.Contains(category);
        }

        /// <summary>
        /// Check whether this category can be used as an ingredient in recipes, but not itself have a recipe.
        /// </summary>
        public static bool IsRawMaterial(this TechTypeCategory category)
        {
            return category.IsIngredient() && !category.IsCraftable();
        }
    }
}
