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
        private static List<string> _preparedProgressionPath = new List<string>();

        private static string[] content = {
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

        private static void PrepareProgressionPath()
        {
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
            PrepareProgressionPath();

            using (StreamWriter file = new StreamWriter(Path.Combine(InitMod.s_modDirectory, s_fileName)))
            {
                foreach (string line in content)
                {
                    await file.WriteLineAsync(line);
                }

                foreach (string line in _preparedProgressionPath)
                {
                    await file.WriteLineAsync(line);
                }
            }
        }
    }
}
