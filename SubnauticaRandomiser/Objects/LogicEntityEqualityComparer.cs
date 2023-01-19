using System.Collections.Generic;

namespace SubnauticaRandomiser.Objects
{
    /// <summary>
    /// Provides a comparer to enable using entities in HashSets and Dictionaries.
    /// </summary>
    internal class LogicEntityEqualityComparer : IEqualityComparer<LogicEntity>
    {
        public bool Equals(LogicEntity x, LogicEntity y)
        {
            if (ReferenceEquals(x, y))
                return true;
            if (ReferenceEquals(x, null))
                return false;
            if (ReferenceEquals(y, null))
                return false;
            if (x.GetType() != y.GetType())
                return false;
            return x.EntityType == y.EntityType && x.TechType == y.TechType && x.Category == y.Category;
        }

        public int GetHashCode(LogicEntity obj)
        {
            unchecked
            {
                var hashCode = (int)obj.EntityType;
                hashCode = (hashCode * 397) ^ (int)obj.TechType;
                hashCode = (hashCode * 397) ^ (int)obj.Category;
                return hashCode;
            }
        }
    }
}