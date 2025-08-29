using System.Collections.Generic;

namespace SubnauticaRandomiser.Logic.LogicObjects
{
    /// <summary>
    /// Represents an abstract unlockable that can be found in a <see cref="Region"/>.
    /// </summary>
    internal abstract class LogicEntity
    {
        /// <summary>
        /// The TechType this Entity interacts with, whether by unlocking, crafting, spawning, or whatever else.
        /// </summary>
        public readonly TechType TechType;

        /// <summary>
        /// These other Entities need to be in logic first in order for this Entity to be able to be randomised.
        /// </summary>
        public List<LogicEntity> Dependencies = new List<LogicEntity>();
    }
}