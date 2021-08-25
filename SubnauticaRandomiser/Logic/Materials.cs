using System.Collections.Generic;

namespace SubnauticaRandomiser.Logic
{
    internal class Materials
    {
        // I was really debating making this a dictionary instead. I still made
        // this into a list since the searchability of _all is important,
        // and _reachable often gets iterated over anyway. Plus, lists have the
        // advantage of making it very easy to call up a random element.
        private List<RandomiserRecipe> _allMaterials;
        private List<RandomiserRecipe> _reachableMaterials;

        internal List<RandomiserRecipe> GetAll() => _allMaterials;
        internal List<RandomiserRecipe> GetReachable() => _reachableMaterials;

        internal Materials(List<RandomiserRecipe> allMaterials)
        {
            _allMaterials = allMaterials;
            _reachableMaterials = new List<RandomiserRecipe>();
        }

        // Add all recipes that match the given requirements to the list.
        internal bool AddReachable(ETechTypeCategory[] categories, int maxDepth)
        {
            List<RandomiserRecipe> additions = new List<RandomiserRecipe>();

            // Use a lambda expression to find every object where the search
            // parameters match.
            additions.AddRange(_allMaterials.FindAll(x => ContainsCategory(categories, x.Category) && x.Depth <= maxDepth));

            return AddToReachableList(additions);
        }

        // Add all recipes where categories, depth, and prerequisites match.
        internal bool AddReachableWithPrereqs(ETechTypeCategory[] categories, int maxDepth, TechType prerequisite, bool invert = false)
        {
            List<RandomiserRecipe> additions = new List<RandomiserRecipe>();

            if (invert)
            {
                additions.AddRange(_allMaterials.FindAll(x => ContainsCategory(categories, x.Category)
                                                           && x.Depth <= maxDepth
                                                           && x.Prerequisites != null
                                                           && !x.Prerequisites.Contains(prerequisite)
                                                           ));
            }
            else
            {
                additions.AddRange(_allMaterials.FindAll(x => ContainsCategory(categories, x.Category)
                                                           && x.Depth <= maxDepth
                                                           && x.Prerequisites != null
                                                           && x.Prerequisites.Contains(prerequisite)
                                                           ));
            }

            return AddToReachableList(additions);
        }

        private bool AddToReachableList(List<RandomiserRecipe> additions)
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

            foreach(RandomiserRecipe r in additions)
            {
                LogHandler.Debug("Adding to reachable materials: " + r.TechType.AsString());
            }
            _reachableMaterials.AddRange(additions);
            return true;
        }

        internal bool AddReachable(RandomiserRecipe recipe)
        {
            return AddToReachableList(new List<RandomiserRecipe> { recipe });
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
