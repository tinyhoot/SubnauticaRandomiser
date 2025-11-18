using System;

namespace SubnauticaRandomiser.Logic.LogicObjects
{
    /// <summary>
    /// Used as a temporary placeholder for the dependencies specified in the files loaded from disk. Should always be
    /// replaced by a reference to the proper entity and never enter the actual logic.
    /// </summary>
    internal class LogicEntityReference : LogicEntity
    {
        public Type EntityType;

        public LogicEntityReference(Type entityType, TechType techType) : base(techType)
        {
            EntityType = entityType;
        }
        
        public override string ToString()
        {
            return "ref" + TypeNameSeparator + EntityType.Name + TypeNameSeparator + TechType.AsString();
        }
    }
}