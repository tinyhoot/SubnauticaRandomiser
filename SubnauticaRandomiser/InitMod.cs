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
                LogHandler.Debug("Successfully loaded recipe state from disk.");
            }
            else
            {
                if (_debug_forceRandomise)
                    LogHandler.Warn("Set to forcibly re-randomise recipes.");
                else
                    LogHandler.Warn("Failed to load recipe state from disk: dictionary empty.");

                Randomise();
            }
            LogHandler.Info("Finished loading.");

            // Only if randomising databoxes is enabled in the config, patch them
            // with Harmony. Make sure nothing can go wrong.
            if (s_config.bRandomiseDataboxes && s_masterDict != null && s_masterDict.Databoxes != null && s_masterDict.Databoxes.Count > 0)
            {
                Harmony harmony = new Harmony("SubnauticaRandomiser");
                harmony.PatchAll();
            }

        }

        internal static void Randomise()
        {
            s_masterDict = new RecipeDictionary();
            s_config.SanitiseConfigValues();

            // Attempt to read and parse the CSV with all recipe information.
            List<Recipe> completeMaterialsList;
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

            ProgressionManager pm = new ProgressionManager(completeMaterialsList, databoxes);

            //pm.RandomSubstituteMaterials(s_masterDict, s_config.bUseFish, s_config.bUseSeeds);
            pm.RandomSmart(s_masterDict, s_config);
            LogHandler.Info("Randomisation successful!");

            SaveRecipeStateToDisk();
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
    }
}
