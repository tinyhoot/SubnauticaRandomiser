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
        private List<Region> _regions;
        private List<Transition> _transitions;

        /// <summary>
        /// Get the region with the provided unique name.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown if no region with that name exists.</exception>
        public Region GetRegion(string name)
        {
            var region = _regions.Find(reg => reg.Name == name);
            if (region is null)
                throw new ArgumentException($"No region with name '{name}' exists!", nameof(name));
            return region;
        }

        public async Task ParseRegionsFromDisk(EntityManager manager)
        {
            try
            {
                _regions = await EntityManager.DeserializeLogicObjects<Region>(RegionsFile,
                    new StringEntityConverter(manager));
                _transitions = await EntityManager.DeserializeLogicObjects<Transition>(TransitionsFile,
                    new StringRegionConverter(_regions));
            }
            catch (Exception ex)
            {
                _log.Error($"{ex.GetType()}: {ex.Message}\n{ex.StackTrace}");
            }
        }
    }
}