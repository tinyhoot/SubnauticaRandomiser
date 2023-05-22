using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using SubnauticaRandomiser.Configuration;
using SubnauticaRandomiser.Objects;
using SubnauticaRandomiser.Objects.Events;
using UnityEngine;
using ILogHandler = SubnauticaRandomiser.Interfaces.ILogHandler;

namespace SubnauticaRandomiser.Logic
{
    /// <summary>
    /// Keeps track of events and progress during randomisation and writes a spoilerlog to disk at the end.
    /// </summary>
    [RequireComponent(typeof(CoreLogic), typeof(ProgressionManager))]
    internal class SpoilerLog : MonoBehaviour
    {
        private CoreLogic _coreLogic;
        private ProgressionManager _manager;
        private Config _config;
        private ILogHandler _log;
        private readonly List<Tuple<TechType, int>> _progression = new List<Tuple<TechType, int>>();

        private List<string> _basicOptions;
        private string[] _contentHeader;
        private string[] _contentBasics;
        private string[] _contentAdvanced;
        private string[] _contentDataboxes;
        private string[] _contentFragments;
        
        private const string _FileName = "spoilerlog.txt";

        private void Awake()
        {
            _coreLogic = GetComponent<CoreLogic>();
            _manager = GetComponent<ProgressionManager>();
            _config = _coreLogic._Config;
            _log = _coreLogic._Log;

            // Register events.
            _coreLogic.MainLoopCompleted += OnMainLoopCompleted;
            _manager.DepthIncreased += OnDepthIncrease;
            _manager.HasProgressed += OnProgression;
        }

        /// <summary>
        /// When a progression entity causes the reachable depth to increase, update the responsible entity.
        /// </summary>
        private void OnDepthIncrease(object sender, EntityEventArgs args)
        {
            UpdateLastProgressionEntry(_manager.ReachableDepth);
        }

        /// <summary>
        /// Once the main loop completes, all randomising is over. Write the spoiler log to disk.
        /// </summary>
        private void OnMainLoopCompleted(object sender, EventArgs args)
        {
            // Start writing and discard the Task. Gets rid of a compiler warning.
            _ = WriteLogAsync(CoreLogic._Serializer);
        }

        /// <summary>
        /// Log every progression entity as soon as it unlocks.
        /// </summary>
        private void OnProgression(object sender, EntityEventArgs args)
        {
            AddProgressionEntry(args.LogicEntity.TechType, _manager.ReachableDepth);
        }

