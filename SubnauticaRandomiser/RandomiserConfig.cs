using System;
using SMLHelper.V2.Json;
using SMLHelper.V2.Options.Attributes;
namespace SubnauticaRandomiser
{
    [Menu("Randomiser")]
    public class RandomiserConfig : ConfigFile
    {
        private DateTime _timeButtonPressed = new DateTime();
        private readonly int _confirmInterval = 5;

        // Every public variable listed here will end up in the config file
        // Additionally, adding the relevant Attributes will also make them
        // show up in the in-game options menu
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

        [Slider("Max biomes to spawn each fragment in", 1, 5, DefaultValue = 3)]
        public int iMaxBiomesPerFragment = ConfigDefaults.iMaxBiomesPerFragment;

        [Slider("Spawn chance per fragment per spawn attempt", 0.0f, 0.5f, DefaultValue = 0.1f)]
        public float fFragmentSpawnChance = ConfigDefaults.fFragmentSpawnChance;

        [Button("Randomise with new seed")]
        public void NewRandomNewSeed()
        {
            // Re-randomising everything is a serious request, and it should not
            // happen accidentally. This here ensures the button is pressed twice
            // within a certain timeframe before actually randomising.
            if (EnsureButtonTime())
            {
                Random ran = new Random();
                iSeed = ran.Next();
                ran = new Random(iSeed);
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
            if (iMaxBiomesPerFragment > 10 || iMaxBiomesPerFragment < 1)
                iMaxBiomesPerFragment = ConfigDefaults.iMaxBiomesPerFragment;
            if (fFragmentSpawnChance > 1.0f || fFragmentSpawnChance < 0.01f)
                fFragmentSpawnChance = ConfigDefaults.fFragmentSpawnChance;

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
        }

        private bool EnsureButtonTime()
        {
            // Re-randomising everything is a serious request, and it should not
            // happen accidentally. This here ensures the button is pressed twice
            // within a certain timeframe before actually randomising.
            if (DateTime.UtcNow.Subtract(_timeButtonPressed).TotalSeconds < _confirmInterval)
            {
                _timeButtonPressed = DateTime.MinValue;
                return true;
            }

            _timeButtonPressed = DateTime.UtcNow;
            return false;
        }
    }

    // Mostly used so that the spoiler log can tell which settings to include.
    internal static class ConfigDefaults
    {
        internal static readonly int iRandomiserMode = 0;
        internal static readonly bool bUseFish = true;
        internal static readonly bool bUseEggs = false;
        internal static readonly bool bUseSeeds = true;
        internal static readonly bool bRandomiseDataboxes = true;
        internal static readonly bool bRandomiseFragments = true;
        internal static readonly bool bVanillaUpgradeChains = false;
        internal static readonly bool bDoBaseTheming = false;
        internal static readonly int iEquipmentAsIngredients = 1;
        internal static readonly int iToolsAsIngredients = 1;
        internal static readonly int iUpgradesAsIngredients = 1;
        internal static readonly int iMaxAmountPerIngredient = 5;
        internal static readonly int iMaxIngredientsPerRecipe = 7;
        internal static readonly int iMaxBiomesPerFragment = 3;
        internal static readonly float fFragmentSpawnChance = 0.1f;

        // Advanced setting defaults start here.
        internal static readonly int iDepthSearchTime = 15;
        internal static readonly int iMaxBasicOutpostSize = 24;
        internal static readonly int iMaxEggsAsSingleIngredient = 1;
        internal static readonly int iMaxInventorySizePerRecipe = 24;
        internal static readonly double dFuzziness = 0.2;
        internal static readonly double dIngredientRatio = 0.45;
    }
}
