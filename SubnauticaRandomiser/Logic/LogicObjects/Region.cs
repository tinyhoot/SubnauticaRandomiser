using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using SubnauticaRandomiser.Logic.LogicObjects.Transitions;

namespace SubnauticaRandomiser.Logic.LogicObjects
{
    /// <summary>
    /// Represents an area in the game which contains <see cref="LogicEntity"/>s and is linked to other Regions via
    /// <see cref="Transitions"/>. The size of a region is arbitrary and can range from an entire biome to a single room.
    /// <br />
    /// Access to a region implicitly grants access to all the entities and biomes it contains.
    /// </summary>
    [Serializable]
    internal class Region
    {
        /// <summary>
        /// A unique identifying name.
        /// </summary>
        [JsonProperty]
        public readonly string Name;

        /// <summary>
        /// The list of transitions leading to other regions that can be reached from this region.
        /// </summary>
        public List<Transition> Transitions = new List<Transition>();

        /// <summary>
        /// Access to this region also unlocks access to all of these entities.
        /// </summary>
        public List<LogicEntity> Entities = new List<LogicEntity>();

        /// <summary>
        /// Access to this region corresponds to access to these vanilla biomes for spawning random loot.
        /// </summary>
        public List<BiomeType> BiomeTypes = new List<BiomeType>();
    }
}