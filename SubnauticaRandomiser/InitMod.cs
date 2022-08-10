using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using HarmonyLib;
using QModManager.API.ModLoading;
using SMLHelper.V2.Handlers;
using SubnauticaRandomiser.Logic;
using SubnauticaRandomiser.Logic.Recipes;
using SubnauticaRandomiser.RandomiserObjects;

namespace SubnauticaRandomiser
{
    [QModCore]
    public static class InitMod
    {
        internal static string s_modDirectory;
        internal static RandomiserConfig s_config;
        internal const string s_biomeFile = "biomeSlots.csv";
        internal const string s_recipeFile = "recipeInformation.csv";
        internal const string s_wreckageFile = "wreckInformation.csv";
        internal const string s_expectedRecipeMD5 = "4ab1b7a019037f76c0d508f1c2aee5f8";
        internal const int s_expectedSaveVersion = 3;

        internal static readonly Dictionary<int, string> s_versionDict = new Dictionary<int, string> { [1] = "v0.5.1", 
                                                                                                       [2] = "v0.6.1",
                                                                                                       [3] = "v0.7.0"};

        // The master list of everything that is modified by the mod.
        internal static EntitySerializer s_masterDict;
        private const bool _debug_forceRandomise = false;

        [QModPatch]
        public static void Initialise()
        {
            LogHandler.Info("Randomiser starting up!");

            // Register options menu.
            s_modDirectory = GetSubnauticaRandomiserDirectory();
            s_config = OptionsPanelHandler.Main.RegisterModOptions<RandomiserConfig>();
            LogHandler.Debug("Registered options menu.");

            // Ensure the user did not update into a save incompatibility, and abort if they did to preserve a prior
            // version's state.
            if (!CheckSaveCompatibility())
                return;

            // Try and restore a game state from disk.
            try
            {
                s_masterDict = RestoreGameStateFromDisk();
            }
            catch (Exception ex)
            {
                LogHandler.Warn("Could not load game state from disk.");
                LogHandler.Warn(ex.Message);
            }

            // Triple checking things here in case the save got corrupted somehow.
            if (!_debug_forceRandomise && s_masterDict != null)
            {
                ApplyAllChanges();
                LogHandler.Info("Successfully loaded game state from disk.");
            }
            else
            {
                if (_debug_forceRandomise)
                    LogHandler.Warn("Set to forcibly re-randomise recipes.");
                else
                    LogHandler.Warn("Failed to load game state from disk: dictionary empty.");

                Randomise();
                if (s_masterDict?.isDataboxRandomised == true)
                    EnableHarmonyPatching();
            }

            LogHandler.Info("Finished loading.");
        }

        /// <summary>
        /// Randomise the game, discarding any earlier randomisation data.
        /// </summary>
        internal static void Randomise()
        {
            s_masterDict = null;
            s_config.SanitiseConfigValues();
            s_config.iSaveVersion = s_expectedSaveVersion;
            var csvReader = new CSVReader();

            // Attempt to read and parse the CSV with all biome information.
            var biomes = csvReader.ParseBiomeFile(s_biomeFile);
            if (biomes is null)
            {
                LogHandler.Fatal("Failed to extract biome information from CSV, aborting.");
                throw new ParsingException("Failed to extract biome information: null");
            }

            // Attempt to read and parse the CSV with all recipe information.
            var materials = csvReader.ParseRecipeFile(s_recipeFile);
            if (materials is null)
            {
                LogHandler.Fatal("Failed to extract recipe information from CSV, aborting.");
                throw new ParsingException("Failed to extract recipe information: null");
            }

            // Attempt to read and parse the CSV with wreckages and databox info.
            var databoxes = csvReader.ParseWreckageFile(s_wreckageFile);
            if (databoxes is null || databoxes.Count == 0)
            {
                LogHandler.Error("Failed to extract databox information from CSV.");
                throw new ParsingException("Failed to extract databox information: null");
            }

            // Create a new seed if the current one is just a default
            Random random;
            if (s_config.iSeed == 0)
            {
                random = new Random();
                s_config.iSeed = random.Next();
            }
            random = new Random(s_config.iSeed);

            // Randomise!
            CoreLogic logic = new CoreLogic(random, s_config, materials, biomes, databoxes);
            s_masterDict = logic.Randomise();
            ApplyAllChanges();
            LogHandler.Info("Randomisation successful!");

            SaveGameStateToDisk();
        }

