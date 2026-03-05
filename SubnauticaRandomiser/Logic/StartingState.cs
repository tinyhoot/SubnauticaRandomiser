using System.Collections.Generic;
using SubnauticaRandomiser.Logic.LogicObjects;

namespace SubnauticaRandomiser.Logic
{
    /// <summary>
    /// Represents the initial state of <see cref="Sphere"/> 0.
    /// </summary>
    internal class StartingState
    {
        public Region StartingRegion;
        public List<LogicEntity> StartingEntities = new List<LogicEntity>();

        public StartingState(Region start)
        {
            StartingRegion = start;
            StartingEntities.AddRange(StartingRegion.Entities);
        }
    }
}