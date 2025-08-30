using System;

namespace SubnauticaRandomiser.Logic.LogicObjects.Transitions
{
    internal class DepthLock : TransitionLock
    {
        public int RequiredDepth;

        public override bool IsUnlocked()
        {
            throw new NotImplementedException();
        }
    }
}