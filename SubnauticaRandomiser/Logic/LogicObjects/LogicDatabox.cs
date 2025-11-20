using System.Collections.Generic;

namespace SubnauticaRandomiser.Logic.LogicObjects
{
    /// <summary>
    /// Represents physical databoxes out in the world that unlock a recipe for crafting.
    /// </summary>
    internal class LogicDatabox : LogicBlueprint
    {
        /// <summary>
        /// The positions of all databoxes that contain a blueprint of this TechType.
        /// </summary>
        public List<RegionNode> DataboxPositions = new List<RegionNode>();
    }
}