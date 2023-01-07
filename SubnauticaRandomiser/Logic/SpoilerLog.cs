using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using SubnauticaRandomiser.Interfaces;
using SubnauticaRandomiser.Objects;

namespace SubnauticaRandomiser.Logic
{
    /// <summary>
    /// Handles everything related to the spoiler log generated during randomisation.
    /// </summary>
    internal class SpoilerLog
    {
        private const string _FileName = "spoilerlog.txt";
        private readonly RandomiserConfig _config;
        private readonly ILogHandler _log;
        private readonly EntitySerializer _serializer;
        private readonly List<KeyValuePair<TechType, int>> _progression = new List<KeyValuePair<TechType, int>>();

        private List<string> _basicOptions;
        private string[] _contentHeader;
        private string[] _contentBasics;
        private string[] _contentAdvanced;
        private string[] _contentDataboxes;
        private string[] _contentFragments;

        public SpoilerLog(RandomiserConfig config, ILogHandler logger, EntitySerializer serializer)
        {
            _config = config;
            _log = logger;
            _serializer = serializer;
        }
        
        /// <summary>
        /// Prepare the more basic aspects of the log.
        /// </summary>
        private void PrepareStrings()
        {
            _basicOptions = new List<string>()
            {
                "iSeed",
                "iRandomiserMode",
                "bUseFish", "bUseEggs", "bUseSeeds",
                "bRandomiseDataboxes",
                "bRandomiseFragments",
                "bRandomiseNumFragments", "iMaxFragmentsToUnlock",
                "bRandomiseDuplicateScans",
                "bRandomiseRecipes",
                "bVanillaUpgradeChains",
                "bDoBaseTheming",
                "iEquipmentAsIngredients", "iToolsAsIngredients", "iUpgradesAsIngredients",
                "iMaxIngredientsPerRecipe", "iMaxAmountPerIngredient",
                "bMaxBiomesPerFragments"
            };
            _contentHeader = new[]
            {
                "*************************************************",
                "*****   SUBNAUTICA RANDOMISER SPOILER LOG   *****",
                "*************************************************",
                "",
                "Generated on " + DateTime.Now + " with " + Initialiser.VERSION
            };
            _contentBasics = new[]
            {
                "",
                "",
                "///// Basic Information /////",
                "Seed: " + _config.iSeed,
                "Mode: " + _config.iRandomiserMode,
                "Spawnpoint: " + _config.sSpawnPoint,
                "Fish, Eggs, Seeds: " + _config.bUseFish + ", " + _config.bUseEggs + ", " + _config.bUseSeeds,
                "Random Databoxes: " + _config.bRandomiseDataboxes,
                "Random Fragments: " + _config.bRandomiseFragments,
                "Random Fragment numbers: " + _config.bRandomiseNumFragments + ", " + _config.iMaxFragmentsToUnlock,
                "Random Duplicate Scan Rewards: " + _config.bRandomiseDuplicateScans,
                "Random Recipes: " + _config.bRandomiseRecipes,
                "Vanilla Upgrade Chains: " + _config.bVanillaUpgradeChains,
                "Base Theming: " + _config.bDoBaseTheming,
                "Equipment, Tools, Upgrades: " + _config.iEquipmentAsIngredients + ", " + _config.iToolsAsIngredients + ", " + _config.iUpgradesAsIngredients,
                "Max Ingredients: " + _config.iMaxIngredientsPerRecipe + " per recipe, " + _config.iMaxAmountPerIngredient + " per ingredient",
                "Max Biomes per Fragment: " + _config.iMaxBiomesPerFragment,
                ""
            };
            _contentAdvanced = new[]
            {
                "",
                "",
                "///// Depth Progression Path /////"
            };
            _contentDataboxes = new[]
            {
                "",
                "",
                "///// Databox Locations /////"
            };
            _contentFragments = new[]
            {
                "",
                "",
                "///// Fragment Locations /////"
            };
        }
        
        /// <summary>
        /// Add advanced settings to the spoiler log, but only if they have been modified.
        /// </summary>
        /// <returns>An array of modified settings.</returns>
        private string[] PrepareAdvancedSettings()
        {
            List<string> preparedAdvSettings = new List<string>();
            FieldInfo[] fieldInfoArray = typeof(RandomiserConfig).GetFields(BindingFlags.Public | BindingFlags.Instance);
            
            foreach (FieldInfo field in fieldInfoArray)
            {
                // Check whether this field is an advanced config option or not.
                if (_basicOptions.Contains(field.Name))
                    continue;
                if (!ConfigDefaults.Contains(field.Name))
                    continue;
                
                var userValue = field.GetValue(_config);
                var defaultValue = ConfigDefaults.GetDefault(field.Name);
                // If the value of a config field does not correspond to its default value, the user must have
                // modified it. Add it to the list in that case.
                if (!userValue.Equals(defaultValue))
                    preparedAdvSettings.Add(field.Name + ": " + userValue);
            }
            _log.Debug("Added anomalies: " + preparedAdvSettings.Count);

            if (preparedAdvSettings.Count == 0)
                preparedAdvSettings.Add("No advanced settings were modified.");

            return preparedAdvSettings.ToArray();
        }
        
