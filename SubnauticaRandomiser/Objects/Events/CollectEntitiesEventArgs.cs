using System;
using System.Collections.Generic;

namespace SubnauticaRandomiser.Objects.Events
{
    internal class CollectEntitiesEventArgs : EventArgs
    {
        public List<LogicEntity> ToBeRandomised = new List<LogicEntity>();
    }
}