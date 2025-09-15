using System;
using System.Collections.Generic;
using SubnauticaRandomiser.Logic.LogicObjects.Transitions;

namespace SubnauticaRandomiser.Logic.LogicObjects
{
    /// <summary>
    /// Represents a 'stage' in the playthrough. Only a limited number of <see cref="Region"/>s and
    /// <see cref="LogicEntity"/>s are accessible. The player's aim is to find the entities that unlock access to the
    /// next sphere and, eventually, the win condition.
    /// <br /><br />
    /// Spheres are concentric and can never shrink. Progress is permanent and later spheres have access to everything
    /// that came before.
    /// </summary>
    internal class Sphere
    {
        /// <summary>
        /// Spheres are concentric. Spheres of higher tiers have access to everything in spheres of lower tiers.
        /// </summary>
        public readonly int Tier;

        /// <summary>
        /// All regions accessible within this sphere.
        /// </summary>
        public List<Region> Regions = new List<Region>();

        /// <summary>
        /// All transitions leading from this sphere to regions not contained within this sphere.
        /// </summary>
        public List<Transition> EdgeTransitions = new List<Transition>();

        public Sphere(RandomisationContext context)
        {
            Tier = 0;
            Regions.Add(context.StartingRegion);
        }

        public Sphere(Sphere innerSphere)
        {
            Tier = innerSphere.Tier + 1;
            Regions = new List<Region>(innerSphere.Regions);
            AddAllReachableRegions();
        }

        private void AddAllReachableRegions()
        {
            PopulateEdges();
            while (TryUnlockEdges(out var unlocked))
            {
                // A new region was unlocked; either it has no lock or all its locks are already covered by progression
                // from earlier spheres.
                Regions.AddRange(unlocked);
                // Repopulate the edges with any new transitions from the new regions.
                // TODO: Check performance impact of redoing this work all the time and optimise if needed.
                PopulateEdges();
            }
        }

        /// <summary>
        /// (Re-)populates the transitions leading out of this sphere.
        /// </summary>
        private void PopulateEdges()
        {
            EdgeTransitions.Clear();

            foreach (var region in Regions)
            {
                foreach (var trans in region.Transitions)
                {
                    // Do not add transitions that are fully contained within the sphere, i.e. where both ends are
                    // part of the sphere.
                    if (!Regions.Contains(trans.Entry) || !Regions.Contains(trans.Exit))
                    {
                        // Because we are only adding transitions with one region in the sphere this can never lead
                        // to duplicates.
                        EdgeTransitions.Add(trans);
                    }
                }
            }
        }

        /// <summary>
        /// Try to unlock the <see cref="TransitionLock"/>s on the edges of this Sphere to gain access to new
        /// <see cref="Region"/>s.
        /// </summary>
        /// <param name="unlockedRegions">The regions outside the sphere that can be accessed through newly unlocked
        /// transitions.</param>
        /// <returns>True if an edge was unlocked, false otherwise.</returns>
        public bool TryUnlockEdges(out List<Region> unlockedRegions)
        {
            if (EdgeTransitions.Count == 0)
            {
                unlockedRegions = null;
                return false;
            }

            unlockedRegions = new List<Region>();
            foreach (var edge in EdgeTransitions)
            {
                if (!edge.CheckLocks())
                    continue;

                if (!Regions.Contains(edge.Entry))
                    unlockedRegions.Add(edge.Entry);
                if (!Regions.Contains(edge.Exit))
                    unlockedRegions.Add(edge.Exit);
            }

            if (unlockedRegions.Count != 0)
                return true;

            unlockedRegions = null;
            return false;
        }

        public void PriorityFill()
        {
            throw new NotImplementedException();
        }

        public void Fill()
        {
            // Fill the sphere with random things until everything is populated.
            // After the sphere is full, expand to the next sphere.
            // We *know* that, in order to expand to the next-next sphere, at least one transition has to get unblocked.
            // That means picking one transition goal for the next sphere, possibly at random.
            // The transition goal becomes a priority fill that is handled first, along with all its dependencies.
            throw new NotImplementedException();
        }
    }
}