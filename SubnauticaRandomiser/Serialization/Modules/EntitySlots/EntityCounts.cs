using System;
using System.Collections.Generic;
using UnityEngine;

namespace SubnauticaRandomiser.Serialization.Modules.EntitySlots
{
    /// <summary>
    /// Represents the number of spawns that must be ensured for a number of TechTypes in a particular biome.
    /// </summary>
    [Serializable]
    internal class EntityCounts
    {
        public BiomeType Biome;
        public List<SpawnCounter> SpawnCounters = new List<SpawnCounter>();
        public float NextCheckThreshold;
        public bool Finished;

        /// <summary>
        /// Register an entity with its minimum spawn count for a biome.
        /// </summary>
        public void AddEntity(TechType techType, int required)
        {
            SpawnCounters.Add(new SpawnCounter(techType, required));
        }

        /// <summary>
        /// Count one successful spawn for an entity.
        /// </summary>
        public void CountSpawn(TechType techType)
        {
            var counter = SpawnCounters.Find(c => c.TechType == techType);
            if (counter is null)
                return;
            
            counter.Add(1);
            SortCounters();
        }

        /// <summary>
        /// Sort the counters so that the ones with the least spawns come first. Ideally, all entities finish spawning
        /// around the same time.
        /// </summary>
        private void SortCounters()
        {
            SpawnCounters.Sort((a, b) => a.Completion.CompareTo(b.Completion));
        }

        /// <summary>
        /// Get an entity that needs to be force-spawned right now.
        /// </summary>
        /// <param name="completionThreshold">The proportion of all available slots that have already spawned, i.e.
        /// what proportion of opportunities for the entity to spawn has passed.</param>
        /// <returns>The TechType of the entity, or TechType.None if no forced spawn is needed.</returns>
        public TechType GetForcedSpawn(float completionThreshold)
        { 
            // Because we keep the list sorted the next most urgent spawn is always in first position.
            var counter = SpawnCounters[0];
            // Don't force anything if the vanilla systems are currently performing well.
            if (counter.Completion > completionThreshold)
            {
                // All entities are ahead in spawns. Check in again once EntitySlots catch up to the current level
                // to ensure we don't get stuck.
                NextCheckThreshold = counter.Completion;
                return TechType.None;
            }
            
            // Keep the threshold at this level even though it'll be outdated with the next slot. This avoids a lack of
            // forced spawns when the second entity's completion percentage is much higher than the first one's.
            NextCheckThreshold = counter.Completion;
            return counter.TechType;
        }

        [Serializable]
        internal class SpawnCounter
        {
            public TechType TechType;
            public int Spawned;
            public int Required;
            public float Completion;

            public SpawnCounter(TechType techType, int required)
            {
                TechType = techType;
                Spawned = 0;
                Required = required;
            }

            public SpawnCounter(TechType techType, int spawned, int required)
            {
                TechType = techType;
                Spawned = spawned;
                Required = required;
            }

            public void Add(int spawned)
            {
                Spawned += spawned;
                Completion = Mathf.Clamp01((float)Spawned / Required);
            }
        }
    }
}