using System.Collections.Generic;
using SubnauticaRandomiser.Logic.LogicObjects;
using LogicEntity = SubnauticaRandomiser.Objects.LogicEntity;

namespace SubnauticaRandomiser.Logic
{
    /// <summary>
    /// Represents the context in which randomising takes place. The data in this class determines the initial state
    /// of <see cref="Sphere"/> 0.
    /// </summary>
    internal class RandomisationContext
    {
        public Region StartingRegion;
        public List<LogicEntity> StartingEntities;

        public RandomisationContext(Region start)
        {
            StartingRegion = start;
        }
    }
}