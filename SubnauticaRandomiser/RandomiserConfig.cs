using System;
using SMLHelper.V2.Json;
using SMLHelper.V2.Options.Attributes;
namespace SubnauticaRandomiser
{
    [Menu("Randomiser")]
    public class RandomiserConfig : ConfigFile
    {
        private DateTime _timeButtonPressed = new DateTime();
        private const int _confirmInterval = 5;

        // Every public variable listed here will end up in the config file.
        // Additionally, adding the relevant Attributes will also make them show up in the in-game options menu.
        public int iSeed = 0;

        [Choice("Mode", "Balanced", "Chaotic")]
        public int iRandomiserMode = ConfigDefaults.iRandomiserMode;

        [Toggle("Use fish in logic?")]
        public bool bUseFish = ConfigDefaults.bUseFish;

        [Toggle("Use eggs in logic?")]
        public bool bUseEggs = ConfigDefaults.bUseEggs;

        [Toggle("Use seeds in logic?")]
        public bool bUseSeeds = ConfigDefaults.bUseSeeds;

        [Toggle("Randomise blueprints in databoxes?")]
        public bool bRandomiseDataboxes = ConfigDefaults.bRandomiseDataboxes;

        [Toggle("Randomise fragments?")]
        public bool bRandomiseFragments = ConfigDefaults.bRandomiseFragments;

        [Toggle("Randomise recipes?")]
        public bool bRandomiseRecipes = ConfigDefaults.bRandomiseRecipes;

        [Toggle("Respect vanilla upgrade chains?")]
        public bool bVanillaUpgradeChains = ConfigDefaults.bVanillaUpgradeChains;

        [Toggle("Theme base parts around a common ingredient?")]
        public bool bDoBaseTheming = ConfigDefaults.bDoBaseTheming;

        [Choice("Include equipment as ingredients?", "Never", "Top-level recipes only", "Unrestricted")]
        public int iEquipmentAsIngredients = ConfigDefaults.iEquipmentAsIngredients;

        [Choice("Include tools as ingredients?", "Never", "Top-level recipes only", "Unrestricted")]
        public int iToolsAsIngredients = ConfigDefaults.iToolsAsIngredients;

        [Choice("Include upgrades as ingredients?", "Never", "Top-level recipes only", "Unrestricted")]
        public int iUpgradesAsIngredients = ConfigDefaults.iUpgradesAsIngredients;

        [Slider("Max number of a single ingredient", 1, 10, DefaultValue = 5)]
        public int iMaxAmountPerIngredient = ConfigDefaults.iMaxAmountPerIngredient;

        [Slider("Max ingredients per recipe", 1, 10, DefaultValue = 7)]
        public int iMaxIngredientsPerRecipe = ConfigDefaults.iMaxIngredientsPerRecipe;

        [Slider("Max biomes to spawn each fragment in", 3, 10, DefaultValue = 5)]
        public int iMaxBiomesPerFragment = ConfigDefaults.iMaxBiomesPerFragment;

        [Button("Randomise with new seed")]
        public void NewRandomNewSeed()
        {
            // Re-randomising everything is a serious request, and it should not happen accidentally. This ensures
            // the button is pressed twice within a certain timeframe before actually randomising.
            if (EnsureButtonTime())
            {
                Random random = new Random();
                iSeed = random.Next();
                LogHandler.MainMenuMessage("Changed seed to " + iSeed);
                LogHandler.MainMenuMessage("Randomising...");
                InitMod.Randomise();
                LogHandler.MainMenuMessage("Finished randomising! Please restart the game for changes to take effect.");
            }
            else
            {
                LogHandler.MainMenuMessage("Are you sure you wish to re-randomise all recipes?");
                LogHandler.MainMenuMessage("Press the button again to proceed.");
            }
        }

        [Button("Randomise with same seed")]
        public void NewRandomOldSeed()
        {
            LogHandler.MainMenuMessage("Randomising...");
            Load();
            InitMod.Randomise();
            LogHandler.MainMenuMessage("Finished randomising! Please restart the game for changes to take effect.");
        }

        public string ADVANCED_SETTINGS_BELOW_THIS_POINT = "ADVANCED_SETTINGS_BELOW_THIS_POINT";
        public int iDepthSearchTime = ConfigDefaults.iDepthSearchTime;
        public int iMaxBasicOutpostSize = ConfigDefaults.iMaxBasicOutpostSize;
        public int iMaxEggsAsSingleIngredient = ConfigDefaults.iMaxEggsAsSingleIngredient;
        public int iMaxInventorySizePerRecipe = ConfigDefaults.iMaxInventorySizePerRecipe;
        public double dFuzziness = ConfigDefaults.dFuzziness;
        public double dIngredientRatio = ConfigDefaults.dIngredientRatio;
        public float fFragmentSpawnChanceMin = ConfigDefaults.fFragmentSpawnChanceMin;
        public float fFragmentSpawnChanceMax = ConfigDefaults.fFragmentSpawnChanceMax;

