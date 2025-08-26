using System;
using System.Collections.Generic;
using SubnauticaRandomiser.Interfaces;

namespace SubnauticaRandomiser.Serialization.Modules.EntitySlots
{
    /// <summary>
    /// Represents a prefab that can spawn in an <see cref="EntitySlot"/> and acts as a bridge between TechType and
    /// underlying prefab path data needed for the final object to be created.
    /// </summary>
    [Serializable]
    internal class Spawnable
    {
        public TechType TechType;
        public List<string> ClassIds = new List<string>();
        public EntitySlotData.EntitySlotType SlotType;
        
        public void AddClassId(string id)
        {
            ClassIds.Add(id);
        }

        /// <summary>
        /// Get a random prefab from all classIds associated with this Spawnable.
        /// </summary>
        public string GetRandomPrefab(IRandomHandler rng)
        {
            return rng.Choice(ClassIds);
        }
    }
}