        private void AddProgressionEntry(TechType techType, int depth)
        {
            Tuple<TechType, int> entry = new Tuple<TechType, int>(techType, depth);
            _progression.Add(entry);
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
                "Seed: " + _config.Seed.Value,
                "Mode: " + _config.RecipeMode.Value,
                "Spawnpoint: " + _config.SpawnPoint.Value,
                "Fish, Eggs, Seeds: " + _config.UseFish.Value + ", " + _config.UseEggs.Value + ", " + _config.UseSeeds.Value,
                "Random Databoxes: " + _config.RandomiseDataboxes.Value,
                "Random Fragments: " + _config.RandomiseFragments.Value,
                "Random Fragment numbers: " + _config.RandomiseNumFragments.Value + ", " + _config.MaxFragmentsToUnlock.Value,
                "Random Duplicate Scan Rewards: " + _config.RandomiseDuplicateScans.Value,
                "Random Recipes: " + _config.RandomiseRecipes.Value,
                "Vanilla Upgrade Chains: " + _config.VanillaUpgradeChains.Value,
                "Base Theming: " + _config.BaseTheming.Value,
                "Equipment, Tools, Upgrades: " + _config.EquipmentAsIngredients.Value + ", " + _config.ToolsAsIngredients.Value + ", " + _config.UpgradesAsIngredients.Value,
                "Max Ingredients: " + _config.MaxIngredientsPerRecipe.Value + " per recipe, " + _config.MaxNumberPerIngredient.Value + " per ingredient",
                "Max Biomes per Fragment: " + _config.MaxBiomesPerFragment.Value,
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
                "///// Fragment Locations /////",
                "// Note: Biomes which end in _TechSite are the big, explorable wrecks.",
                "//       Biomes which end in _TechSite_Barrier are inside the wrecks, behind laser cutter doors.",
                "//       Biomes which end in _TechSite_Scatter are medium-sized Aurora debris out in the open."
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
        /// Grab the randomised boxes from the serializer, and sort them alphabetically.
        /// </summary>
        /// <returns>The prepared log entries.</returns>
        private string[] PrepareDataboxes(EntitySerializer serializer)
        {
            if (serializer.Databoxes is null)
                return new [] { "Not randomised, all in vanilla locations." };

            List<string> preparedDataboxes = new List<string>();
            foreach (KeyValuePair<RandomiserVector, TechType> entry in serializer.Databoxes) 
            {
                preparedDataboxes.Add(entry.Value.AsString() + " can be found at " + entry.Key);
            }
            preparedDataboxes.Sort();

            return preparedDataboxes.ToArray();
        }

        /// <summary>
        /// Grab the randomise fragments from the serializer, and sort them alphabetically.
        /// </summary>
        /// <returns>The prepared log entries.</returns>
        private string[] PrepareFragments(EntitySerializer serializer)
        {
            if (serializer.SpawnDataDict is null || serializer.SpawnDataDict.Count == 0)
                return new[] { "Not randomised, all in vanilla locations." };

            List<string> preparedFragments = new List<string>();
            // Iterate through every TechType representing each fragment.
            foreach (var kv in serializer.SpawnDataDict)
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

            foreach (Tuple<TechType, int> pair in _progression)
            {
                if (pair.Item2 > lastDepth)
                    preparedProgressionPath.Add($"Craft {pair.Item1} to reach {pair.Item2}m");
                else
                    preparedProgressionPath.Add($"Unlocked {pair.Item1}.");

                lastDepth = pair.Item2;
            }

            return preparedProgressionPath.ToArray();
        }

        /// <summary>
        /// When a progression item first gets unlocked, its depth reflects the depth required to reach it, rather than
        /// what it makes accessible; update that here.
        /// Always changes the latest addition to the spoiler log.
        /// </summary>
        /// <param name="depth">The new depth to update the entry with.</param>
        private void UpdateLastProgressionEntry(int depth)
        {
            if (_progression.Count == 0)
                return;

            // Since this is a list of immutable tuples, it must be removed and replaced entirely.
            TechType type = _progression[_progression.Count - 1].Item1;
            _progression.RemoveAt(_progression.Count - 1);
            AddProgressionEntry(type, depth);
        }

        /// <summary>
        /// Write the log to disk.
        /// </summary>
        public async Task WriteLogAsync(EntitySerializer serializer)
        {
            List<string> lines = new List<string>();
            PrepareStrings();

            lines.AddRange(_contentHeader);
            // lines.Add(PrepareMD5());
            
            lines.AddRange(_contentBasics);
            lines.AddRange(PrepareAdvancedSettings());
            lines.AddRange(_contentAdvanced);
            
            lines.AddRange(PrepareProgressionPath());
            lines.AddRange(_contentDataboxes);
            lines.AddRange(PrepareDataboxes(serializer));
            
            lines.AddRange(_contentFragments);
            lines.AddRange(PrepareFragments(serializer));

            using (StreamWriter file = new StreamWriter(Path.Combine(Initialiser._ModDirectory, _FileName)))
            {
                await WriteTextToLog(file, lines.ToArray());
            }

            _log.Info("Wrote spoiler log to disk.");
        }

        private async Task WriteTextToLog(StreamWriter file, IEnumerable<string> text)
        {
            foreach (string line in text)
            {
                await file.WriteLineAsync(line);
            }
        }
    }
}
