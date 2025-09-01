using System;
using System.Collections.Generic;

namespace SubnauticaRandomiser.Logic.LogicObjects
{
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

        public Sphere(RandomisationContext context)
        {
            Tier = 0;
            Regions.Add(context.StartingRegion);
        }

        public Sphere(int tier)
        {
            Tier = tier;
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