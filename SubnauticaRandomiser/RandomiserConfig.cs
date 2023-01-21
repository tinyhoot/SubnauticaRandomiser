using System;
using SMLHelper.V2.Json;
using SMLHelper.V2.Options.Attributes;
using SubnauticaRandomiser.Handlers;
using SubnauticaRandomiser.Interfaces;

namespace SubnauticaRandomiser
{
    [Menu("Randomiser")]
    internal class RandomiserConfig : ConfigFile
    {
        private readonly ILogHandler _log;
        private DateTime _lastButtonPress;
        private const double _ButtonMinInterval = 0.5;

        public RandomiserConfig()
        {
            _log = new LogHandler();
            _lastButtonPress = new DateTime();
        }
        
        public RandomiserConfig(ILogHandler logger)
        {
            _log = logger;
            _lastButtonPress = DateTime.UtcNow;
        }
        
        [Button("Randomise!", Order = 0,
            Tooltip = "Apply your config changes and randomise. Restart your game afterwards!")]
        public void NewRandomNewSeed()
        {
            // Due to how the randomiser locks up when pressing the button, it is possible for the click to be
            // registered twice and randomisation to happen twice in a row. Prevent this here.
            if (!IsButtonPressAllowed(DateTime.UtcNow))
                return;

            Random random = new Random();
            iSeed = random.Next();
            _log.InGameMessage("Changed seed to " + iSeed);
            _log.InGameMessage("Randomising...");
            Initialiser._Main.RandomiseFromConfig();
        }

        [Button("Apply config from disk", Order = 0,
            Tooltip = "If someone else gave you their config.json, click this to load it. Restart your game afterwards!")]
        public void NewRandomOldSeed()
        {
            if (!IsButtonPressAllowed(DateTime.UtcNow))
                return;
            
            _log.InGameMessage("Randomising...");
            // Ensure all manual changes to the config file are loaded.
            Load();
            Initialiser._Main.RandomiseFromConfig();
        }

        // Every public variable listed here will end up in the config file.
        // Additionally, adding the relevant Attributes will also make them show up in the in-game options menu.
        public int iSeed = 0;

        [Choice("Spawnpoint biome", "Vanilla", "Random", "Chaotic Random", "BloodKelp", "BulbZone",
            "CragField", "CrashZone", "Dunes", "Floating Island", "GrandReef", "GrassyPlateaus", "Kelp", "Mountains",
            "MushroomForest", "SeaTreaderPath", "SparseReef", "UnderwaterIslands", "Void",
            Tooltip = "Random is limited to early game biomes, Chaotic Random chooses from ALL available biomes.")]
        public string sSpawnPoint = "Vanilla";
        
        [Toggle("------------------------------------------------------------------------------------------")]
        public bool visualDivider1 = true;

        [Toggle("Randomise blueprints in databoxes?",
            Tooltip = "Databoxes will be in the same locations, but contain different blueprints.")]
        public bool bRandomiseDataboxes = (bool)ConfigDefaults.GetDefault("bRandomiseDataboxes");

        [Toggle("------------------------------------------------------------------------------------------")]
        public bool visualDivider2 = true;

        [Toggle("Randomise fragment locations?")]
        public bool bRandomiseFragments = (bool)ConfigDefaults.GetDefault("bRandomiseFragments");

        [Toggle("Randomise number of fragments needed?",
            Tooltip = "Randomises how many fragments need to be scanned for the blueprint to unlock.")]
        public bool bRandomiseNumFragments = (bool)ConfigDefaults.GetDefault("bRandomiseNumFragments");
        
        [Slider("Max number of fragments needed", 1, 20, DefaultValue = 5,
            Tooltip = "The number of scans needed to unlock a blueprint will never exceed this value.")]
        public int iMaxFragmentsToUnlock = (int)ConfigDefaults.GetDefault("iMaxFragmentsToUnlock");
        
        [Slider("Max biomes to spawn each fragment in", 3, 10, DefaultValue = 5,
            Tooltip = "Use with caution. Very low/high values can make it difficult to find enough fragments.")]
        public int iMaxBiomesPerFragment = (int)ConfigDefaults.GetDefault("iMaxBiomesPerFragment");

        [Toggle("Randomise duplicate scan rewards?",
            Tooltip = "When scanning a fragment you already unlocked, changes the two titanium to a random low-mid value reward.")]
        public bool bRandomiseDuplicateScans = (bool)ConfigDefaults.GetDefault("bRandomiseDuplicateScans");
        
        [Toggle("------------------------------------------------------------------------------------------")]
        public bool visualDivider3 = true;

        [Toggle("Randomise recipes?")]
        public bool bRandomiseRecipes = (bool)ConfigDefaults.GetDefault("bRandomiseRecipes");
        
