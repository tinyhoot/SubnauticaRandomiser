using System.Collections.Generic;
using SubnauticaRandomiser.Configuration;
using SubnauticaRandomiser.Logic.LogicObjects;

namespace SubnauticaRandomiser.Logic
{
    /// <summary>
    /// Convenience class to keep method signatures from ballooning. Grants access to the many manager-like objects
    /// necessary for the logic.
    /// <br />
    /// On an abstract level, contains all the informational context within which randomising happens.
    /// </summary>
    internal class RandoContext
    {
        public EntityManager EntityManager = new EntityManager();
        public RegionManager RegionManager = new RegionManager();
        public TravelDistanceManager TravelManager;
        public List<PriorityRule> PriorityRules = new List<PriorityRule>();

        public RandoContext(Config config)
        {
            TravelManager = new TravelDistanceManager(config);
        }
    }
}