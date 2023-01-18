using System;
using System.Collections.Generic;

namespace SubnauticaRandomiser.Objects.Events
{
    internal class SetupProgressionEventArgs : EventArgs
    {
        public HashSet<TechType> ProgressionEntities { get; }

        public SetupProgressionEventArgs(HashSet<TechType> entities)
        {
            ProgressionEntities = entities;
        }
    }
}