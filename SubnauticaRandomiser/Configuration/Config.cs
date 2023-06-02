using BepInEx.Configuration;
using SubnauticaRandomiser.Objects.Enums;

namespace SubnauticaRandomiser.Configuration
{
    internal class Config
    {
        public ConfigEntryWrapper<int> Seed;
        public ConfigEntryWrapper<int> DepthSearchTime;
        public ConfigEntryWrapper<int> MaxDepthWithoutVehicle;
        public ConfigEntryWrapper<bool> DebugForceRandomise;

        // Alternate Start
        public ConfigEntryWrapper<bool> EnableAlternateStartModule;
        public ConfigEntryWrapper<string> SpawnPoint;

        // Aurora
        public ConfigEntryWrapper<bool> RandomiseDoorCodes;
        public ConfigEntryWrapper<bool> RandomiseSupplyBoxes;

        // Databoxes
        public ConfigEntryWrapper<bool> RandomiseDataboxes;

        // Fragments
        public ConfigEntryWrapper<bool> EnableFragmentModule;
        public ConfigEntryWrapper<bool> RandomiseFragments;
        public ConfigEntryWrapper<bool> RandomiseNumFragments;
        public ConfigEntryWrapper<int> MaxFragmentsToUnlock;
        public ConfigEntryWrapper<int> MaxBiomesPerFragment;
        public ConfigEntryWrapper<bool> RandomiseDuplicateScans;
        // Fragments Advanced
        public ConfigEntryWrapper<int> MaxDuplicateScanYield;
        public ConfigEntryWrapper<int> MaxFragmentTypesPerBiome;
        public ConfigEntryWrapper<int> MinFragmentsToUnlock;
        public ConfigEntryWrapper<float> FragmentSpawnChanceMax;
        public ConfigEntryWrapper<float> FragmentSpawnChanceMin;
        public ConfigEntryWrapper<double> RareDropChance;

        // Recipes
        public ConfigEntryWrapper<bool> EnableRecipeModule;
        public ConfigEntryWrapper<bool> RandomiseRecipes;
        public ConfigEntryWrapper<RecipeDifficultyMode> RecipeMode;
        public ConfigEntryWrapper<bool> UseFish;
        public ConfigEntryWrapper<bool> UseEggs;
        public ConfigEntryWrapper<bool> DiscoverEggs;
        public ConfigEntryWrapper<bool> UseSeeds;
        public ConfigEntryWrapper<IngredientInclusionLevel> EquipmentAsIngredients;
        public ConfigEntryWrapper<IngredientInclusionLevel> ToolsAsIngredients;
        public ConfigEntryWrapper<IngredientInclusionLevel> UpgradesAsIngredients;
        public ConfigEntryWrapper<bool> VanillaUpgradeChains;
        public ConfigEntryWrapper<bool> BaseTheming;
        public ConfigEntryWrapper<int> MaxNumberPerIngredient;
        public ConfigEntryWrapper<int> MaxIngredientsPerRecipe;
        // Recipes Advanced
        public ConfigEntryWrapper<int> MaxBasicOutpostSize;
        public ConfigEntryWrapper<int> MaxEggsAsSingleIngredient;
        public ConfigEntryWrapper<int> MaxInventorySizePerRecipe;
        public ConfigEntryWrapper<double> PrimaryIngredientValue;
        public ConfigEntryWrapper<double> RecipeValueVariance;
        
        // Saving
        public ConfigEntry<string> Base64Save;
        public ConfigEntry<int> SaveVersion;

        private ConfigFile _configFile;

        public Config(ConfigFile configFile)
        {
            _configFile = configFile;
        }