        /// <summary>
        /// Apply all changes contained within the serialiser.
        /// </summary>
        /// <exception cref="InvalidDataException">If the serialiser is null or invalid.</exception>
        internal static void ApplyAllChanges()
        {
            if (s_masterDict is null)
                throw new InvalidDataException("Cannot apply randomisation changes: MasterDict is null!");
            
            // Load recipe changes.
            if (s_masterDict.RecipeDict?.Count > 0)
                RecipeLogic.ApplyMasterDict(s_masterDict);
                
            // Load fragment changes.
            if (s_masterDict.SpawnDataDict?.Count > 0)
            {
                FragmentLogic.ApplyMasterDict(s_masterDict);
                LogHandler.Info("Loaded fragment state.");
            }

            // Load databox changes.
            if (s_masterDict.isDataboxRandomised)
                EnableHarmonyPatching();
        }

        /// <summary>
        /// Ensure the user did not update into a save incompatibility.
        /// </summary>
        private static bool CheckSaveCompatibility()
        {
            if (s_config.iSaveVersion == s_expectedSaveVersion)
                return true;
            
            s_versionDict.TryGetValue(s_config.iSaveVersion, out string version);
            if (string.IsNullOrEmpty(version))
                version = "unknown or corrupted.";

            LogHandler.MainMenuMessage("It seems you updated Subnautica Randomiser. This version is incompatible with your previous savegame.");
            LogHandler.MainMenuMessage("The last supported version for your savegame is " + version);
            LogHandler.MainMenuMessage("To protect your previous savegame, no changes to the game have been made.");
            LogHandler.MainMenuMessage("If you wish to continue anyway, randomise again in the options menu or delete your config.json");
            return false;
        }

        /// <summary>
        /// Serialise the current randomisation state to disk.
        /// </summary>
        internal static void SaveGameStateToDisk()
        {
            if (s_masterDict != null)
            {
                string base64 = s_masterDict.ToBase64String();
                s_config.sBase64Seed = base64;
                s_config.Save();
                LogHandler.Debug("Saved game state to disk!");
            }
            else
            {
                LogHandler.Error("Could not save game state to disk: invalid data.");
            }
        }

        /// <summary>
        /// Attempt to deserialise a randomisation state from disk.
        /// </summary>
        /// <returns>The EntitySerializer as previously written to disk.</returns>
        /// <exception cref="InvalidDataException">Raised if the game state is corrupted in some way.</exception>
        internal static EntitySerializer RestoreGameStateFromDisk()
        {
            if (string.IsNullOrEmpty(s_config.sBase64Seed))
            {
                throw new InvalidDataException("base64 seed is empty.");
            }

            LogHandler.Debug("Trying to decode base64 string...");
            EntitySerializer dictionary = EntitySerializer.FromBase64String(s_config.sBase64Seed);

            if (dictionary?.SpawnDataDict is null || dictionary.RecipeDict is null)
            {
                throw new InvalidDataException("base64 seed is invalid; could not deserialize Dictionary.");
            }

            return dictionary;
        }

        /// <summary>
        /// Get the installation directory of the mod.
        /// </summary>
        internal static string GetSubnauticaRandomiserDirectory()
        {
            return new FileInfo(Assembly.GetExecutingAssembly().Location).Directory?.FullName;
        }

        /// <summary>
        /// Enables all necessary harmony patches based on the randomisation state in s_masterDict.
        /// </summary>
        private static void EnableHarmonyPatching()
        {
            if (s_masterDict?.Databoxes?.Count > 0)
            {
                Harmony harmony = new Harmony("SubnauticaRandomiser");
                harmony.PatchAll();
            }
        }
    }
}
