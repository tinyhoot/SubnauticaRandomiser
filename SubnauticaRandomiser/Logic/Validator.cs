using System.Collections.Generic;
using HootLib;
using SubnauticaRandomiser.Logic.LogicObjects;
using SubnauticaRandomiser.Objects.Exceptions;

namespace SubnauticaRandomiser.Logic
{
    /// <summary>
    /// Responsible for checking the randomised game state for errors.
    /// </summary>
    internal class Validator
    {
        /// <summary>
        /// Check whether every <see cref="LogicEntityReference"/> has been resolved.
        /// </summary>
        /// <exception cref="LinkingException">Thrown if any entity depends on a <see cref="LogicEntityReference"/>.
        /// </exception>
        public static void ValidateEntityReferenceLinking(List<LogicEntity> entities)
        {
            foreach (var entity in entities)
            {
                foreach (var dep in entity.Dependencies)
                {
                    if (dep is LogicEntityReference)
                        throw new LinkingException($"Found unresolved entity reference in {entity} pointing to {dep}");
                }
            }
        }

        /// <summary>
        /// Check whether all regions are linked together via transitions.
        /// </summary>
        /// <exception cref="LinkingException">Thrown if one or more regions are inaccessible.</exception>
        public static void ValidateRegionLinking(RegionManager manager)
        {
            HashSet<int> regions = new HashSet<int> { 0 };
            List<int> toExplore = new List<int> { 0 };

            while (toExplore.Count > 0)
            {
                var current = manager.GetRegion(toExplore[0]);
                foreach (var trans in current.Transitions)
                {
                    var entry = manager.GetId(trans.Entry);
                    var exit = manager.GetId(trans.Exit);
                    if (regions.Add(entry))
                        toExplore.Add(entry);
                    if (regions.Add(exit))
                        toExplore.Add(exit);
                }
                toExplore.RemoveAt(0);
            }

            List<Region> orphaned = new List<Region>();
            foreach (var region in manager.GetAllRegions())
            {
                var id = manager.GetId(region);
                if (!regions.Contains(id))
                    orphaned.Add(region);
            }

            if (orphaned.Count > 0)
                throw new LinkingException($"Found {orphaned.Count} orphaned regions: {orphaned.ElementsToString()}");
        }
    }
}