using System;
using System.Collections.Generic;
using System.IO;
using SubnauticaRandomiser.Logic;

namespace SubnauticaRandomiser.Objects
{
    /// <summary>
    /// A class representing the knowledge required for an entity to appear in the player's PDA.
    /// </summary>
    [Serializable]
    internal class Blueprint
    {
        public TechType TechType;
        public List<TechType> UnlockConditions;
        public List<TechType> Fragments;
        public int NumFragments;
        public bool NeedsDatabox;
        public int UnlockDepth;

        public Blueprint(TechType techType)
        {
            TechType = techType;
        }

        public Blueprint(TechType techType, List<TechType> unlockConditions = null, int numFragments = 3,
            bool databox = false, int unlockDepth = 0)
        {
            Fragments = new List<TechType>();

            TechType = techType;
            UnlockConditions = unlockConditions;
            
            NumFragments = numFragments;
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
