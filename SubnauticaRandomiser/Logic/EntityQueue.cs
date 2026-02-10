using System;
using System.Collections;
using System.Collections.Generic;
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
        public int Count => _entities.Count;

        public EntityQueue(List<LogicEntity> entities, IRandomHandler rng)
        {
            _entities = entities;
            _rng = rng;
            
            RandomiseOrder();
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
        public void RandomiseOrder()
        {
            _log.Debug("Shuffling queue order.");
            
            // First, assign a random priority to every entity.
            foreach (var entity in _entities)
            {
                entity.Priority = _rng.NextFloat();
            }
            // Sort the entities by this random priority.
            _entities.Sort();
            
            // Normalise the priorities.
            float total = _entities.Count;
            for (int i = 0; i < _entities.Count; i++)
            {
                var entity = _entities[i];
                entity.Priority = i / total;
                // Give modules a chance to adjust entity priorities.
                // _monitor.TriggerPrioritySetup(entity);
            }
            
            // Sort again in case the priorities were modified by a module.
            _entities.Sort();
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