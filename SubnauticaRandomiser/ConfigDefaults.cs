using System.Collections;
using System.Collections.Generic;

namespace SubnauticaRandomiser
{
    /// <summary>
    /// A class that contains all the default values for most of the config options. Also provides minimum and maximum
    /// bounds for numeric options.
    /// </summary>
    internal static class ConfigDefaults
    {
        private static readonly Dictionary<string, IList> s_defaults = new Dictionary<string, IList>()
        {
            // Key, Default value, Minimum value, Maximum value.
            { "iRandomiserMode", new[] { 0, 0, 1 } },
            { "bUseFish", new[] { true, true, true } },
            { "bUseEggs", new[] { false, false, false } },
            { "bUseSeeds", new[] { true, true, true } },
            { "bRandomiseDataboxes", new[] { true, true, true } },
            { "bRandomiseFragments", new[] { true, true, true } },
            { "bRandomiseNumFragments", new[] { true, true, true } },
            { "bRandomiseDuplicateScans", new[] { true, true ,true } },
            { "bRandomiseRecipes", new[] { true, true, true } },
            { "bVanillaUpgradeChains", new[] { false, false, false } },
            { "bDoBaseTheming", new[] { false, false, false } },
            { "iEquipmentAsIngredients", new[] { 0, 0, 2 } },
            { "iToolsAsIngredients", new[] { 0, 0, 2 } },
            { "iUpgradesAsIngredients", new[] { 1, 0, 2 } },
            { "iMaxAmountPerIngredient", new[] { 5, 1, 10 } },
            { "iMaxIngredientsPerRecipe", new[] { 7, 1, 10 } },
            { "iMaxBiomesPerFragment", new[] { 5, 3, 10 } },
            { "iMaxFragmentsToUnlock", new[] { 5, 1, 30 } },

            // Advanced settings start here.
            { "iDepthSearchTime", new[] { 15, 0, 45 } },
            { "iMaxBasicOutpostSize", new[] { 24, 4, 48 } },
            { "iMaxDepthWithoutVehicle", new[] { 300, 100, 500 } },
            { "iMaxDuplicateScanYield", new[] { 2, 1, 10 } },
            { "iMaxEggsAsSingleIngredient", new[] { 1, 1, 10 } },
            { "iMaxInventorySizePerRecipe", new[] { 24, 4, 100 } },
            { "iMinFragmentsToUnlock", new[] { 2, 1, 30 } },
            { "dPrimaryIngredientValue", new[] { 0.45, 0.0, 1.0 } },
            { "dRecipeValueVariance", new[] { 0.2, 0.0, 1.0 } },
            { "fFragmentSpawnChanceMin", new[] { 0.3f, 0.01f, 10.0f } },
            { "fFragmentSpawnChanceMax", new[] { 0.6f, 0.01f, 10.0f } },
        };

        internal static bool Contains(string key)
        {
            return s_defaults.ContainsKey(key);
        }
        
        internal static object GetDefault(string key)
        {
            if (!s_defaults.ContainsKey(key))
            {
                LogHandler.Warn("Tried to get invalid key from config default dictionary: " + key);
                return null;
            }
            return s_defaults[key][0];
        }

        internal static object GetMax(string key)
        {
            if (!s_defaults.ContainsKey(key))
            {
                LogHandler.Warn("Tried to get invalid key from config default dictionary: " + key);
                return null;
            }
            return s_defaults[key][2];
        }

        internal static object GetMin(string key)
        {
            if (!s_defaults.ContainsKey(key))
            {
                LogHandler.Warn("Tried to get invalid key from config default dictionary: " + key);
                return null;
            }
            return s_defaults[key][1];
        }
    }
}