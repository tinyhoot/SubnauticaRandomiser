using System;
using System.Collections.Generic;

namespace SubnauticaRandomiser.RandomiserObjects
{
    /// <summary>
    /// A class representing the knowledge required for an entity to appear in the player's PDA.
    /// </summary>
    [Serializable]
    public class Blueprint
    {
        public TechType TechType;
        public List<TechType> UnlockConditions;
        public List<TechType> Fragments;
        public bool NeedsDatabox;
        public int UnlockDepth;

        public Blueprint(TechType techType, List<TechType> unlockConditions = null, TechType fragment = TechType.None, bool databox = false, int unlockDepth = 0)
        {
            Fragments = new List<TechType>();

            TechType = techType;
            UnlockConditions = unlockConditions;
            Fragments.Add(fragment);
            NeedsDatabox = databox;
            UnlockDepth = unlockDepth;
        }

        public Blueprint(TechType techType, List<TechType> unlockConditions = null, List<TechType> fragments = null, bool databox = false, int unlockDepth = 0)
        {
            TechType = techType;
            UnlockConditions = unlockConditions;
            Fragments = fragments;
            NeedsDatabox = databox;
            UnlockDepth = unlockDepth;
        }
    }
}
