using System;
using System.Collections.Generic;

namespace SubnauticaRandomiser
{
    [Serializable]
    public class Blueprint
    {
        public TechType TechType;
        public List<TechType> UnlockConditions;
        public TechType Fragment;
        public bool NeedsDatabox;
        public int UnlockDepth;

        public Blueprint(TechType techType, List<TechType> unlockConditions = null, TechType fragment = TechType.None, bool databox = false, int unlockDepth = 0)
        {
            TechType = techType;
            UnlockConditions = unlockConditions;
            Fragment = fragment;
            NeedsDatabox = databox;
            UnlockDepth = unlockDepth;
        }
    }
}
