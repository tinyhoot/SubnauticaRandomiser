using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using SubnauticaRandomiser.Interfaces;
using SubnauticaRandomiser.Objects;
using SubnauticaRandomiser.Objects.Enums;

namespace SubnauticaRandomiser.Logic.Recipes
{
    internal class Materials
    {
        // I was really debating making this a dictionary instead. I still made
        // this into a list since the searchability of _all is important,
        // and _reachable often gets iterated over anyway. Plus, lists have the
        // advantage of making it very easy to call up a random element.
        private readonly List<LogicEntity> _allMaterials;
        private readonly List<LogicEntity> _reachableMaterials;
        private readonly ILogHandler _log;

        public List<LogicEntity> GetAll() => _allMaterials;
        public List<LogicEntity> GetReachable() => _reachableMaterials;

        public Materials(List<LogicEntity> allMaterials, ILogHandler logger)
        {
            _allMaterials = allMaterials;
            _reachableMaterials = new List<LogicEntity>();
            _log = logger;
        }
        
        /// <summary>
        /// Add all recipes that match the given requirements to the list of reachable materials.
        /// </summary>
        /// <param name="categories">The category of materials to consider.</param>
        /// <param name="maxDepth">The maximum depth at which the material is allowed to be available.</param>
        /// <returns>True if any new entries were added to the list of reachable materials, false otherwise.</returns>
        public bool AddReachable(ETechTypeCategory[] categories, int maxDepth)
        {
            List<LogicEntity> additions = new List<LogicEntity>();

            // Use a lambda expression to find every object where the search parameters match.
            additions.AddRange(_allMaterials.FindAll(x => categories.Contains(x.Category) && x.AccessibleDepth <= maxDepth));

            return AddToReachableList(additions);
        }
        
        /// <summary>
        /// Add all recipes where categories, maximum, depth, and prerequisites match to the list of reachable materials.
        /// </summary>
        /// <param name="categories">The category of materials to consider.</param>
        /// <param name="maxDepth">The maximum depth at which the material is allowed to be available.</param>
        /// <param name="prerequisite">Only consider materials which require this TechType to be randomised before they
        /// are allowed to be considered available.</param>
        /// <param name="invert">If true, invert the behaviour of the prerequisite to consider exclusively materials
        /// which do <i>not</i> require that TechType.</param>
        /// <returns>True if any new entries were added to the list of reachable materials, false otherwise.</returns>
        public bool AddReachableWithPrereqs(ETechTypeCategory[] categories, int maxDepth, TechType prerequisite, bool invert = false)
        {
            List<LogicEntity> additions = new List<LogicEntity>();

            if (invert)
            {
                additions.AddRange(_allMaterials.FindAll(x => categories.Contains(x.Category)
                                                           && x.AccessibleDepth <= maxDepth
                                                           && (!x.HasPrerequisites
                                                           || !x.Prerequisites.Contains(prerequisite))
                                                           ));
            }
            else
            {
                additions.AddRange(_allMaterials.FindAll(x => categories.Contains(x.Category)
                                                           && x.AccessibleDepth <= maxDepth
                                                           && x.HasPrerequisites
                                                           && x.Prerequisites.Contains(prerequisite)
                                                           ));
            }

            return AddToReachableList(additions);
        }

        /// <summary>
        /// Add materials to the list of things considered reachable.
        /// </summary>
        /// <param name="additions"></param>
        /// <returns>True if any new entities were added to the list, false otherwise.</returns>
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
                _log.Debug("[R] Adding to reachable materials: " + ent.TechType);
            }
            _reachableMaterials.AddRange(additions);
            return true;
        }

        /// <summary>
        /// Add a single entity to the list of reachable things.
        /// </summary>
        /// <param name="entity">The entity to add.</param>
        /// <returns>True if successful, false otherwise.</returns>
        public bool AddReachable(LogicEntity entity)
        {
            return AddToReachableList(new List<LogicEntity> { entity });
        }

        /// <summary>
        /// Add all entities matching the category up to a maximum depth to the list of reachable things.
        /// </summary>
        /// <param name="category">The category to consider.</param>
        /// <param name="maxDepth">The maximum depth at which the entity is allowed to be available.</param>
        /// <returns>True if any new entities were added to the list, false otherwise.</returns>
        public bool AddReachable(ETechTypeCategory category, int maxDepth)
        {
            return AddReachable(new[] { category }, maxDepth);
        }

        /// <summary>
        /// Add all recipes where category, maximum, depth, and prerequisites match to the list of reachable materials.
        /// </summary>
        /// <param name="category">The category of materials to consider.</param>
        /// <param name="maxDepth">The maximum depth at which the material is allowed to be available.</param>
        /// <param name="prerequisite">Only consider materials which require this TechType to be randomised before they
        /// are allowed to be considered available.</param>
        /// <param name="invert">If true, invert the behaviour of the prerequisite to consider exclusively materials
        /// which do <i>not</i> require that TechType.</param>
        /// <returns>True if any new entries were added to the list of reachable materials, false otherwise.</returns>
        public bool AddReachableWithPrereqs(ETechTypeCategory category, int maxDepth, TechType prerequisite, bool invert = false)
        {
            return AddReachableWithPrereqs(new[] { category }, maxDepth, prerequisite, invert);
        }

        /// <summary>
        /// Get the corresponding LogicEntity to the given TechType.
        /// </summary>
        /// <param name="type">The TechType.</param>
        /// <returns>The LogicEntity if found, null otherwise.</returns>
        [CanBeNull]
        public LogicEntity Find(TechType type)
        {
            return _allMaterials.Find(x => x.TechType.Equals(type));
        }

        /// <summary>
        /// Get all entities that are capable of being crafted.
        /// </summary>
        public List<LogicEntity> GetAllCraftables()
        {
            var craftables = _allMaterials.FindAll(x => 
                !x.Category.Equals(ETechTypeCategory.RawMaterials) 
                && !x.Category.Equals(ETechTypeCategory.Fish) 
                && !x.Category.Equals(ETechTypeCategory.Seeds)
                && !x.Category.Equals(ETechTypeCategory.Eggs)
                && !x.Category.Equals(ETechTypeCategory.Fragments));

            return craftables;
        }

        /// <summary>
        /// Get all entities that are considered fragments.
        /// </summary>
        public List<LogicEntity> GetAllFragments()
        {
            var fragments = _allMaterials.FindAll(x =>
                x.Category.Equals(ETechTypeCategory.Fragments));

            return fragments;
        }

        /// <summary>
        /// Get all entities that are considered raw materials without prerequisites and accessible by the given depth.
        /// </summary>
        /// <param name="maxDepth">The maximum depth at which the raw materials must be available.</param>
        public List<LogicEntity> GetAllRawMaterials(int maxDepth = 2000)
        {
            var rawMaterials = _allMaterials.FindAll(x =>
                x.Category.Equals(ETechTypeCategory.RawMaterials) && x.AccessibleDepth <= maxDepth
                                                                  && !x.HasPrerequisites);

            return rawMaterials;
        }
    }
}
