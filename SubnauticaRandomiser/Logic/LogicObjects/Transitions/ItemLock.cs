using System;

namespace SubnauticaRandomiser.Logic.LogicObjects.Transitions
{
    internal class ItemLock : TransitionLock
    {
        public TechType RequiredItem;
        
        public override bool IsUnlocked()
        {
            throw new NotImplementedException();
        }
    }
}