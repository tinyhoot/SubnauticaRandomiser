using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using QModManager.API.ModLoading;
using SMLHelper.V2.Handlers;

namespace SubnauticaRandomiser
{
    [QModCore]
    public static class InitMod
    {
        internal static string s_modDirectory;
        internal static RandomiserConfig s_config;
        internal static readonly string s_recipeFile = "recipeinformation.csv";
        // The master list of all recipes that have been modified
        internal static RecipeDictionary s_randomisedRecipes = new RecipeDictionary();
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
                s_randomisedRecipes = RestoreRecipeStateFromDisk();
            }
            catch (Exception ex)
            {
                LogHandler.Warn("Could not load recipe state from disk.");
                LogHandler.Warn(ex.Message);
            }

            // Triple checking things here in case the save got corrupted somehow
            if (!_debug_forceRandomise && s_randomisedRecipes != null && s_randomisedRecipes.DictionaryInstance != null && s_randomisedRecipes.DictionaryInstance.Count > 0)
            {
                ProgressionManager.ApplyMasterList(s_randomisedRecipes);
                LogHandler.Debug("Successfully loaded recipe state from disk.");
            }
            else
            {
                LogHandler.Debug("Failed to load recipe state from disk: dictionary empty.");
                s_randomisedRecipes = new RecipeDictionary();

                // Attempt to read and parse the CSV with all recipe information
                List<Recipe> completeMaterialsList;
                completeMaterialsList = CSVReader.ParseFile(s_recipeFile);
                if (completeMaterialsList == null)
                {
                    LogHandler.Fatal("Failed to extract recipe information from CSV, aborting.");
                    return;
                }

                // Try and do some randomising
                // TODO actually make this a real thing
                LogHandler.Debug("Attempting randomisation test...");
                ProgressionManager pm = new ProgressionManager(1, completeMaterialsList);
                pm.AddMaterialsToReachableList(ETechTypeCategory.RawMaterials, 0);
                pm.AddMaterialsToReachableList(ETechTypeCategory.BasicMaterials, 0);
                pm.AddMaterialsToReachableList(ETechTypeCategory.RawMaterials, 0);
                pm.AddMaterialsToReachableList(completeMaterialsList.Find(x => x.ItemType.Equals(TechType.Cyclops)));
                //pm.Randomise();
                LogHandler.Info("Randomisation successful!");
                TestRecipe.EditRadiationSuit(s_randomisedRecipes, completeMaterialsList);

                // pm.RandomiseTest();                            
                // pm.ApplyMasterList(s_randomisedRecipes);
                SaveRecipeStateToDisk();
            }
            LogHandler.Info("Finished loading. Ready to scramble!");
        }

        public static void SaveRecipeStateToDisk()
        {
            if (s_randomisedRecipes.DictionaryInstance != null && s_randomisedRecipes.DictionaryInstance.Count > 0)
            {
                string base64 = s_randomisedRecipes.ToBase64String();
                s_config.sBase64Seed = base64;
                s_config.Save();
                LogHandler.Debug("Saved recipe state to disk!");
            }
            else
            {
                LogHandler.Error("Could not save recipe state to disk: Dictionary empty.");
            }
        }

        public static RecipeDictionary RestoreRecipeStateFromDisk()
        {
            if (String.IsNullOrEmpty(s_config.sBase64Seed))
            {
                throw new InvalidDataException("base64 seed is empty.");
            }
            LogHandler.Debug("Trying to decode base64 string...");
            return RecipeDictionary.FromBase64String(s_config.sBase64Seed);
        }

        public static string GetSubnauticaRandomiserDirectory()
        {
            return new FileInfo(Assembly.GetExecutingAssembly().Location).Directory.FullName;
        }
    }
}
