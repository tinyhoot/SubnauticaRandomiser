using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace SubnauticaRandomiser.RandomiserObjects
{
    public class SpoilerLog
    {
        internal static readonly string s_fileName = "spoilerlog.txt";
        private RandomiserConfig _config;
        internal static List<KeyValuePair<TechType, int>> s_progression = new List<KeyValuePair<TechType, int>>();

        private List<string> _basicOptions;
        private string[] _contentHeader;
        private string[] _contentBasics;
        private string[] _contentAdvanced;
        private string[] _contentDataboxes;

        internal SpoilerLog(RandomiserConfig config)
        {
            _config = config;
        }

        // Prepare most of the fluff of the log.
        private void PrepareStrings()
        {
            _basicOptions = new List<string>()
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
            _contentHeader = new string[]
            {
                "*************************************************",
                "*****   SUBNAUTICA RANDOMISER SPOILER LOG   *****",
                "*************************************************",
                "",
                "Generated on " + DateTime.Now + " with " + InitMod.s_versionDict[InitMod.s_expectedSaveVersion]
            };
            _contentBasics = new string[]
            {
                "",
                "",
                "///// Basic Information /////",
                "Seed: " + _config.iSeed,
                "Mode: " + _config.iRandomiserMode,
                "Fish, Eggs, Seeds: " + _config.bUseFish + ", " + _config.bUseEggs + ", " + _config.bUseSeeds,
                "Databoxes: " + _config.bRandomiseDataboxes,
                "Vanilla Upgrade Chains: " + _config.bVanillaUpgradeChains,
                "Base Theming: " + _config.bDoBaseTheming,
                "Equipment, Tools, Upgrades: " + _config.iEquipmentAsIngredients + ", " + _config.iToolsAsIngredients + ", " + _config.iUpgradesAsIngredients,
                "Max Ingredients: " + _config.iMaxIngredientsPerRecipe + " per recipe, " + _config.iMaxAmountPerIngredient + " per ingredient",
                ""
            };
            _contentAdvanced = new string[]
            {
                "",
                "",
                "///// Depth Progression Path /////"
            };
            _contentDataboxes = new string[]
            {
                "",
                "",
                "///// Databox Locations /////"
            };
        }

        // Add advanced settings to the spoiler log, but only if they have been
        // modified.
        private string[] PrepareAdvancedSettings()
        {
            List<string> preparedAdvSettings = new List<string>();
            FieldInfo[] defaultFieldInfoArray = typeof(ConfigDefaults).GetFields(BindingFlags.NonPublic | BindingFlags.Static);
            FieldInfo[] fieldInfoArray = typeof(RandomiserConfig).GetFields(BindingFlags.Public | BindingFlags.Instance);

            LogHandler.Debug("Number of fields in default, instance: " + defaultFieldInfoArray.Length + ", " + fieldInfoArray.Length);

            foreach (FieldInfo defaultField in defaultFieldInfoArray)
            {
                // Check whether this field is an advanced config option or not.
                if (_basicOptions.Contains(defaultField.Name))
                    continue;

                foreach (FieldInfo field in fieldInfoArray)
                {
                    if (!field.Name.Equals(defaultField.Name))
                        continue;

                    var value = field.GetValue(_config);

                    // If the value of a config field does not correspond to its
                    // default value, the user must have modified it. Add it to
                    // the list in that case.
                    if (!value.Equals(defaultField.GetValue(null)))
                        preparedAdvSettings.Add(field.Name + ": " + value);

                    break;
                }
            }

            if (preparedAdvSettings.Count == 0)
                preparedAdvSettings.Add("No advanced settings were modified.");

            return preparedAdvSettings.ToArray();
        }

        // Grab the randomised boxes from masterDict, and sort them alphabetically.
        private string[] PrepareDataboxes()
        {
            if (!InitMod.s_masterDict.isDataboxRandomised)
                return new string[] { "Not randomised, all in vanilla locations." };

            List<string> preparedDataboxes = new List<string>();

            foreach (KeyValuePair<RandomiserVector, TechType> entry in InitMod.s_masterDict.Databoxes) 
            {
                preparedDataboxes.Add(entry.Value.AsString() + " can be found at " + entry.Key);
            }

            preparedDataboxes.Sort();

            return preparedDataboxes.ToArray();
        }

        // Compare the MD5 of the recipe CSV and try to see if it's still the same.
        // Since this is done while parsing the CSV anyway, grab the value from there.
        private string PrepareMD5()
        {
            if (!InitMod.s_expectedRecipeMD5.Equals(CSVReader.s_recipeCSVMD5))
            {
                return "recipeInformation.csv has been modified: " + CSVReader.s_recipeCSVMD5;
            }
            else
            {
                return "recipeInformation.csv is unmodified.";
            }
        }

        // Make the data gathered during randomising a bit nicer for human eyes.
        private string[] PrepareProgressionPath()
        {
            List <string> preparedProgressionPath = new List<string>();
            int lastDepth = 0;

            foreach (KeyValuePair<TechType, int> pair in s_progression)
            {
                if (pair.Value > lastDepth)
                {
                    preparedProgressionPath.Add("Craft " + pair.Key.AsString() + " to reach " + pair.Value + "m");
                }
                else
                {
                    preparedProgressionPath.Add("Unlocked " + pair.Key.AsString() + ".");
                }

                lastDepth = pair.Value;
            }

            return preparedProgressionPath.ToArray();
        }

        internal async Task WriteLog()
        {
            List<string> lines = new List<string>();
            PrepareStrings();

            lines.AddRange(_contentHeader);
            lines.Add(PrepareMD5());

            lines.AddRange(_contentBasics);
            lines.AddRange(PrepareAdvancedSettings());
            lines.AddRange(_contentAdvanced);

            lines.AddRange(PrepareProgressionPath());
            lines.AddRange(_contentDataboxes);
            lines.AddRange(PrepareDataboxes());

            using (StreamWriter file = new StreamWriter(Path.Combine(InitMod.s_modDirectory, s_fileName)))
            {
                await WriteTextToLog(file, lines.ToArray());
            }

            LogHandler.Info("Wrote spoiler log to disk.");
        }

        private async Task WriteTextToLog(StreamWriter file, string text)
        {
            await file.WriteLineAsync(text);
        }

        private async Task WriteTextToLog(StreamWriter file, string[] text)
        {
            foreach (string line in text)
            {
                await file.WriteLineAsync(line);
            }
        }
    }
}
