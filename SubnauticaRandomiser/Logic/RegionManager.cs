using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SubnauticaRandomiser.Handlers;
using SubnauticaRandomiser.Logic.LogicObjects;
using SubnauticaRandomiser.Logic.LogicObjects.Transitions;
using SubnauticaRandomiser.Serialization.Converters;

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

        public async Task ParseRegionsFromDisk()
        {
            try
            {
                var regions = await EntityManager.DeserializeLogicObjects<Region>(RegionsFile,
                    new StringEntityConverter());
                AddRegions(regions);
                _transitions = await EntityManager.DeserializeLogicObjects<Transition>(TransitionsFile,
                    new StringRegionConverter(this));
            }
            catch (Exception ex)
            {
                _log.Error($"{ex.GetType()}: {ex.Message}\n{ex.StackTrace}");
            }
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