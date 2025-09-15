using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace SubnauticaRandomiser.Logic.LogicObjects
{
    /// <summary>
    /// Represents an abstract unlockable that can be found in a <see cref="Region"/>.
    /// </summary>
    [Serializable]
    public abstract class LogicEntity
    {
        /// <summary>
        /// The TechType this Entity interacts with, whether by unlocking, crafting, spawning, or whatever else.
        /// </summary>
        [JsonProperty]
        public readonly TechType TechType;

        /// <summary>
        /// These other Entities need to be in logic first in order for this Entity to be able to be randomised.
        /// </summary>
        public List<LogicEntity> Dependencies = new List<LogicEntity>();

        /// <summary>
        /// The relative importance of this entity compared to all others. 0 represents the very first and 1 the very
        /// last entity to be randomised.
        /// </summary>
        public float Priority;

        /// <summary>
        /// The <see cref="Sphere"/> this entity has been assigned to. A negative value indicates the entity has not
        /// been randomised yet.
        /// </summary>
        public int Sphere = -1;
    }
}