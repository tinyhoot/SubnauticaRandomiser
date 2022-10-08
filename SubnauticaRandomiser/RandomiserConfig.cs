using System;
using SMLHelper.V2.Json;
using SMLHelper.V2.Options.Attributes;
using SubnauticaRandomiser.RandomiserObjects;

namespace SubnauticaRandomiser
{
    [Menu("Randomiser")]
    public class RandomiserConfig : ConfigFile
    {
        private DateTime _timeButtonPressed = new DateTime();
        private const double _buttonMinInterval = 0.5;

        // Every public variable listed here will end up in the config file.
        // Additionally, adding the relevant Attributes will also make them show up in the in-game options menu.
        public int iSeed = 0;

        [Choice("Mode", "Balanced", "Chaotic")]
        public int iRandomiserMode = (int)ConfigDefaults.GetDefault("iRandomiserMode");

        [Choice("Spawnpoint", "Vanilla", "Random", "Chaotic Random", "BloodKelp", "BulbZone", "CragField",
            "CrashZone", "Dunes", "Floating Island", "GrandReef", "GrassyPlateaus", "Kelp", "Mountains",
            "MushroomForest", "SeaTreaderPath", "SparseReef", "UnderwaterIslands", "Void")]
        public string sSpawnPoint = "Vanilla";

        [Toggle("Use fish in logic?")] public bool bUseFish = (bool)ConfigDefaults.GetDefault("bUseFish");

        [Toggle("Use eggs in logic?")] public bool bUseEggs = (bool)ConfigDefaults.GetDefault("bUseEggs");

        [Toggle("Use seeds in logic?")] public bool bUseSeeds = (bool)ConfigDefaults.GetDefault("bUseSeeds");

        [Toggle("Randomise blueprints in databoxes?")]
        public bool bRandomiseDataboxes = (bool)ConfigDefaults.GetDefault("bRandomiseDataboxes");

        [Toggle("Randomise fragment locations?")]
        public bool bRandomiseFragments = (bool)ConfigDefaults.GetDefault("bRandomiseFragments");

        [Toggle("Randomise number of fragments needed?")]
        public bool bRandomiseNumFragments = (bool)ConfigDefaults.GetDefault("bRandomiseNumFragments");

        [Toggle("Randomise duplicate scan rewards?")]
        public bool bRandomiseDuplicateScans = (bool)ConfigDefaults.GetDefault("bRandomiseDuplicateScans");

        [Toggle("Randomise recipes?")]
        public bool bRandomiseRecipes = (bool)ConfigDefaults.GetDefault("bRandomiseRecipes");

        [Toggle("Respect vanilla upgrade chains?")]
        public bool bVanillaUpgradeChains = (bool)ConfigDefaults.GetDefault("bVanillaUpgradeChains");

        [Toggle("Theme base parts around a common ingredient?")]
        public bool bDoBaseTheming = (bool)ConfigDefaults.GetDefault("bDoBaseTheming");

        [Choice("Include equipment as ingredients?", "Never", "Top-level recipes only", "Unrestricted")]
        public int iEquipmentAsIngredients = (int)ConfigDefaults.GetDefault("iEquipmentAsIngredients");

        [Choice("Include tools as ingredients?", "Never", "Top-level recipes only", "Unrestricted")]
        public int iToolsAsIngredients = (int)ConfigDefaults.GetDefault("iToolsAsIngredients");

        [Choice("Include upgrades as ingredients?", "Never", "Top-level recipes only", "Unrestricted")]
        public int iUpgradesAsIngredients = (int)ConfigDefaults.GetDefault("iUpgradesAsIngredients");

        [Slider("Max number of a single ingredient", 1, 10, DefaultValue = 5)]
        public int iMaxAmountPerIngredient = (int)ConfigDefaults.GetDefault("iMaxAmountPerIngredient");

        [Slider("Max ingredients per recipe", 1, 10, DefaultValue = 7)]
        public int iMaxIngredientsPerRecipe = (int)ConfigDefaults.GetDefault("iMaxIngredientsPerRecipe");

        [Slider("Max biomes to spawn each fragment in", 3, 10, DefaultValue = 5)]
        public int iMaxBiomesPerFragment = (int)ConfigDefaults.GetDefault("iMaxBiomesPerFragment");

