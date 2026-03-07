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
        public readonly EntityManager EntityManager = new EntityManager();
        public readonly RegionManager RegionManager = new RegionManager();
        public readonly TravelDistanceManager TravelManager;
        public readonly List<PriorityRule> PriorityRules = new List<PriorityRule>();

        public RandoContext(Config config)
        {
            TravelManager = new TravelDistanceManager(config);
        }

        public void AddPriorityRules(IEnumerable<PriorityRule> rules)
        {
            PriorityRules.AddRange(rules);
        }
    }
}