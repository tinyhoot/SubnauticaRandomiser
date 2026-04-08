using System.Collections.Generic;

namespace SubnauticaRandomiser.Logic.LogicObjects
{
    /// <summary>
    /// Represents an entity that spawns in the world, like fragments or raw materials.
    /// </summary>
    internal class LogicSpawnable : LogicEntity
    {
        /// <summary>
        /// The internal classIds used by the game to spawn any variations of this entity.
        /// <br />
        /// For fragments, this may be used for either the random spawner found in WorldEntities/Fragments or the
        /// specific variants in WorldEntities/Environment/Wrecks. It is best to use whichever id is already present in
        /// the vanilla game.
        /// </summary>
        public List<string> ClassIds = new List<string>();
        
        /// <summary>
        /// The biomes this entity should spawn in.
        /// </summary>
        public List<BiomeType> Spawns = new List<BiomeType>();
    }
}