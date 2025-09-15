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

        private bool _unlocked;

        public bool IsUnlocked()
        {
            return _unlocked;
        }

        public bool CheckLocks()
        {
            // There can be no backwards progress. Locks that are open stay open.
            if (_unlocked)
                return true;
            
            if (Locks is null || Locks.Count == 0)
            {
                _unlocked = true;
                return true;
            }

            if (Locks.TrueForAll(l => l.CheckUnlocked()))
            {
                _unlocked = true;
                return true;
            }

            return false;
        }
    }
}