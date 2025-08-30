using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace SubnauticaRandomiser.Logic.LogicObjects.Transitions
{
    /// <summary>
    /// Represents a connection between two <see cref="Region"/>s.
    /// </summary>
    [Serializable]
    internal class Transition
    {
        /// <summary>
        /// The unique name of this transition.
        /// </summary>
        [JsonProperty] public readonly string Name;
        
        /// <summary>
        /// The region this transition originates from.
        /// </summary>
        public Region Entry;
        
        /// <summary>
        /// The region this transition leads to.
        /// </summary>
        public Region Exit;

        /// <summary>
        /// The locks that prevent passage through this transition until resolved.
        /// </summary>
        [JsonProperty(ItemTypeNameHandling = TypeNameHandling.All)]
        public List<TransitionLock> Locks;

        public bool IsUnlocked()
        {
            // What can it be?
            // Laser cutter, Propulsion cannon, Teleporter IonCrystal, PrecursorKeys
            // *Also* depth/distance from the nearest reachable Region
            // Enum for lock type?
            throw new NotImplementedException();
        }
    }
}