using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace SubnauticaRandomiser
{
    public class SpoilerLog
    {
        internal static readonly string s_fileName = "spoilerlog.txt";
        private static readonly RandomiserConfig config = InitMod.s_config;
        internal static List<KeyValuePair<TechType, int>> s_progression = new List<KeyValuePair<TechType, int>>();
        private static List<string> s_preparedDataboxes;
        private static List<string> s_preparedProgressionPath;
        private static string s_preparedMD5;

        private static string[] contentHeader =
        {
            "*************************************************",
            "*****   SUBNAUTICA RANDOMISER SPOILER LOG   *****",
            "*************************************************",
            "",
            "Generated on " + DateTime.Now
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
            "Equipment, Tools, Upgrades: " + config.iEquipmentAsIngredients + ", " + config.iToolsAsIngredients + ", " + config.iUpgradesAsIngredients,
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
            if (CSVReader.s_isModifiedRecipeCSV)
            {
                s_preparedMD5 = "recipeInformation.csv has been modified.";
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
            PrepareDataboxes();
            PrepareMD5();
            PrepareProgressionPath();

            using (StreamWriter file = new StreamWriter(Path.Combine(InitMod.s_modDirectory, s_fileName)))
            {
                await WriteTextToLog(file, contentHeader);
                await WriteTextToLog(file, s_preparedMD5);
                await WriteTextToLog(file, contentBasics);
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