        [Slider("Max number of fragments needed", 1, 20, DefaultValue = 5)]
        public int iMaxFragmentsToUnlock = (int)ConfigDefaults.GetDefault("iMaxFragmentsToUnlock");

        [Button("Randomise with new seed")]
        public void NewRandomNewSeed()
        {
            // Due to how the randomiser locks up when pressing the button, it is possible for the click to be
            // registered twice and randomisation to happen twice in a row. Prevent this here.
            if (WasButtonRecentlyPressed())
                return;

            Random random = new Random();
            iSeed = random.Next();
            LogHandler.MainMenuMessage("Changed seed to " + iSeed);
            LogHandler.MainMenuMessage("Randomising...");
            InitMod.Randomise();
            LogHandler.MainMenuMessage("Finished randomising! Please restart the game for changes to take effect.");
        }

        [Button("Randomise with same seed")]
        public void NewRandomOldSeed()
        {
            if (WasButtonRecentlyPressed())
                return;
            
            LogHandler.MainMenuMessage("Randomising...");
            // Ensure all manual changes to the config file are loaded.
            Load();
            InitMod.Randomise();
            LogHandler.MainMenuMessage("Finished randomising! Please restart the game for changes to take effect.");
        }

        public string ADVANCED_SETTINGS_BELOW_THIS_POINT = "ADVANCED_SETTINGS_BELOW_THIS_POINT";
        public int iDepthSearchTime = (int)ConfigDefaults.GetDefault("iDepthSearchTime");
        public int iMaxBasicOutpostSize = (int)ConfigDefaults.GetDefault("iMaxBasicOutpostSize");
        public int iMaxDepthWithoutVehicle = (int)ConfigDefaults.GetDefault("iMaxDepthWithoutVehicle");
        public int iMaxDuplicateScanYield = (int)ConfigDefaults.GetDefault("iMaxDuplicateScanYield");
        public int iMaxEggsAsSingleIngredient = (int)ConfigDefaults.GetDefault("iMaxEggsAsSingleIngredient");
        public int iMaxFragmentsPerBiome = (int)ConfigDefaults.GetDefault("iMaxFragmentsPerBiome");
        public int iMaxInventorySizePerRecipe = (int)ConfigDefaults.GetDefault("iMaxInventorySizePerRecipe");
        public int iMinFragmentsToUnlock = (int)ConfigDefaults.GetDefault("iMinFragmentsToUnlock");
        public double dPrimaryIngredientValue = (double)ConfigDefaults.GetDefault("dPrimaryIngredientValue");
        public double dRecipeValueVariance = (double)ConfigDefaults.GetDefault("dRecipeValueVariance");
        public float fFragmentSpawnChanceMin = (float)ConfigDefaults.GetDefault("fFragmentSpawnChanceMin");
        public float fFragmentSpawnChanceMax = (float)ConfigDefaults.GetDefault("fFragmentSpawnChanceMax");

        // Way down here since it tends to take up some space and scrolling is annoying.
        public string sBase64Seed = "";
        public int iSaveVersion = InitMod.s_expectedSaveVersion;

        public void SanitiseConfigValues()
        {
            // Iterate through every variable of the config.
            foreach (var field in typeof(RandomiserConfig).GetFields())
            {
                string name = field.Name;
                Type type = field.FieldType;
                // Skip clamping values for special cases, and for non-numeric options.
                if (!ConfigDefaults.Contains(name) || type == typeof(bool))
                {
                    // LogHandler.Debug("Skipping config sanity check for variable " + name);
                    continue;
                }

                var value = (IComparable)field.GetValue(this);
                
                // If the variable is outside the range of acceptable values, reset it.
                if (value.CompareTo(ConfigDefaults.GetMin(name)) < 0
                    || value.CompareTo(ConfigDefaults.GetMax(name)) > 0)
                {
                    LogHandler.Debug("Resetting invalid config value for " + name);
                    field.SetValue(this, ConfigDefaults.GetDefault(name));
                }
            }
        }

        /// <summary>
        /// Ensure the button is not accidentally pressed twice within a certain timeframe by checking against the
        /// system clock.
        /// </summary>
        /// <returns>True if the button was recently pressed, false if it was not.</returns>
        private bool WasButtonRecentlyPressed()
        {
            if (DateTime.UtcNow.Subtract(_timeButtonPressed).TotalSeconds< _buttonMinInterval)
                return false;

            _timeButtonPressed = DateTime.UtcNow;
            return true;
        }
    }
}