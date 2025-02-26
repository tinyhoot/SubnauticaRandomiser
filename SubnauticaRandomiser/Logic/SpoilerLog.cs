using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HootLib;
using SubnauticaRandomiser.Handlers;
using SubnauticaRandomiser.Objects;
using SubnauticaRandomiser.Objects.Enums;
using SubnauticaRandomiser.Objects.Events;
using SubnauticaRandomiser.Serialization;
using SubnauticaRandomiser.Serialization.Modules;
using UnityEngine;
using ILogHandler = HootLib.Interfaces.ILogHandler;

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
        private ILogHandler _log;
        private readonly List<Tuple<TechType, int>> _progression = new List<Tuple<TechType, int>>();
        private string _spoilerDirectory;
        
        private string[] _contentHeader;
        private string[] _contentDataboxes;
        private string[] _contentFragments;
        private string[] _contentRecipes;

        private const string _DirName = "SpoilerLogs";
        private const string _ProgressionFileName = "intended_progression_spoilers.txt";
        private readonly Dictionary<EntityType, string> _EntityFileNames = new Dictionary<EntityType, string>
        {
            { EntityType.Databox, "databox_spoilers.txt" },
            { EntityType.Fragment, "fragment_spoilers.txt" },
            { EntityType.Craftable, "recipe_spoilers.txt" },
        };

        private void Awake()
        {
            _coreLogic = GetComponent<CoreLogic>();
            _manager = GetComponent<ProgressionManager>();
            _log = PrefixLogHandler.Get("[Spoiler]");
            _spoilerDirectory = GetSpoilerDirectory();
            PrepareStrings();

            // Register events.
            _coreLogic.MainLoopCompleted += OnMainLoopCompleted;
            _manager.DepthIncreased += OnDepthIncrease;
            _manager.HasProgressed += OnProgression;
        }

        /// <summary>
        /// Get a unique directory for the spoiler logs to avoid accidentally overwriting previous ones.
        /// </summary>
        private string GetSpoilerDirectory()
        {
            // Create a subdirectory with the current date and time as its name.
            string dateTime = DateTime.Now.ToString("yyyy-MM-dd---HH-mm-ss");
            return Path.Combine(Hootils.GetModDirectory(), _DirName, dateTime);
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
            _ = WriteLogFilesAsync(Bootstrap.SaveData);
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
            _contentHeader = new[]
            {
                "*************************************************",
                "*****   SUBNAUTICA RANDOMISER SPOILER LOG   *****",
                "*************************************************",
                "",
                "Generated on " + DateTime.Now + " with v" + Initialiser.VERSION,
                "",
                "",
            };
            _contentDataboxes = new[]
            {
                "///// Databox Locations /////",
                "",
            };
            _contentFragments = new[]
            {
                "///// Fragment Locations /////",
                "// Note: Biomes which end in _TechSite are the big, explorable wrecks.",
                "//       Biomes which end in _TechSite_Barrier are inside the wrecks, behind laser cutter doors.",
                "//       Biomes which end in _TechSite_Scatter are medium-sized Aurora debris out in the open.",
                "",
            };
            _contentRecipes = new[]
            {
                "///// Generated Recipes /////",
                "",
            };
        }

        /// <summary>
        /// Grab the randomised boxes from the serializer, and sort them alphabetically.
        /// </summary>
        /// <returns>The prepared log entries.</returns>
        private IEnumerable<string> GetDataboxes(DataboxSaveData saveData)
        {
            if (saveData.Databoxes is null)
                return new [] { "Not randomised, all in vanilla locations." };

            List<string> preparedDataboxes = new List<string>();
            foreach (Databox databox in saveData.Databoxes) 
            {
                preparedDataboxes.Add(databox.TechType.AsString() + " can be found at " + databox.Coordinates);
            }
            preparedDataboxes.Sort();

            return preparedDataboxes;
        }

        /// <summary>
        /// Grab the randomise fragments from the serializer, and sort them alphabetically.
        /// </summary>
        /// <returns>The prepared log entries.</returns>
        private IEnumerable<string> GetFragments(FragmentSaveData saveData)
        {
            if (saveData.SpawnDataDict is null || saveData.SpawnDataDict.Count == 0)
                return new[] { "Not randomised, all in vanilla locations." };

            List<string> preparedFragments = new List<string>();
            // Iterate through every TechType representing each fragment.
            foreach (var kv in saveData.SpawnDataDict)
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
            
            return preparedFragments;
        }

        /// <summary>
        /// Prepare a human readable way to tell what must be crafted to reach greater depths.
        /// </summary>
        /// <returns>The prepared log entries.</returns>
        private IEnumerable<string> GetProgressionPath()
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

            return preparedProgressionPath;
        }

        /// <summary>
        /// Prepare all recipes together with a list of their ingredients.
        /// </summary>
        /// <returns>The prepared log entries.</returns>
        private IEnumerable<string> GetRecipes(RecipeSaveData saveData)
        {
            if (saveData.RecipeDict is null || saveData.RecipeDict.Count == 0)
                return new[] { "Recipes were not modified." };

            List<string> preparedRecipes = new List<string>();
            var recipes = saveData.RecipeDict.OrderBy(kv => kv.Key.AsString());
            foreach (var kv in recipes)
            {
                preparedRecipes.Add(kv.Key.AsString());
                foreach (var ingredient in kv.Value.Ingredients)
                {
                    preparedRecipes.Add($" > {ingredient.amount} {ingredient.techType}");
                }
                preparedRecipes.Add("");
            }

            return preparedRecipes;
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
        /// Write the log files to disk.
        /// </summary>
        public async Task WriteLogFilesAsync(SaveData saveData)
        {
            Directory.CreateDirectory(_spoilerDirectory);

            using (StreamWriter file = new StreamWriter(Path.Combine(_spoilerDirectory, _ProgressionFileName)))
            {
                await WriteTextToLogAsync(file, _contentHeader, GetProgressionPath());
            }
            
            if (saveData.TryGetModuleData(out DataboxSaveData databoxes))
            {
                using StreamWriter file = new StreamWriter(Path.Combine(_spoilerDirectory, _EntityFileNames[EntityType.Databox]));
                await WriteTextToLogAsync(file, _contentHeader, _contentDataboxes, GetDataboxes(databoxes));
            }
            if (saveData.TryGetModuleData(out FragmentSaveData fragments))
            {
                using StreamWriter file = new StreamWriter(Path.Combine(_spoilerDirectory, _EntityFileNames[EntityType.Fragment]));
                await WriteTextToLogAsync(file, _contentHeader, _contentFragments, GetFragments(fragments));
            }
            if (saveData.TryGetModuleData(out RecipeSaveData recipes))
            {
                using StreamWriter file = new StreamWriter(Path.Combine(_spoilerDirectory, _EntityFileNames[EntityType.Craftable]));
                await WriteTextToLogAsync(file, _contentHeader, _contentRecipes, GetRecipes(recipes));
            }

            _log.Info("Wrote spoiler log to disk.");
        }

        private async Task WriteTextToLogAsync(StreamWriter file, params IEnumerable<string>[] content)
        {
            foreach (IEnumerable<string> text in content)
            {
                foreach (string line in text)
                {
                    await file.WriteLineAsync(line);
                }
            }
        }
    }
}
