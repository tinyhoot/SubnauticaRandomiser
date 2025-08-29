using System.Collections.Generic;

namespace SubnauticaRandomiser.Logic.LogicObjects
{
    internal class Region
    {
        /// <summary>
        /// A unique identifying name.
        /// </summary>
        public readonly string Name;

        /// <summary>
        /// The list of transitions leading to other regions that can be reached from this region.
        /// </summary>
        public List<Transition> Transitions = new List<Transition>();

        /// <summary>
        /// Access to this region also unlocks access to all of these entities.
        /// </summary>
        public List<LogicEntity> Entities = new List<LogicEntity>();
    }
}