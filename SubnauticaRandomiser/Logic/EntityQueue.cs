using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SubnauticaRandomiser.Handlers;
using SubnauticaRandomiser.Interfaces;
using SubnauticaRandomiser.Logic.LogicObjects;

namespace SubnauticaRandomiser.Logic
{
    internal class EntityQueue : IEnumerable<LogicEntity>
    {
        private PrefixLogHandler _log = PrefixLogHandler.Get("[Queue]");
        private List<LogicEntity> _entities;
        private IRandomHandler _rng;
        private List<PriorityRule> _rules;
        public int Count => _entities.Count;

        public EntityQueue(List<LogicEntity> entities, IRandomHandler rng, List<PriorityRule> rules, LogicMonitor monitor)
        {
            _entities = entities;
            _rng = rng;
            _rules = rules;
            monitor.SphereCreated += OnSphereCreated;
            
            RandomiseOrder();
            ApplyRules(0);
        }

        private void OnSphereCreated(Sphere sphere)
        {
            // If a new sphere is created as a result of applying a rule that requires a choice among entities, it is
            // possible for this new call to choose a different entity for that rule. Not great, but in practice this
            // should not be a problem even if it happens.
            ApplyRules(sphere.Tier);
        }

        /// <summary>
        /// Get the entity that is currently at the front of the queue.
        /// </summary>
        /// <exception cref="IndexOutOfRangeException">Thrown if the queue is empty.</exception>
        public LogicEntity Current()
        {
            if (Count == 0)
                throw new IndexOutOfRangeException();

            return _entities[0];
        }

        /// <summary>
        /// Remove an entity from the queue.
        /// </summary>
        public void RemoveCurrent()
        {
            if (Count == 0)
                throw new IndexOutOfRangeException();
            
            _entities.RemoveAt(0);
        }

        /// <summary>
        /// Delay the current entity at the front of the queue to a random spot and pick a different one.
        /// </summary>
        public void DelayCurrent()
        {
            if (Count == 0)
                throw new IndexOutOfRangeException();
            
            var entity = _entities[0];
            entity.Priority += (1 - entity.Priority) * _rng.NextFloat();
            _log.Debug($"Entity {entity} delayed to {entity.Priority}");
            
            // Ensure we don't converge all priorities towards 1 with repeated delays.
            if (entity.Priority >= 0.99f)
                RandomiseOrder();
            else
                _entities.Sort();
        }

        /// <summary>
        /// Shuffle the queue into a random order.
        /// </summary>
        private void RandomiseOrder()
        {
            _log.Debug("Shuffling queue order.");
            
            // First, assign a random priority to every entity.
            foreach (var entity in _entities)
            {
                entity.Priority = _rng.NextFloat();
            }
            // Sort the entities by this random priority.
            _entities.Sort();
            NormalisePriorities();
        }

        /// <summary>
        /// Redistribute all entity priorities evenly across the full range without modifying their order.
        /// </summary>
        private void NormalisePriorities()
        {
            float total = _entities.Count;
            for (int i = 0; i < _entities.Count; i++)
            {
                var entity = _entities[i];
                entity.Priority = i / total;
            }
        }

        /// <summary>
        /// Apply the <see cref="PriorityRule"/>s to the queue. If necessary, some entities and their dependencies will
        /// be pushed to the front of the queue.
        /// </summary>
        private void ApplyRules(int sphereTier)
        {
            _log.Debug($"Applying {_rules.Count} priority rules to entity queue.");
            
            List<LogicEntity> priority = new List<LogicEntity>();
            foreach (var rule in _rules)
            {
                // Check if rule applies.
                if (sphereTier >= rule.ForceBySphere)
                {
                    var notRandomised = rule.Entities.Where(e => e.Sphere < 0).ToList();
                    if ((rule.RequireAllEntities && notRandomised.Count == 0)
                        || (!rule.RequireAllEntities && notRandomised.Count < rule.Entities.Count))
                        continue;
                    
                    // Add the entity and all its dependencies to the list.
                    // This setup means that earlier rules will take priority over later ones, because the first rule
                    // will always be first in the priority list.
                    if (rule.RequireAllEntities)
                        notRandomised.ForEach(e => priority.AddRange(GetDependenciesRecursively(e)));
                    else
                        priority.AddRange(GetDependenciesRecursively(_rng.Choice(notRandomised)));
                }
            }

            // Many rules will probably share dependencies (builder tool!), deduplicate them.
            priority = priority.Distinct().ToList();
            // Iterate back to front so dependencies always get the lowest possible value.
            for (int i = priority.Count - 1; i >= 0; i--)
            {
                var entity = priority[i];
                // Give the first entity the lowest priority number so it'll be first, guaranteed.
                entity.Priority = -priority.Count + i;
                _log.Debug($"Prioritising: {entity} at {entity.Priority}");
            }
            
            // Apply the new order but squish the priorities back into 0-1 range afterwards.
            _entities.Sort();
            NormalisePriorities();
        }

        /// <summary>
        /// Assemble a list of the given entity and all its dependencies. The dependencies are first in the list such
        /// that all entities can be randomised in order.
        /// As a bonus, this method will reliably find loops in dependency relationships :)
        /// </summary>
        private List<LogicEntity> GetDependenciesRecursively(LogicEntity entity)
        {
            List<LogicEntity> entities = new List<LogicEntity>();
            foreach (var dep in entity.Dependencies)
            {
                // Keep pushing all dependencies to the front of the list.
                entities.InsertRange(0, GetDependenciesRecursively(dep));
            }

            // Only if the entity has not already been randomised.
            if (entity.Sphere < 0)
                entities.Add(entity);
            
            return entities;
        }

        public IEnumerator<LogicEntity> GetEnumerator()
        {
            while (_entities.Count > 0)
            {
                yield return Current();
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}