        [Choice("Recipe mode", "Balanced", "Chaotic",
            Tooltip = "Balanced tries to stick to standard expectations of what should be expensive and what shouldn't. Chaotic is almost purely random.")]
        public int iRandomiserMode = (int)ConfigDefaults.GetDefault("iRandomiserMode");

        [Toggle("Use fish as ingredients?")]
        public bool bUseFish = (bool)ConfigDefaults.GetDefault("bUseFish");

        [Toggle("Use eggs as ingredients?")]
        public bool bUseEggs = (bool)ConfigDefaults.GetDefault("bUseEggs");

        [Toggle("Use seeds as ingredients?")]
        public bool bUseSeeds = (bool)ConfigDefaults.GetDefault("bUseSeeds");

        [Choice("Include equipment as ingredients?", "Never", "Top-level recipes only", "Unrestricted",
            Tooltip = "Top-level recipes are recipes which cannot be re-used as ingredients, such as base pieces.")]
        public int iEquipmentAsIngredients = (int)ConfigDefaults.GetDefault("iEquipmentAsIngredients");

        [Choice("Include tools as ingredients?", "Never", "Top-level recipes only", "Unrestricted",
            Tooltip = "Top-level recipes are recipes which cannot be re-used as ingredients, such as base pieces.")]
        public int iToolsAsIngredients = (int)ConfigDefaults.GetDefault("iToolsAsIngredients");

        [Choice("Include upgrades as ingredients?", "Never", "Top-level recipes only", "Unrestricted",
            Tooltip = "Top-level recipes are recipes which cannot be re-used as ingredients, such as base pieces.")]
        public int iUpgradesAsIngredients = (int)ConfigDefaults.GetDefault("iUpgradesAsIngredients");
        
        [Toggle("Enforce vanilla upgrade chains?",
            Tooltip = "If enabled, forces upgrades to be sequential. E.g. vehicle depth upgrade 3 will always require upgrade 2 first.")]
        public bool bVanillaUpgradeChains = (bool)ConfigDefaults.GetDefault("bVanillaUpgradeChains");

        [Toggle("Theme base parts around a common ingredient?",
            Tooltip = "If enabled, every base part will require the same random ingredient in addition to its other ingredients.")]
        public bool bDoBaseTheming = (bool)ConfigDefaults.GetDefault("bDoBaseTheming");

        [Slider("Max number of a single ingredient", 1, 10, DefaultValue = 5,
            Tooltip = "Recipes cannot require more than this many of a single ingredient at once, e.g. no more than 5 titanium.")]
        public int iMaxAmountPerIngredient = (int)ConfigDefaults.GetDefault("iMaxAmountPerIngredient");

        [Slider("Max ingredient types per recipe", 1, 10, DefaultValue = 7,
            Tooltip = "Recipes cannot require more than this many different ingredients.")]
        public int iMaxIngredientsPerRecipe = (int)ConfigDefaults.GetDefault("iMaxIngredientsPerRecipe");
        
        [Toggle("------------------------------------------------------------------------------------------")]
        public bool visualDivider4 = true;

        [Toggle("Randomise Aurora door codes?")]
        public bool bRandomiseDoorCodes = (bool)ConfigDefaults.GetDefault("bRandomiseDoorCodes");
        
        [Toggle("Randomise supply box contents?")]
        public bool bRandomiseSupplyBoxes = (bool)ConfigDefaults.GetDefault("bRandomiseSupplyBoxes");

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
        public bool debug_forceRandomise = false;

        // Way down here since it tends to take up some space and scrolling is annoying.
        public string sBase64Seed = "";
        public int iSaveVersion = Initialiser._ExpectedSaveVersion;

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
                    // _log.Debug("Skipping config sanity check for variable " + name);
                    continue;
                }

                var value = (IComparable)field.GetValue(this);
                
                // If the variable is outside the range of acceptable values, reset it.
                if (value.CompareTo(ConfigDefaults.GetMin(name)) < 0
                    || value.CompareTo(ConfigDefaults.GetMax(name)) > 0)
                {
                    _log.Debug("Resetting invalid config value for " + name);
                    field.SetValue(this, ConfigDefaults.GetDefault(name));
                }
            }
        }

        /// <summary>
        /// Ensure the button is not accidentally pressed twice within a certain timeframe by checking against the
        /// system clock.
        /// </summary>
        /// <returns>True if the button was not recently pressed, false if it was.</returns>
        internal bool IsButtonPressAllowed(DateTime time)
        {
            if (time.Subtract(_lastButtonPress).TotalSeconds < _ButtonMinInterval)
                return false;

            _lastButtonPress = time;
            return true;
        }
    }
}