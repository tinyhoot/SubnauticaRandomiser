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
        private static List<string> _preparedDataboxes;
        private static List<string> _preparedProgressionPath;

        private static string[] content1 = 
        {
            "*************************************************",
            "*****   SUBNAUTICA RANDOMISER SPOILER LOG   *****",
            "*************************************************",
            "",
            "Generated on " + DateTime.Now,
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
        private static string[] content2 =
        {
            "",
            "",
            "///// Databox Locations /////"
        };

        // Grab the randomised boxes from masterDict, and sort them alphabetically.
        private static void PrepareDataboxes()
        {
            _preparedDataboxes = new List<string>();

            if (InitMod.s_masterDict.isDataboxRandomised)
            {
                foreach (KeyValuePair<RandomiserVector, TechType> entry in InitMod.s_masterDict.Databoxes){
                    _preparedDataboxes.Add(entry.Value.AsString() + " can be found at " + entry.Key);
                }

                _preparedDataboxes.Sort();
            }
            else
            {
                _preparedDataboxes.Add("Not randomised, all in vanilla locations.");
            }
        }

        // Make the data gathered during randomising a bit nicer for human eyes.
        private static void PrepareProgressionPath()
        {
            _preparedProgressionPath = new List<string>();
            int lastDepth = 0;

            foreach (KeyValuePair<TechType, int> pair in s_progression)
            {
                if (pair.Value > lastDepth)
                {
                    _preparedProgressionPath.Add("Craft " + pair.Key.AsString() + " to reach " + pair.Value + "m");
                }
                else
                {
                    _preparedProgressionPath.Add("Unlocked " + pair.Key.AsString() + ".");
                }

                lastDepth = pair.Value;
            }
        }

        public static async Task WriteLog()
        {
            PrepareDataboxes();
            PrepareProgressionPath();

            using (StreamWriter file = new StreamWriter(Path.Combine(InitMod.s_modDirectory, s_fileName)))
            {
                await WriteTextToLog(file, content1);
                await WriteTextToLog(file, _preparedProgressionPath.ToArray());
                await WriteTextToLog(file, content2);
                await WriteTextToLog(file, _preparedDataboxes.ToArray());
            }

            LogHandler.Info("Wrote spoiler log to disk.");
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
