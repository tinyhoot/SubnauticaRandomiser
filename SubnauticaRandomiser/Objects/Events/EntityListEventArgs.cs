using System.Collections.Generic;

namespace SubnauticaRandomiser.Objects.Events
{
    internal class EntityListEventArgs
    {
        public List<LogicEntity> EntityList;

        public EntityListEventArgs()
        {
            EntityList = new List<LogicEntity>();
        }
    }
}