        public void RegisterOptions()
        {
            _configFile.Bind("_Welcome", "Welcome", true,
                "Welcome to the config file! A word before you edit anything.\n"
                + "> Changes will not apply until the next time you randomise in game.\n"
                + "> Invalid values for options will be silently reset to their defaults.\n"
                + "> Be careful editing any options in 'Advanced' sections. These are not listed in the in-game menu. "
                + "They are meant for advanced users, and changing them may greatly impact your experience. "
                + "Extreme values may even softlock you. Proceed with caution."
            );
            
            Seed = new ConfigEntryWrapper<int>(
                configFile: _configFile,
                section: "General",
                key: nameof(Seed),
                defaultValue: 0,
                description: "The random seed used to generate a game state."
            );
            // General Advanced
            DebugForceRandomise = new ConfigEntryWrapper<bool>(
                configFile: _configFile,
                section: "General.Advanced",
                key: nameof(DebugForceRandomise),
                defaultValue: false,
                description: "Forces a new seed and re-randomisation on every game startup."
            );
            DepthSearchTime = new ConfigEntryWrapper<int>(
                configFile: _configFile,
                section: "General.Advanced",
                key: nameof(DepthSearchTime),
                defaultValue: 15,
                description: "When calculating how deep you can go with your currently accessible equipment, "
                             + "the randomiser will stop at a depth where you can remain for this many seconds "
                             + "before having to return for air without dying. This number is capped at 45 seconds "
                             + "since that is your maximum at the beginning of the game, without any tanks.",
                acceptableValues: new AcceptableValueRange<int>(0, 45)
            );
            MaxDepthWithoutVehicle = new ConfigEntryWrapper<int>(
                configFile: _configFile,
                section: "General.Advanced",
                key: nameof(MaxDepthWithoutVehicle),
                defaultValue: 200,
                description: "The depth you can reach on foot will be capped at this number. Increasing it will make "
                             + "more materials available to the logic earlier. Very high values can mean very "
                             + "difficult seeds, as a Seamoth (200m) can easily lead to 'access' to late game areas "
                             + "(on foot +500m, for a total of 700m).",
                acceptableValues: new AcceptableValueRange<int>(100, 500)
            );
            
            // Alternate Start
            EnableAlternateStartModule = new ConfigEntryWrapper<bool>(
                configFile: _configFile,
                section: "Spawn",
                key: nameof(EnableAlternateStartModule),
                defaultValue: false,
                description: "Enable spawning module."
            ).WithDescription(
                "Enable Spawning Module",
                null
            );
            SpawnPoint = new ConfigEntryWrapper<string>(
                configFile: _configFile,
                section: "Spawn",
                key: nameof(SpawnPoint),
                defaultValue: "Vanilla",
                description: "The biome the lifepod will spawn in. Random is limited to early game biomes, "
                             + "Chaotic Random chooses from ALL available biomes.",
                acceptableValues: new AcceptableValueList<string>("Vanilla", "Random", "Chaotic Random",
                    "BloodKelp", "BulbZone", "CragField", "CrashZone", "Dunes", "Floating Island", "GrandReef",
                    "GrassyPlateaus", "Kelp", "Mountains", "MushroomForest", "SeaTreaderPath", "SparseReef",
                    "UnderwaterIslands", "Void")
            ).WithDescription(
                "Spawnpoint biome",
                "Random is limited to early game biomes, Chaotic Random chooses from ALL available biomes."
            );

            // Aurora
            RandomiseDoorCodes = new ConfigEntryWrapper<bool>(
                configFile: _configFile,
                section: "Aurora",
                key: nameof(RandomiseDoorCodes),
                defaultValue: true,
                description: "Randomises door access codes inside the Aurora if enabled."
            ).WithDescription(
                "Randomise Aurora door codes?",
                null
            );
            RandomiseSupplyBoxes = new ConfigEntryWrapper<bool>(
                configFile: _configFile,
                section: "Aurora",
                key: nameof(RandomiseSupplyBoxes),
                defaultValue: true,
                description: "Randomises the contents of supply boxes strewn across the ocean floor if enabled."
            ).WithDescription(
                "Randomise supply box contents?",
                null
            );

            // Databoxes
            RandomiseDataboxes = new ConfigEntryWrapper<bool>(
                configFile: _configFile,
                section: "Databoxes",
                key: nameof(RandomiseDataboxes),
                defaultValue: true,
                description: "Shuffles blueprints found in databoxes if enabled. Databoxes will be in the same "
                             + "locations, but contain different blueprints."
            ).WithDescription(
                "Randomise blueprints in databoxes?",
                "Databoxes will be in the same locations, but contain different blueprints."
            );

            
            // Fragments
            EnableFragmentModule = new ConfigEntryWrapper<bool>(
                configFile: _configFile,
                section: "Fragments",
                key: nameof(EnableFragmentModule),
                defaultValue: true,
                description: "Enable fragment module."
            ).WithDescription(
                "Enable Fragment Module",
                null
            );
            RandomiseFragments = new ConfigEntryWrapper<bool>(
                configFile: _configFile,
                section: "Fragments",
                key: nameof(RandomiseFragments),
                defaultValue: true,
                description: "Randomises fragment locations if enabled."
            ).WithDescription(
                "Randomise fragment locations?",
                null
            );
            RandomiseNumFragments = new ConfigEntryWrapper<bool>(
                configFile: _configFile,
                section: "Fragments",
                key: nameof(RandomiseNumFragments),
                defaultValue: true,
                description: "Randomises how many fragments need to be scanned for the blueprint to unlock."
            ).WithDescription(
                "Randomise number of fragments needed?",
                null
            );
            MaxFragmentsToUnlock = new ConfigEntryWrapper<int>(
                configFile: _configFile,
                section: "Fragments",
                key: nameof(MaxFragmentsToUnlock),
                defaultValue: 5,
                description: "The number of fragment scans needed to unlock a blueprint will never exceed this value.",
                acceptableValues: new AcceptableValueRange<int>(1, 30)
            ).WithDescription(
                "Max number of fragments needed",
                null
            );
            MaxBiomesPerFragment = new ConfigEntryWrapper<int>(
                configFile: _configFile,
                section: "Fragments",
                key: nameof(MaxBiomesPerFragment),
                defaultValue: 5,
                description: "Each fragment can occur in a number of biomes no higher than this value. "
                             + "Use with caution. Very low/high values can make it difficult to find enough fragments.",
                acceptableValues: new AcceptableValueRange<int>(3, 10)
            ).WithDescription(
                "Max biomes to spawn each fragment in",
                "Use with caution. Very low/high values can make it difficult to find enough fragments."
            );
            RandomiseDuplicateScans = new ConfigEntryWrapper<bool>(
                configFile: _configFile,
                section: "Fragments",
                key: nameof(RandomiseDuplicateScans),
                defaultValue: true,
                description: "When scanning a fragment you already unlocked, changes the two titanium to a random "
                             + "low-mid value reward."
            ).WithDescription(
                "Randomise duplicate scan rewards?",
                null
            );
            // Fragments Advanced
            MaxDuplicateScanYield = new ConfigEntryWrapper<int>(
                configFile: _configFile,
                section: "Fragments.Advanced",
                key: nameof(MaxDuplicateScanYield),
                defaultValue: 2,
                description: "The maximum number of items you will be given upon scanning a fragment that is "
                             + "already known. Setting this number too high will quickly clutter your inventory.",
                acceptableValues: new AcceptableValueRange<int>(1, 10)
            );
            MaxFragmentTypesPerBiome = new ConfigEntryWrapper<int>(
                configFile: _configFile,
                section: "Fragments.Advanced",
                key: nameof(MaxFragmentTypesPerBiome),
                defaultValue: 4,
                description: "The maximum number of different fragment types that can be placed per biome. Set it too "
                             + "high, and you will struggle to find enough fragments. Set it too low, and the logic "
                             + "will not have enough biomes to actually place fragments in.",
                acceptableValues: new AcceptableValueRange<int>(1, 10)
            );
            MinFragmentsToUnlock = new ConfigEntryWrapper<int>(
                configFile: _configFile,
                section: "Fragments.Advanced",
                key: nameof(MinFragmentsToUnlock),
                defaultValue: 2,
                description: "The number of fragment scans needed to unlock a blueprint will never undercut this value.",
                acceptableValues: new AcceptableValueRange<int>(1, 30)
            );
            FragmentSpawnChanceMax = new ConfigEntryWrapper<float>(
                configFile: _configFile,
                section: "Fragments.Advanced",
                key: nameof(FragmentSpawnChanceMax),
                defaultValue: 0.6f,
                description: "This setting, tethered with the one below, provides a global modifier for the "
                             + "randomiser to decide how likely a fragment spawn should be within a biome. The value "
                             + "it ultimately decides on is multiplied with the vanilla average fragment spawn rate "
                             + "within that biome. Small adjustments can have large effects, particularly if combined "
                             + "with the maximum number of fragments allowed to spawn in a single biome.",
                acceptableValues: new AcceptableValueRange<float>(0.01f, 10.0f)
            );
            FragmentSpawnChanceMin = new ConfigEntryWrapper<float>(
                configFile: _configFile,
                section: "Fragments.Advanced",
                key: nameof(FragmentSpawnChanceMin),
                defaultValue: 0.3f,
                description: "See above.",
                acceptableValues: new AcceptableValueRange<float>(0.01f, 10.0f)
            );
            RareDropChance = new ConfigEntryWrapper<double>(
                configFile: _configFile,
                section: "Fragments.Advanced",
                key: nameof(RareDropChance),
                defaultValue: 0.05,
                description: "Scanning a known fragment has a chance to grant a high-value drop. This setting controls "
                             + "how often these high value drops occur.",
                acceptableValues: new AcceptableValueRange<double>(0.0, 1.0)
            );
            

            // Recipes
            EnableRecipeModule = new ConfigEntryWrapper<bool>(
                configFile: _configFile,
                section: "Recipes",
                key: nameof(EnableRecipeModule),
                defaultValue: true,
                description: "Enable recipe module."
            ).WithDescription(
                "Enable Recipe Module",
                null
            );
            RandomiseRecipes = new ConfigEntryWrapper<bool>(
                configFile: _configFile,
                section: "Recipes",
                key: nameof(RandomiseRecipes),
                defaultValue: true,
                description: "Randomise recipes if enabled."
            ).WithDescription(
                "Randomise recipes?",
                null
            );
            RecipeMode = new ConfigEntryWrapper<RecipeDifficultyMode>(
                configFile: _configFile,
                section: "Recipes",
                key: nameof(RecipeMode),
                defaultValue: Objects.Enums.RecipeDifficultyMode.Balanced,
                description: "Recipe mode. Balanced tries to stick to standard expectations of what should be "
                             + "expensive and what shouldn't. Chaotic is almost purely random."
            ).WithDescription(
                "Recipe mode",
                "Balanced tries to stick to standard expectations of what should be expensive and what "
                + "shouldn't. Chaotic is almost purely random."
            );
            UseFish = new ConfigEntryWrapper<bool>(
                configFile: _configFile,
                section: "Recipes",
                key: nameof(UseFish),
                defaultValue: true,
                description: "Use fish as ingredients if enabled."
            ).WithDescription(
                "Use fish as ingredients?",
                null
            );
            UseEggs = new ConfigEntryWrapper<bool>(
                configFile: _configFile,
                section: "Recipes",
                key: nameof(UseEggs),
                defaultValue: false,
                description: "Use eggs as ingredients if enabled."
            ).WithDescription(
                "Use eggs as ingredients?",
                null
            );
            DiscoverEggs = new ConfigEntryWrapper<bool>(
                configFile: _configFile,
                section: "Recipes",
                key: nameof(DiscoverEggs),
                defaultValue: false,
                description: "Auto-discover all eggs if enabled. This skips having to build an ACU before using eggs."
            ).WithDescription(
                "Auto-discover all eggs?",
                "This skips having to build an ACU before using eggs."
            );
            UseSeeds = new ConfigEntryWrapper<bool>(
                configFile: _configFile,
                section: "Recipes",
                key: nameof(UseSeeds),
                defaultValue: true,
                description: "Use seeds as ingredients if enabled."
            ).WithDescription(
                "Use seeds as ingredients?",
                null
            );
            EquipmentAsIngredients = new ConfigEntryWrapper<IngredientInclusionLevel>(
                configFile: _configFile,
                section: "Recipes",
                key: nameof(EquipmentAsIngredients),
                defaultValue: IngredientInclusionLevel.TopLevelOnly,
                description: "Determine whether to include equipment as possible ingredients in other recipes.\n"
                             + $"{IngredientInclusionLevel.Never}: Equipment is not a valid ingredient.\n"
                             + $"{IngredientInclusionLevel.TopLevelOnly}: Equipment is a valid ingredient in recipes "
                             + $"which cannot be nested, such as base parts.\n"
                             + $"{IngredientInclusionLevel.Unrestricted}: Treat equipment like any other item."
            ).WithDescription(
                "Include equipment as ingredients?",
                "Top-level recipes are recipes which cannot be re-used as ingredients, such as base pieces."
            );
            ToolsAsIngredients = new ConfigEntryWrapper<IngredientInclusionLevel>(
                configFile: _configFile,
                section: "Recipes",
                key: nameof(ToolsAsIngredients),
                defaultValue: IngredientInclusionLevel.TopLevelOnly,
                description: "Determine whether to include tools as possible ingredients in other recipes.\n"
                             + $"{IngredientInclusionLevel.Never}: Tools are not valid ingredients.\n"
                             + $"{IngredientInclusionLevel.TopLevelOnly}: Tools are valid ingredients in recipes "
                             + $"which cannot be nested, such as base parts.\n"
                             + $"{IngredientInclusionLevel.Unrestricted}: Treat tools like any other item."
            ).WithDescription(
                "Include tools as ingredients?",
                "Top-level recipes are recipes which cannot be re-used as ingredients, such as base pieces."
            );
            UpgradesAsIngredients = new ConfigEntryWrapper<IngredientInclusionLevel>(
                configFile: _configFile,
                section: "Recipes",
                key: nameof(UpgradesAsIngredients),
                defaultValue: IngredientInclusionLevel.TopLevelOnly,
                description: "Determine whether to include upgrades as possible ingredients in other recipes.\n"
                             + $"{IngredientInclusionLevel.Never}: Upgrades are not valid ingredients.\n"
                             + $"{IngredientInclusionLevel.TopLevelOnly}: Upgrades are valid ingredients in recipes "
                             + $"which cannot be nested, such as base parts.\n"
                             + $"{IngredientInclusionLevel.Unrestricted}: Treat upgrades like any other item."
            ).WithDescription(
                "Include upgrades as ingredients?",
                "Top-level recipes are recipes which cannot be re-used as ingredients, such as base pieces."
            );
            VanillaUpgradeChains = new ConfigEntryWrapper<bool>(
                configFile: _configFile,
                section: "Recipes",
                key: nameof(VanillaUpgradeChains),
                defaultValue: false,
                description: "If enabled, forces upgrades to be sequential. E.g. vehicle depth upgrade 3 will always "
                             + "require upgrade 2 first. The heat knife will always require a knife. Etc."
            ).WithDescription(
                "Enforce vanilla upgrade chains?",
                "If enabled, forces upgrades to be sequential. E.g. vehicle depth upgrade 3 will always require upgrade 2 first."
            );
            BaseTheming = new ConfigEntryWrapper<bool>(
                configFile: _configFile,
                section: "Recipes",
                key: nameof(BaseTheming),
                defaultValue: false,
                description: "Theme base parts around a common ingredient if enabled. If enabled, every base part will "
                             + "require the same random ingredient in addition to its other ingredients."
            ).WithDescription(
                "Theme base parts around a common ingredient?",
                "If enabled, every base part will require the same random ingredient in addition to its other ingredients."
            );
            MaxNumberPerIngredient = new ConfigEntryWrapper<int>(
                configFile: _configFile,
                section: "Recipes",
                key: nameof(MaxNumberPerIngredient),
                defaultValue: 5,
                description: "The maximum number of a single ingredient. Recipes cannot require more than this many of "
                             + "a single ingredient at once, e.g. no more than 5 titanium.",
                acceptableValues: new AcceptableValueRange<int>(1, 10)
            ).WithDescription(
                "Max number of a single ingredient",
                "Recipes cannot require more than this many of a single ingredient at once, e.g. no more than 5 titanium."
            );
            MaxIngredientsPerRecipe = new ConfigEntryWrapper<int>(
                configFile: _configFile,
                section: "Recipes",
                key: nameof(MaxIngredientsPerRecipe),
                defaultValue: 7,
                description: "Maximum number of ingredient types per recipe. Recipes cannot require more than this "
                             + "many different ingredients.",
                acceptableValues: new AcceptableValueRange<int>(1, 10)
            ).WithDescription(
                "Max ingredient types per recipe",
                "Recipes cannot require more than this many different ingredients."
            );
            // Recipes Advanced
            MaxBasicOutpostSize = new ConfigEntryWrapper<int>(
                configFile: _configFile,
                section: "Recipes.Advanced",
                key: nameof(MaxBasicOutpostSize),
                defaultValue: 24,
                description: "The absolute essentials to establish a small scanning outpost all taken together "
                             + "will not require ingredients which exceed this much space in your inventory. This "
                             + "affects I-corridors, hatches, scanner rooms, windows, solar panels, and beacons.",
                acceptableValues: new AcceptableValueRange<int>(4, 48)
            );
            MaxEggsAsSingleIngredient = new ConfigEntryWrapper<int>(
                configFile: _configFile,
                section: "Recipes.Advanced",
                key: nameof(MaxEggsAsSingleIngredient),
                defaultValue: 1,
                description: "This setting changes how many of the same egg can be required at once. Because "
                             + "eggs are relatively rare and difficult to obtain even with an alien containment, "
                             + "this setting by default caps them at 1. Note that both eggs and the fish that are "
                             + "bred from them are affected.",
                acceptableValues: new AcceptableValueRange<int>(1, 10)
            );
            MaxInventorySizePerRecipe = new ConfigEntryWrapper<int>(
                configFile: _configFile,
                section: "Recipes.Advanced",
                key: nameof(MaxInventorySizePerRecipe),
                defaultValue: 24,
                description: "Some recipes, particularly mid-late game ones like the cyclops, are valued so highly "
                             + "that the total number of ingredients you'd need to craft them would exceed how much "
                             + "you can physically carry in your inventory. Without an inventory-expanding "
                             + "mod, this results in a softlock. The vanilla inventory is 6x8, resulting in 48 "
                             + "units of inventory space. The default value thus blocks any recipe from requiring "
                             + "more than half your inventory at once.\n"
                             + "This setting is included mostly for users of inventory-expanding mods. Increase "
                             + "it at your own risk.",
                acceptableValues: new AcceptableValueRange<int>(4, 144)
            );
            PrimaryIngredientValue = new ConfigEntryWrapper<double>(
                configFile: _configFile,
                section: "Recipes.Advanced",
                key: nameof(PrimaryIngredientValue),
                defaultValue: 0.45,
                description: "This setting only applies to Recipe Mode Balanced.\n"
                             + "While trying to find ingredients to fit into a recipe, the randomiser will always "
                             + "attempt to find a major, high-value item first. This setting controls roughly how "
                             + "valuable that first ingredient should be. It represents a percentage of the total "
                             + "value of the recipe with a tolerance range of 10% of the total in each direction. "
                             + "With the default value, this means that the randomiser will first try to find an "
                             + "ingredient with 35%-55% of the total value before moving on to entirely random ones. "
                             + "Set to 0.0 to disable this behaviour.",
                acceptableValues: new AcceptableValueRange<double>(0.0, 1.0)
            );
            RecipeValueVariance = new ConfigEntryWrapper<double>(
                configFile: _configFile,
                section: "Recipes.Advanced",
                key: nameof(RecipeValueVariance),
                defaultValue: 0.2,
                description: "This setting only applies to Recipe Mode Balanced.\n"
                             + "Every recipe in the game is assigned a value before randomising. This setting controls "
                             + "how closely the randomiser tries to stick to that value before it declares a "
                             + "recipe done.\n"
                             + "The setting represents a percentage. Assume that Titanium Ingots had a value of 100. "
                             + "With this setting set to 0.2, the randomiser will try to reach 100 with a tolerance "
                             + "range of 20%, or half of that in each direction. In practice, this means it will "
                             + "be satisfied once the new ingredients reach any value between 90 and 110. Higher "
                             + "values of this setting thus lead to a much more random experience.",
                acceptableValues: new AcceptableValueRange<double>(0.0, 1.0)
            );
            
            // Saving
            Base64Save = _configFile.Bind("Z.Bottom.Saving", "Base64Save", "",
                "This is your saved randomisation state. Delete this to delete your save.");
            SaveVersion = _configFile.Bind("Z.Bottom.Saving", "SaveVersion", Initialiser._ExpectedSaveVersion,
                "This helps the mod keep track of whether you updated into a save incompatibility. "
                + "Do not touch this.");
            
            RegisterControllingOptions();
        }

        /// <summary>
        /// Set up all options that can toggle the display of other options in the mod menu.
        /// </summary>
        private void RegisterControllingOptions()
        {
            EnableAlternateStartModule.WithControlOverOptions(
                SpawnPoint
            );
            EnableFragmentModule.WithControlOverOptions(
                RandomiseFragments,
                RandomiseNumFragments,
                MaxFragmentsToUnlock,
                MaxBiomesPerFragment,
                RandomiseDuplicateScans
            );
            EnableRecipeModule.WithControlOverOptions(
                RandomiseRecipes,
                RecipeMode,
                UseFish,
                UseEggs,
                DiscoverEggs,
                UseSeeds,
                EquipmentAsIngredients,
                ToolsAsIngredients,
                UpgradesAsIngredients,
                VanillaUpgradeChains,
                BaseTheming,
                MaxNumberPerIngredient,
                MaxIngredientsPerRecipe
            );
        }
        
        public void Reload()
        {
            _configFile.Reload();
        }
        
        public void Save()
        {
            _configFile.Save();
        }
    }
}