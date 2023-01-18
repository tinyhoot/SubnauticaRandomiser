using System;

namespace SubnauticaRandomiser.Objects.Events
{
    internal class EntityEventArgs : EventArgs
    {
        public LogicEntity LogicEntity;

        public EntityEventArgs()
        {
        }

        public EntityEventArgs(LogicEntity entity)
        {
            LogicEntity = entity;
        }
    }
}