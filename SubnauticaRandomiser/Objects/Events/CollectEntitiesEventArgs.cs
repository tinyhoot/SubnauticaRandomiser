using System;
using System.Collections.Generic;

namespace SubnauticaRandomiser.Objects.Events
{
    internal class CollectEntitiesEventArgs : EventArgs
    {
        public List<LogicEntity> toBeRandomised = new List<LogicEntity>();
    }
}