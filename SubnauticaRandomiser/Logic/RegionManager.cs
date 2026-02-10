using System.Collections;
using System.Collections.Generic;
using SubnauticaRandomiser.Handlers;
using SubnauticaRandomiser.Logic.LogicObjects;
using SubnauticaRandomiser.Logic.LogicObjects.Transitions;
using SubnauticaRandomiser.Serialization;
using SubnauticaRandomiser.Serialization.Converters;
using UnityEngine;

namespace SubnauticaRandomiser.Logic
{
    internal class RegionManager
    {
        private const string RegionsFile = "regions.json";
        private const string TransitionsFile = "transitions.json";

        private PrefixLogHandler _log = PrefixLogHandler.Get("[RegionManager]");
        private List<Region> _regions = new List<Region>();
        private List<Transition> _transitions;
        private Dictionary<string, int> _regionIdMap = new Dictionary<string, int>();

        /// <summary>
        /// Get the region with the provided unique name.
        /// </summary>
        /// <exception cref="KeyNotFoundException">Thrown if no region with that name exists.</exception>
        public Region GetRegion(string name)
        {
            if (!_regionIdMap.TryGetValue(name, out int id))
                throw new KeyNotFoundException($"Tried to get nonexistent region with name '{name}'!");
            
            return GetRegion(id);
        }

        /// <summary>
        /// Get the region with the provided unique id.
        /// </summary>
        /// <exception cref="KeyNotFoundException">Thrown if no region with that name exists.</exception>
        public Region GetRegion(int id)
        {
            if (id < 0 || id >= _regions.Count)
                throw new KeyNotFoundException($"Tried to get nonexistent region with id {id}!");

            return _regions[id];
        }

        /// <summary>
        /// Load and parse regions and transitions from their files on disk.
        /// </summary>
        public IEnumerator ParseFromDiskAsync()
        {
            // Start loading all files' contents.
            var regionTask = SerdeUtils.ReadFileContents(RegionsFile);
            var transTask = SerdeUtils.ReadFileContents(TransitionsFile);
            yield return new WaitUntil(() => regionTask.IsCompleted && transTask.IsCompleted);

            var regions = new TaskResult<List<Region>>();
            yield return SerdeUtils.DeserializeObjectsAsync(regionTask.Result, regions, new StringEntityConverter());
            AddRegions(regions.Get());
            
            var transitions = new TaskResult<List<Transition>>();
            yield return SerdeUtils.DeserializeObjectsAsync(transTask.Result, transitions, new StringRegionConverter(this));
            _transitions = transitions.Get();
            LinkTransitions();
        }

        private void AddRegions(IEnumerable<Region> regions)
        {
            foreach (var region in regions)
            {
                // The numerical ID of an entity is its registration number.
                _regionIdMap[region.Name] = _regions.Count;
                _regions.Add(region);
            }
        }
        
        /// <summary>
        /// Update regions with all the transitions connected to them.
        /// </summary>
        private void LinkTransitions()
        {
            foreach (var trans in _transitions)
            {
                trans.Entry?.Transitions.Add(trans);
                trans.Exit?.Transitions.Add(trans);
            }
        }
        
        /// <summary>
        /// Try to replace all <see cref="LogicEntityReference"/> with the proper <see cref="LogicEntity"/>.
        /// </summary>
        public void ReplaceReferences(EntityManager manager)
        {
            foreach (var region in _regions)
            {
                if (region.Entities is null || region.Entities.Count == 0)
                    continue;
                
                for (int i = region.Entities.Count - 1; i >= 0; i--)
                {
                    if (region.Entities[i] is LogicEntityReference dep)
                    {
                        var replacement = manager.Find(dep.EntityType, dep.TechType);
                        if (replacement is null)
                        {
                            _log.Warn($"Failed to replace reference entity in dependencies of {region} --> {dep}");
                            region.Entities.RemoveAt(i);
                            continue;
                        }

                        region.Entities[i] = replacement;
                    }
                }
            }
        }
    }
}