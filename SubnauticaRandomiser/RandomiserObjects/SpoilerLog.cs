using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace SubnauticaRandomiser
{
    public class SpoilerLog
    {
        internal static readonly string s_fileName = "spoilerlog.txt";
        private static readonly RandomiserConfig config = InitMod.s_config;
        internal static List<KeyValuePair<TechType, int>> s_progression = new List<KeyValuePair<TechType, int>>();
        private static List<string> s_preparedAdvSettings;
        private static List<string> s_preparedDataboxes;
        private static List<string> s_preparedProgressionPath;
        private static string s_preparedMD5;

        private static readonly List<string> s_basicOptions = new List<string>()
        {
            "iSeed",
            "iRandomiserMode",
            "bUseFish", "bUseEggs", "bUseSeeds",
            "bRandomiseDataboxes",
            "bVanillaUpgradeChains",
            "bDoBaseTheming",
            "iEquipmentAsIngredients", "iToolsAsIngredients", "iUpgradesAsIngredients",
            "iMaxIngredientsPerRecipe", "iMaxAmountPerIngredient"
        };

        private static string[] contentHeader =
        {
            "*************************************************",
            "*****   SUBNAUTICA RANDOMISER SPOILER LOG   *****",
            "*************************************************",
            "",
            "Generated on " + DateTime.Now + " with " + InitMod.s_versionDict[InitMod.s_expectedSaveVersion]
        };
        private static string[] contentBasics =
        {
            "",
            "",
            "///// Basic Information /////",
            "Seed: " + config.iSeed,
            "Mode: " + config.iRandomiserMode,
            "Fish, Eggs, Seeds: " + config.bUseFish + ", " + config.bUseEggs + ", " + config.bUseSeeds,
            "Databoxes: " + config.bRandomiseDataboxes,
            "Vanilla Upgrade Chains: " + config.bVanillaUpgradeChains,
            "Base Theming: " + config.bDoBaseTheming,
            "Equipment, Tools, Upgrades: " + config.iEquipmentAsIngredients + ", " + config.iToolsAsIngredients + ", " + config.iUpgradesAsIngredients,
            "Max Ingredients: " + config.iMaxIngredientsPerRecipe + " per recipe, " + config.iMaxAmountPerIngredient + " per ingredient",
            ""
        };
        private static string[] contentAdvanced = 
        {
            "",
            "",
            "///// Depth Progression Path /////"
        };
        private static string[] contentDataboxes =
        {
            "",
            "",
            "///// Databox Locations /////"
        };

        // Add advanced settings to the spoiler log, but only if they have been
        // modified.
        private static void PrepareAdvancedSettings()
        {
            s_preparedAdvSettings = new List<string>();
            FieldInfo[] defaultFieldInfoArray = typeof(ConfigDefaults).GetFields(BindingFlags.NonPublic | BindingFlags.Static);
            FieldInfo[] fieldInfoArray = typeof(RandomiserConfig).GetFields(BindingFlags.Public | BindingFlags.Instance);

            LogHandler.Debug("Number of fields in default, instance: " + defaultFieldInfoArray.Length + ", " + fieldInfoArray.Length);

            foreach (FieldInfo defaultField in defaultFieldInfoArray)
            {
                // Check whether this field is an advanced config option or not.
                if (s_basicOptions.Contains(defaultField.Name))
                    continue;

                foreach (FieldInfo field in fieldInfoArray)
                {
                    if (!field.Name.Equals(defaultField.Name))
                        continue;

                    var value = field.GetValue(config);

                    // If the value of a config field does not correspond to its
                    // default value, the user must have modified it. Add it to
                    // the list in that case.
                    if (!value.Equals(defaultField.GetValue(null)))
                        s_preparedAdvSettings.Add(field.Name + ": " + value);

                    break;
                }
            }

            if (s_preparedAdvSettings.Count == 0)
                s_preparedAdvSettings.Add("No advanced settings were modified.");
        }

        // Grab the randomised boxes from masterDict, and sort them alphabetically.
        private static void PrepareDataboxes()
        {
            s_preparedDataboxes = new List<string>();

            if (InitMod.s_masterDict.isDataboxRandomised)
            {
                foreach (KeyValuePair<RandomiserVector, TechType> entry in InitMod.s_masterDict.Databoxes){
                    s_preparedDataboxes.Add(entry.Value.AsString() + " can be found at " + entry.Key);
                }

                s_preparedDataboxes.Sort();
            }
            else
            {
                s_preparedDataboxes.Add("Not randomised, all in vanilla locations.");
            }
        }

        // Compare the MD5 of the recipe CSV and try to see if it's still the same.
        // Since this is done while parsing the CSV anyway, grab the value from there.
        private static void PrepareMD5()
        {
            if (!InitMod.s_expectedRecipeMD5.Equals(CSVReader.s_recipeCSVMD5))
            {
                s_preparedMD5 = "recipeInformation.csv has been modified: " + CSVReader.s_recipeCSVMD5;
            }
            else
            {
                s_preparedMD5 = "recipeInformation.csv is unmodified.";
            }
        }

        // Make the data gathered during randomising a bit nicer for human eyes.
        private static void PrepareProgressionPath()
        {
            s_preparedProgressionPath = new List<string>();
            int lastDepth = 0;

            foreach (KeyValuePair<TechType, int> pair in s_progression)
            {
                if (pair.Value > lastDepth)
                {
                    s_preparedProgressionPath.Add("Craft " + pair.Key.AsString() + " to reach " + pair.Value + "m");
                }
                else
                {
                    s_preparedProgressionPath.Add("Unlocked " + pair.Key.AsString() + ".");
                }

                lastDepth = pair.Value;
            }
        }

        internal static async Task WriteLog()
        {
            PrepareAdvancedSettings();
            PrepareDataboxes();
            PrepareMD5();
            PrepareProgressionPath();

            using (StreamWriter file = new StreamWriter(Path.Combine(InitMod.s_modDirectory, s_fileName)))
            {
                await WriteTextToLog(file, contentHeader);
                await WriteTextToLog(file, s_preparedMD5);
                await WriteTextToLog(file, contentBasics);
                await WriteTextToLog(file, s_preparedAdvSettings.ToArray());
                await WriteTextToLog(file, contentAdvanced);
                await WriteTextToLog(file, s_preparedProgressionPath.ToArray());
                await WriteTextToLog(file, contentDataboxes);
                await WriteTextToLog(file, s_preparedDataboxes.ToArray());
            }

            LogHandler.Info("Wrote spoiler log to disk.");
        }

        private static async Task WriteTextToLog(StreamWriter file, string text)
        {
            await file.WriteLineAsync(text);
        }

        private static async Task WriteTextToLog(StreamWriter file, string[] text)
        {
            foreach (string line in text)
            {
                await file.WriteLineAsync(line);
            }
        }
    }
}
