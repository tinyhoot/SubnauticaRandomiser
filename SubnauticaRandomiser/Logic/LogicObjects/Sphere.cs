using System.Collections.Generic;
using System.Linq;
using SubnauticaRandomiser.Handlers;
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

        /// <summary>
        /// The entities that are accessible within this sphere. Includes all entities from previous spheres.
        /// </summary>
        public List<LogicEntity> Entities = new List<LogicEntity>();
        
        private PrefixLogHandler _log = PrefixLogHandler.Get("[Sphere]");
        private EntityManager _entityManager;
        private TravelDistanceManager _travelManager;

        public Sphere(StartingState start, EntityManager entities, TravelDistanceManager travelManager)
        {
            Tier = 0;
            _entityManager = entities;
            _travelManager = travelManager;
            Regions.Add(start.StartingRegion);
            AddEntitiesFromRegions(new[] { start.StartingRegion });
            // Add the entities that the player starts with, like initial items inside the lifepod.
            start.StartingEntities.ForEach(AddEntity);
            // Keep tier 0 very small, intentionally.
            PopulateEdges();
        }

        public Sphere(Sphere innerSphere, List<Region> newRegions)
        {
            Tier = innerSphere.Tier + 1;
            _entityManager = innerSphere._entityManager;
            _travelManager = innerSphere._travelManager;
            // Ensure this sphere's regions and entities are independent of the previous sphere's.
            Regions = innerSphere.Regions.Concat(newRegions).ToList();
            Entities = new List<LogicEntity>(innerSphere.Entities);
            AddEntitiesFromRegions(newRegions);
            // Check for any additional regions that may unlock in a chain reaction.
            AddAllReachableRegions();
        }

        public void AddEntity(LogicEntity entity)
        {
            Entities.Add(entity);
            entity.Sphere = Tier;
        }
        
        /// <summary>
        /// Populate the sphere with entities that are acquired simply by gaining access to a specific region.
        /// </summary>
        private void AddEntitiesFromRegions(IEnumerable<Region> regions)
        {
            foreach (var region in regions)
            {
                if (region.Entities is null || region.Entities.Count == 0)
                    continue;
                
                _log.Debug($"Adding {region.Entities} guaranteed entities from region {region.Name}.");
                foreach (var entity in region.Entities)
                {
                    AddEntity(entity);
                }
            }
        }

        private void AddAllReachableRegions()
        {
            List<Region> unlocked;
            // Keep trying to unlock new regions until all region/transition chains hit a dead end.
            do
            {
                PopulateEdges();
                TryUnlockEdges(out unlocked);
                if (unlocked != null)
                {
                    _log.Debug($"Unlocking {unlocked.Count} regions.");
                    Regions.AddRange(unlocked);
                    // Instantly unlock the entities that are guaranteed to be in these regions.
                    AddEntitiesFromRegions(unlocked);
                }
            } while (unlocked?.Count > 0);
        }

        /// <summary>
        /// (Re-)populates the transitions leading out of this sphere.
        /// </summary>
        private void PopulateEdges()
        {
            _log.Debug($"Recalculating region edges of sphere {Tier}.");
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
                        _log.Debug($"Adding edge {trans} to sphere {Tier}.");
                        EdgeTransitions.Add(trans);
                    }
                }
            }
            _log.Debug($"Recalculated {EdgeTransitions.Count} edges for sphere {Tier}.");
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
                _log.Debug("Sphere had no edges, can't unlock new regions.");
                unlockedRegions = null;
                return false;
            }

            unlockedRegions = new List<Region>();
            foreach (var edge in EdgeTransitions)
            {
                if (!edge.CheckLocks(_entityManager))
                    continue;

                foreach (var region in edge.Regions)
                {
                    if (CanUnlockRegion(region) && !unlockedRegions.Contains(region))
                    {
                        unlockedRegions.Add(region);
                        _log.Debug($"Unlocking region {region}");
                    }
                }
            }

            if (unlockedRegions.Count != 0)
                return true;

            unlockedRegions = null;
            return false;
        }

        private bool CanUnlockRegion(Region region)
        {
            return !Regions.Contains(region) && _travelManager.CanReach(region.Depth);
        }
    }
}