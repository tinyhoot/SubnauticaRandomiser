using System.Collections.Generic;

namespace SubnauticaRandomiser.Logic.LogicObjects
{
    /// <summary>
    /// Represents an entity that spawns in the world, like fragments or raw materials.
    /// </summary>
    internal class LogicSpawnable : LogicEntity
    {
        /// <summary>
        /// The biomes this entity should spawn in.
        /// </summary>
        public List<BiomeType> Spawns = new List<BiomeType>();
    }
}