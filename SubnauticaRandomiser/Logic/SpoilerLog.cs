using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HootLib;
using SubnauticaRandomiser.Handlers;
using SubnauticaRandomiser.Logic.LogicObjects;
using SubnauticaRandomiser.Objects.Enums;
using SubnauticaRandomiser.Serialization;
using SubnauticaRandomiser.Serialization.Modules;
using UnityEngine;
using ILogHandler = HootLib.Interfaces.ILogHandler;
using LogicEntity = SubnauticaRandomiser.Logic.LogicObjects.LogicEntity;

namespace SubnauticaRandomiser.Logic
{
    /// <summary>
    /// Keeps track of events and progress during randomisation and writes a spoilerlog to disk at the end.
    /// </summary>
    internal class SpoilerLog
    {
        private readonly ILogHandler _log = PrefixLogHandler.Get("[Spoiler]");
        private readonly List<Progress> _progression = new List<Progress>();
        private StartingState _startingState;
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

        public SpoilerLog(LogicMonitor monitor)
        {
            _spoilerDirectory = GetSpoilerDirectory();
            PrepareStrings();
            
            monitor.StartingStateCreated += OnStartingStateCreated;
            monitor.SphereCreated += OnSphereCreated;
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

        private void OnStartingStateCreated(StartingState state)
        {
            _startingState = state;
        }

        private void OnSphereCreated(Sphere sphere)
        {
            // Add a dummy in place of sphere zero.
            if (sphere.Tier == 1)
                _progression.Add(new Progress { TotalRegions = 1 });
            
            int lastRegions = _progression.Last().TotalRegions;
            _progression.Add(new Progress
                {
                    KeyEntity = sphere.Entities.Last(),
                    Sphere = sphere,
                    NewRegions = sphere.Regions.Count - lastRegions,
                    TotalRegions = sphere.Regions.Count
                }
            );
        }

        public IEnumerator WriteSpoilerLog(SaveData saveData)
        {
            var task = WriteLogFilesAsync(saveData);
            yield return new WaitUntil(() => task.IsCompleted);
            if (task.IsFaulted)
                throw task.Exception!;
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
            foreach (DataboxSaveData.Databox databox in saveData.Databoxes) 
            {
                preparedDataboxes.Add(databox.TechType.AsString() + " can be found at " + databox.Position);
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
        /// Prepare a human-readable way to tell what must be crafted/gathered/found to progress.
        /// </summary>
        private IEnumerable<string> GetProgressionPath()
        {
            List<string> preparedProgressionPath = new List<string>();

            foreach (var progress in _progression)
            {
                if (progress.Sphere is null)
                    continue;
                
                // TODO: Add start, add end goal, also insert any entities that are of note along the way.
                preparedProgressionPath.Add($"Get {progress.KeyEntity}");
                preparedProgressionPath.Add($"{progress.NewRegions} new regions unlocked.");
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
        /// Write the log files to disk.
        /// </summary>
        private async Task WriteLogFilesAsync(SaveData saveData)
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

        private struct Progress
        {
            public Sphere Sphere;
            public LogicEntity KeyEntity;
            public int NewRegions;
            public int TotalRegions;
        }
    }
}