        /// <summary>
        /// Grab the randomised boxes from masterDict, and sort them alphabetically.
        /// </summary>
        /// <returns>The prepared log entries.</returns>
        private string[] PrepareDataboxes()
        {
            if (_serializer.Databoxes is null)
                return new [] { "Not randomised, all in vanilla locations." };

            List<string> preparedDataboxes = new List<string>();
            foreach (KeyValuePair<RandomiserVector, TechType> entry in _serializer.Databoxes) 
            {
                preparedDataboxes.Add(entry.Value.AsString() + " can be found at " + entry.Key);
            }
            preparedDataboxes.Sort();

            return preparedDataboxes.ToArray();
        }

        /// <summary>
        /// Grab the randomise fragments from masterDict, and sort them alphabetically.
        /// </summary>
        /// <returns>The prepared log entries.</returns>
        private string[] PrepareFragments()
        {
            if (_serializer.SpawnDataDict is null || _serializer.SpawnDataDict.Count == 0)
                return new[] { "Not randomised, all in vanilla locations." };

            List<string> preparedFragments = new List<string>();
            // Iterate through every TechType representing each fragment.
            foreach (var kv in _serializer.SpawnDataDict)
            {
                string line = kv.Key.AsString() + ": ";
                // Fragments are split up into their respective prefabs, but those all have the same spawn biomes
                // and can be neglected. Just take the first prefab's biome spawns directly.
                foreach (var biomeData in kv.Value[0].BiomeDataList)
                {
                    line += biomeData.Biome.AsString() + ", ";
                }
                preparedFragments.Add(line);
            }
            preparedFragments.Sort();
            
            return preparedFragments.ToArray();
        }
        
        /// <summary>
        /// Compare the MD5 of the recipe CSV and try to see if it's still the same.
        /// Since this is done while parsing the CSV anyway, grab the value from there.
        /// </summary>
        /// <returns>The prepared log entry.</returns>
        private string PrepareMD5()
        {
            if (!Initialiser._ExpectedRecipeMD5.Equals(CSVReader.s_recipeCSVMD5))
                return "recipeInformation.csv has been modified: " + CSVReader.s_recipeCSVMD5;
            
            return "recipeInformation.csv is unmodified.";
        }
        
        /// <summary>
        /// Prepare a human readable way to tell what must be crafted to reach greater depths.
        /// </summary>
        /// <returns>The prepared log entries.</returns>
        private string[] PrepareProgressionPath()
        {
            // When recipes are not randomised, this spoiler hint does more or less nothing; go with default value.
            if (_progression.Count == 0)
                return new[] { "Vanilla" };
            
            List <string> preparedProgressionPath = new List<string>();
            int lastDepth = 0;

            foreach (KeyValuePair<TechType, int> pair in _progression)
            {
                if (pair.Value > lastDepth)
                    preparedProgressionPath.Add("Craft " + pair.Key.AsString() + " to reach " + pair.Value + "m");
                else
                    preparedProgressionPath.Add("Unlocked " + pair.Key.AsString() + ".");

                lastDepth = pair.Value;
            }

            return preparedProgressionPath.ToArray();
        }

        /// <summary>
        /// Add an entry for a progression item to the spoiler log.
        /// </summary>
        /// <param name="type">The progression item.</param>
        /// <param name="depth">The depth it unlocks or was unlocked at.</param>
        /// <returns>True if successful, false if the entry already exists.</returns>
        public bool AddProgressionEntry(TechType type, int depth)
        {
            if (_progression.Exists(x => x.Key.Equals(type)))
            {
                _log.Warn("Tried to add duplicate progression item to spoiler log: " + type.AsString());
                return false;
            }
            
            var kvpair = new KeyValuePair<TechType, int>(type, depth);
            _progression.Add(kvpair);
            return true;
        }
        
        /// <summary>
        /// When a progression item first gets unlocked, its depth reflects the depth required to reach it, rather than
        /// what it makes accessible; update that here.
        /// Always changes the latest addition to the spoiler log.
        /// </summary>
        /// <param name="depth">The new depth to update the entry with.</param>
        /// <returns>True if successful, false if the update failed, e.g. if there are no entries in the list.</returns>
        public bool UpdateLastProgressionEntry(int depth)
        {
            if (_progression.Count == 0)
                return false;

            // Since this is a list of immutable k-v pairs, it must be removed and replaced entirely.
            TechType type = _progression[_progression.Count - 1].Key;
            _progression.RemoveAt(_progression.Count - 1);
            return AddProgressionEntry(type, depth);
        }

        /// <summary>
        /// Write the log to disk.
        /// </summary>
        public async Task WriteLog()
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
            
            lines.AddRange(_contentFragments);
            lines.AddRange(PrepareFragments());

            using (StreamWriter file = new StreamWriter(Path.Combine(Initialiser._ModDirectory, _FileName)))
            {
                await WriteTextToLog(file, lines.ToArray());
            }

            _log.Info("Wrote spoiler log to disk.");
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