        // Way down here since it tends to take up some space and scrolling is annoying.
        public string sBase64Seed = "";
        public int iSaveVersion = InitMod.s_expectedSaveVersion;

        public void SanitiseConfigValues()
        {
            if (iRandomiserMode > 1 || iRandomiserMode < 0)
                iRandomiserMode = ConfigDefaults.iRandomiserMode;
            if (iToolsAsIngredients > 2 || iToolsAsIngredients < 0)
                iToolsAsIngredients = ConfigDefaults.iToolsAsIngredients;
            if (iUpgradesAsIngredients > 2 || iUpgradesAsIngredients < 0)
                iUpgradesAsIngredients = ConfigDefaults.iUpgradesAsIngredients;
            if (iDepthSearchTime > 45 || iDepthSearchTime < 0)
                iDepthSearchTime = ConfigDefaults.iDepthSearchTime;
            if (iMaxAmountPerIngredient > 10 || iMaxAmountPerIngredient < 1)
                iMaxAmountPerIngredient = ConfigDefaults.iMaxAmountPerIngredient;
            if (iMaxIngredientsPerRecipe > 10 || iMaxIngredientsPerRecipe < 1)
                iMaxIngredientsPerRecipe = ConfigDefaults.iMaxIngredientsPerRecipe;
            if (iMaxBiomesPerFragment > 10 || iMaxBiomesPerFragment < 3)
                iMaxBiomesPerFragment = ConfigDefaults.iMaxBiomesPerFragment;

            // Advanced settings below.
            if (iMaxBasicOutpostSize > 48 || iMaxBasicOutpostSize < 4)
                iMaxBasicOutpostSize = ConfigDefaults.iMaxBasicOutpostSize;
            if (iMaxEggsAsSingleIngredient > 10 || iMaxEggsAsSingleIngredient < 1)
                iMaxEggsAsSingleIngredient = ConfigDefaults.iMaxEggsAsSingleIngredient;
            if (iMaxInventorySizePerRecipe > 100 || iMaxInventorySizePerRecipe < 4)
                iMaxInventorySizePerRecipe = ConfigDefaults.iMaxInventorySizePerRecipe;
            if (dFuzziness > 1 || dFuzziness < 0)
                dFuzziness = ConfigDefaults.dFuzziness;
            if (dIngredientRatio > 1 || dIngredientRatio < 0)
                dIngredientRatio = ConfigDefaults.dIngredientRatio;
            if (fFragmentSpawnChanceMin > 10.0f || fFragmentSpawnChanceMin < 0.01f)
                fFragmentSpawnChanceMin = ConfigDefaults.fFragmentSpawnChanceMin;
            if (fFragmentSpawnChanceMax > 10.0f || fFragmentSpawnChanceMax < 0.01f)
                fFragmentSpawnChanceMax = ConfigDefaults.fFragmentSpawnChanceMax;
        }

        /// <summary>
        /// Ensure the button is pressed twice within a certain timeframe before actually randomising.
        /// </summary>
        /// <returns>True if the button was pressed for the second time, false if not.</returns>
        private bool EnsureButtonTime()
        {
            if (DateTime.UtcNow.Subtract(_timeButtonPressed).TotalSeconds < _confirmInterval)
            {
                _timeButtonPressed = DateTime.MinValue;
                return true;
            }

            _timeButtonPressed = DateTime.UtcNow;
            return false;
        }
    }

    /// Mostly used so that the spoiler log can tell which settings to include.
    internal static class ConfigDefaults
    {
        internal const int iRandomiserMode = 0;
        internal const bool bUseFish = true;
        internal const bool bUseEggs = false;
        internal const bool bUseSeeds = true;
        internal const bool bRandomiseDataboxes = true;
        internal const bool bRandomiseFragments = true;
        internal const bool bRandomiseRecipes = true;
        internal const bool bVanillaUpgradeChains = false;
        internal const bool bDoBaseTheming = false;
        internal const int iEquipmentAsIngredients = 1;
        internal const int iToolsAsIngredients = 1;
        internal const int iUpgradesAsIngredients = 1;
        internal const int iMaxAmountPerIngredient = 5;
        internal const int iMaxIngredientsPerRecipe = 7;
        internal const int iMaxBiomesPerFragment = 5;

        // Advanced setting defaults start here.
        internal const int iDepthSearchTime = 15;
        internal const int iMaxBasicOutpostSize = 24;
        internal const int iMaxEggsAsSingleIngredient = 1;
        internal const int iMaxInventorySizePerRecipe = 24;
        internal const double dFuzziness = 0.2;
        internal const double dIngredientRatio = 0.45;
        internal const float fFragmentSpawnChanceMin = 0.3f;
        internal const float fFragmentSpawnChanceMax = 0.6f;
    }
}
