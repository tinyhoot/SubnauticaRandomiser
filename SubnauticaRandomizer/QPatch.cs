using Harmony;
using Oculus.Newtonsoft.Json;
using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using SubnauticaRandomizer.Randomizer;

namespace SubnauticaRandomizer
{
    public class QPatch
    {
        public static void Patch()
        {
            ManageSettingsFile();

            HarmonyInstance harmony = HarmonyInstance.Create("theah.subnauticarandomizer");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        public static string GetSubnauticaRandomizerDirectory()
        {
            string modDirectory = Path.Combine(Environment.CurrentDirectory, "QMods");
            return Path.Combine(modDirectory, "SubnauticaRandomizer");
        }

        private static void ManageSettingsFile()
        {
            string settingsPath = Path.Combine(GetSubnauticaRandomizerDirectory(), "config.json");

            if (File.Exists(settingsPath))
            {
                Settings.Instance = JsonConvert.DeserializeObject<Settings>(File.ReadAllText(settingsPath));
            }
            else
            {
                Settings.Instance = new Settings();
            }

            if (string.IsNullOrEmpty(Settings.Instance.RecipeSeed) && Settings.Instance.RandomizeMe)
            {
                var recipes = RecipeRandomizer.Randomize(QPatch.GetSubnauticaRandomizerDirectory());

                if (recipes.RecipesByType.Values.Count > 0)
                {
                    Settings.Instance.RecipeSeed = recipes.ToBase64String();
                    WriteSettingsFile(settingsPath);
                }
                Settings.Instance.Recipes = recipes;
            }
            else if (Settings.Instance.RandomizeMe)
            {
                try
                {
                    Settings.Instance.Recipes = Recipes.FromBase64String(Settings.Instance.RecipeSeed);
                }
                catch(Exception ex)
                {
                    LogError(ex.ToString());
                    Settings.Instance.Recipes = new Recipes();
                }
                WriteSettingsFile(settingsPath);
            }
            else
            {
                Settings.Instance.Recipes = new Recipes();
            }

            Settings.Instance.Initialize();
        }

        private static void WriteSettingsFile(string path)
        {
            File.WriteAllText(path, JsonConvert.SerializeObject(Settings.Instance, Formatting.Indented));
        }

        public static void LogError(string text)
        {
            var logFile = Path.Combine(GetSubnauticaRandomizerDirectory(), "log.txt");
            File.AppendAllText(logFile, "\r\n" + text);
        }
    }
}
