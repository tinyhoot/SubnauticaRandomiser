using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using HarmonyLib;
using QModManager.API.ModLoading;
using SMLHelper.V2.Handlers;

namespace SubnauticaRandomiser
{
    [QModCore]
    public static class InitMod
    {
        internal static string s_modDirectory;
        internal static RandomiserConfig s_config;
        internal static readonly string s_recipeFile = "recipeInformation.csv";
        internal static readonly string s_wreckageFile = "wreckInformation.csv";
        internal static readonly int s_expectedSaveVersion = 2;
        private static readonly Dictionary<int, string> s_versionDict = new Dictionary<int, string> { [1] = "v0.5.1" };

        // The master list of all recipes that have been modified
        internal static RecipeDictionary s_masterDict = new RecipeDictionary();
        private static readonly bool _debug_forceRandomise = false;

        [QModPatch]
        public static void Initialise()
        {
            LogHandler.Info("Randomiser starting up!");

            // Register options menu
            s_modDirectory = GetSubnauticaRandomiserDirectory();
            s_config = OptionsPanelHandler.Main.RegisterModOptions<RandomiserConfig>();
            LogHandler.Debug("Registered options menu.");

            // Ensure the user did not update into a save incompatibility.
            if (s_config.iSaveVersion != s_expectedSaveVersion)
            {
                s_versionDict.TryGetValue(s_config.iSaveVersion, out string version);
                if (string.IsNullOrEmpty(version))
                    version = "unknown.";

                LogHandler.MainMenuMessage("It seems you updated Subnautica Randomiser. This version is incompatible with your previous savegame.");
                LogHandler.MainMenuMessage("The last supported version for your savegame is " + version);
                LogHandler.MainMenuMessage("If you wish to continue anyway, randomise again in the options menu or delete your config.json");
                return;
            }

            // Try and restore a recipe state from disk
            try
            {
                s_masterDict = RestoreRecipeStateFromDisk();
            }
            catch (Exception ex)
            {
                LogHandler.Warn("Could not load recipe state from disk.");
                LogHandler.Warn(ex.Message);
            }

            // Triple checking things here in case the save got corrupted somehow
            if (!_debug_forceRandomise && s_masterDict != null && s_masterDict.DictionaryInstance != null && s_masterDict.DictionaryInstance.Count > 0)
            {
                ProgressionManager.ApplyMasterDict(s_masterDict);
                if (s_masterDict.isDataboxRandomised)
                    EnableHarmonyPatching();

                LogHandler.Info("Successfully loaded recipe state from disk.");
            }
            else
            {
                if (_debug_forceRandomise)
                    LogHandler.Warn("Set to forcibly re-randomise recipes.");
                else
                    LogHandler.Warn("Failed to load recipe state from disk: dictionary empty.");

                Randomise();
                if (s_masterDict.isDataboxRandomised)
                    EnableHarmonyPatching();
            }

            LogHandler.Info("Finished loading.");
        }

        internal static void Randomise()
        {
            s_masterDict = new RecipeDictionary();
            s_config.SanitiseConfigValues();
            s_config.iSaveVersion = s_expectedSaveVersion;

            // Attempt to read and parse the CSV with all recipe information.
            List<RandomiserRecipe> completeMaterialsList;
            completeMaterialsList = CSVReader.ParseRecipeFile(s_recipeFile);
            if (completeMaterialsList == null)
            {
                LogHandler.Fatal("Failed to extract recipe information from CSV, aborting.");
                return;
            }

            // Attempt to read and parse the CSV with wreckages and databox info.
            List<Databox> databoxes;
            databoxes = CSVReader.ParseWreckageFile(s_wreckageFile);
            if (databoxes == null || databoxes.Count == 0)
                LogHandler.Error("Failed to extract databox information from CSV.");

            ProgressionManager pm = new ProgressionManager(completeMaterialsList, databoxes, s_config.iSeed);

            pm.RandomSmart(s_masterDict, s_config);
            LogHandler.Info("Randomisation successful!");

            SaveRecipeStateToDisk();
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            SpoilerLog.WriteLog();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        }

        internal static void SaveRecipeStateToDisk()
        {
            if (s_masterDict.DictionaryInstance != null && s_masterDict.DictionaryInstance.Count > 0)
            {
                string base64 = s_masterDict.ToBase64String();
                s_config.sBase64Seed = base64;
                s_config.Save();
                LogHandler.Debug("Saved recipe state to disk!");
            }
            else
            {
                LogHandler.Error("Could not save recipe state to disk: Dictionary empty.");
            }
        }

        internal static RecipeDictionary RestoreRecipeStateFromDisk()
        {
            if (String.IsNullOrEmpty(s_config.sBase64Seed))
            {
                throw new InvalidDataException("base64 seed is empty.");
            }
            LogHandler.Debug("Trying to decode base64 string...");
            return RecipeDictionary.FromBase64String(s_config.sBase64Seed);
        }

        internal static string GetSubnauticaRandomiserDirectory()
        {
            return new FileInfo(Assembly.GetExecutingAssembly().Location).Directory.FullName;
        }

        private static void EnableHarmonyPatching()
        {
            if (s_masterDict != null && s_masterDict.Databoxes != null && s_masterDict.Databoxes.Count > 0)
            {
                Harmony harmony = new Harmony("SubnauticaRandomiser");
                harmony.PatchAll();
            }
        }
    }
}
