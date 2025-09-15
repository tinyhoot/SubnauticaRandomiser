using System;

namespace SubnauticaRandomiser.Logic.LogicObjects.Transitions
{
    /// <summary>
    /// Represents an obstacle that must be overcome before the <see cref="Transition"/> this is attached to can be used.
    /// </summary>
    [Serializable]
    internal abstract class TransitionLock
    {
        public abstract bool CheckUnlocked();
    }
}