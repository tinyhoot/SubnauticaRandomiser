using System;
using System.Collections.Generic;

namespace SubnauticaRandomiser
{
    public class Blueprint
    {
        public TechType TechType;
        public List<TechType> UnlockConditions;
        public int UnlockDepth;

        public Blueprint(TechType techType, List<TechType> unlockConditions = null, int unlockDepth = 0)
        {
            TechType = techType;
            UnlockConditions = unlockConditions;
            UnlockDepth = unlockDepth;
        }
    }
}
