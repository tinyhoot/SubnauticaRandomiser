using System.Collections.Generic;
using SubnauticaRandomiser.RandomiserObjects;

namespace SubnauticaRandomiser.Logic
{
    internal class Materials
    {
        // I was really debating making this a dictionary instead. I still made
        // this into a list since the searchability of _all is important,
        // and _reachable often gets iterated over anyway. Plus, lists have the
        // advantage of making it very easy to call up a random element.
        private List<LogicEntity> _allMaterials;
        private List<LogicEntity> _reachableMaterials;

        internal List<LogicEntity> GetAll() => _allMaterials;
        internal List<LogicEntity> GetReachable() => _reachableMaterials;

        internal Materials(List<LogicEntity> allMaterials)
        {
            _allMaterials = allMaterials;
            _reachableMaterials = new List<LogicEntity>();
        }

        // Add all recipes that match the given requirements to the list.
        internal bool AddReachable(ETechTypeCategory[] categories, int maxDepth)
        {
            List<LogicEntity> additions = new List<LogicEntity>();

            // Use a lambda expression to find every object where the search
            // parameters match.
            additions.AddRange(_allMaterials.FindAll(x => ContainsCategory(categories, x.Category) && x.AccessibleDepth <= maxDepth));

            return AddToReachableList(additions);
        }

        // Add all recipes where categories, depth, and prerequisites match.
        internal bool AddReachableWithPrereqs(ETechTypeCategory[] categories, int maxDepth, TechType prerequisite, bool invert = false)
        {
            List<LogicEntity> additions = new List<LogicEntity>();

            if (invert)
            {
                additions.AddRange(_allMaterials.FindAll(x => ContainsCategory(categories, x.Category)
                                                           && x.AccessibleDepth <= maxDepth
                                                           && x.HasPrerequisites
                                                           && !x.Prerequisites.Contains(prerequisite)
                                                           ));
            }
            else
            {
                additions.AddRange(_allMaterials.FindAll(x => ContainsCategory(categories, x.Category)
                                                           && x.AccessibleDepth <= maxDepth
                                                           && x.HasPrerequisites
                                                           && x.Prerequisites.Contains(prerequisite)
                                                           ));
            }

            return AddToReachableList(additions);
        }

        private bool AddToReachableList(List<LogicEntity> additions)
        {
            // Ensure no duplicates are added to the list. This loop *must* go
            // in reverse, otherwise the computer gets very unhappy.
            for (int i = additions.Count - 1; i >= 0; i--)
            {
                if (_reachableMaterials.Contains(additions[i]) || !additions[i].HasUsesLeft())
                    additions.RemoveAt(i);
            }

            if (additions.Count <= 0)
                return false;

            foreach(LogicEntity ent in additions)
            {
                LogHandler.Debug("Adding to reachable materials: " + ent.TechType.AsString());
            }
            _reachableMaterials.AddRange(additions);
            return true;
        }

        internal bool AddReachable(LogicEntity entity)
        {
            return AddToReachableList(new List<LogicEntity> { entity });
        }

        internal bool AddReachable(ETechTypeCategory category, int maxDepth)
        {
            return AddReachable(new ETechTypeCategory[] { category }, maxDepth);
        }

        internal bool AddReachableWithPrereqs(ETechTypeCategory category, int maxDepth, TechType prerequisite, bool invert = false)
        {
            return AddReachableWithPrereqs(new ETechTypeCategory[] { category }, maxDepth, prerequisite, invert);
        }

        // TODO: Generalise this.
        private bool ContainsCategory(ETechTypeCategory[] array, ETechTypeCategory target)
        {
            foreach (ETechTypeCategory category in array)
            {
                if (category.Equals(target))
                    return true;
            }

            return false;
        }

        private bool ContainsTechType(TechType[] array, TechType target)
        {
            foreach (TechType type in array)
            {
                if (type.Equals(target))
                    return true;
            }

            return false;
        }
    }
}
