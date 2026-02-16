using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using SubnauticaRandomiser.Serialization.Converters;

namespace SubnauticaRandomiser.Logic.LogicObjects
{
    /// <summary>
    /// Represents an abstract unlockable that can be found in a <see cref="Region"/>.
    /// </summary>
    [Serializable]
    public abstract class LogicEntity : IComparable<LogicEntity>
    {
        /// <summary>
        /// The separation character used in string representations of an entity.
        /// </summary>
        public const char TypeNameSeparator = ':';
        
        /// <summary>
        /// The TechType this Entity interacts with, whether by unlocking, crafting, spawning, or whatever else.
        /// </summary>
        [JsonProperty]
        public readonly TechType TechType;

        /// <summary>
        /// A number of tags that convey additional information about this entity. A tag could e.g. mark an entity as
        /// an egg, or as a base piece.
        /// </summary>
        public List<string> Tags = new List<string>();

        /// <summary>
        /// These other Entities need to be in logic first in order for this Entity to be able to be randomised.
        /// </summary>
        [JsonConverter(typeof(StringEntityConverter))]
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

        protected LogicEntity(){}

        protected LogicEntity(TechType techType)
        {
            TechType = techType;
        }

        /// <summary>
        /// Used for sorting operations. Sorting happens based on priority, preferring lowest.
        /// </summary>
        public int CompareTo(LogicEntity other)
        {
            return Priority.CompareTo(other.Priority);
        }

        public override string ToString()
        {
            return GetType().Name + TypeNameSeparator + TechType.AsString();
        }
    }
}