using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SubnauticaRandomiser.Handlers;
using SubnauticaRandomiser.Logic.LogicObjects;
using SubnauticaRandomiser.Serialization;
using UnityEngine;

namespace SubnauticaRandomiser.Logic
{
    /// <summary>
    /// Responsible for constructing, linking and providing access to <see cref="LogicEntity"/>.
    /// </summary>
    internal class EntityManager
    {
        private const string EntitiesFolder = "Entities";
        private const string ConstructablesFile = "constructables.json";
        private const string DataboxFile = "databoxes.json";
        private const string FragmentsFile = "fragments.json";
        private const string RecipesFile = "recipes.json";
        private const string SpawnablesFile = "spawnables.json";
        private const string InvItemsFile = "inventoryItems.json";
        
        private PrefixLogHandler _log = PrefixLogHandler.Get("[EntityManager]");
        private List<LogicEntity> _entities = new List<LogicEntity>();
        private Dictionary<string, int> _entityIdMap = new Dictionary<string, int>();
        private Dictionary<TechType, List<int>> _entityTechIds = new Dictionary<TechType, List<int>>();

        /// <summary>
        /// Try to find a specific entity. Will return null if none match the search criteria.
        /// </summary>
        public T Find<T>(TechType techType) where T : LogicEntity
        {
            foreach (var entity in GetAllByTechType(techType) ?? Enumerable.Empty<LogicEntity>())
            {
                if (entity is T t)
                    return t;
            }

            return null;
        }

        /// <inheritdoc cref="Find{T}"/>
        public LogicEntity Find(Type type, TechType techType)
        {
            return GetAllByTechType(techType)?.FirstOrDefault(entity => entity.GetType() == type);
        }
        
        /// <summary>
        /// Get a specific entity by its id.
        /// </summary>
        /// <param name="id">The numerical id assigned during registration.</param>
        /// <exception cref="KeyNotFoundException">Thrown if the provided id was not assigned to any entity.</exception>
        public LogicEntity Get(int id)
        {
            if (id < 0 || id >= _entities.Count)
                throw new KeyNotFoundException($"No such entity with id {id}");

            return _entities[id];
        }

        /// <summary>
        /// Get a specific entity by its id.
        /// </summary>
        /// <param name="id">The string representation of the entity, consisting of type and name.</param>
        /// <exception cref="KeyNotFoundException">Thrown if the provided id was not assigned to any entity.</exception>
        public LogicEntity Get(string id)
        {
            if (!_entityIdMap.TryGetValue(id, out int i))
                throw new KeyNotFoundException($"No such entity with id {id}");

            return Get(i);
        }

        /// <summary>
        /// Get a shallow copy of the list of all entities.
        /// </summary>
        public List<LogicEntity> GetAllEntities()
        {
            return new List<LogicEntity>(_entities);
        }

        /// <summary>
        /// Get all entities that are instances or subclasses of T.
        /// </summary>
        public IEnumerable<T> GetAllEntities<T>() where T : LogicEntity
        {
            return _entities.OfType<T>();
        }
        
        /// <summary>
        /// Get all entities that share the provided TechType.
        /// </summary>
        /// <returns>The entities, or null if no entity with that TechType exists.</returns>
        public List<LogicEntity> GetAllByTechType(TechType techType)
        {
            var ids = GetAllIdsByTechType(techType);
            return ids?.Select(Get).ToList();
        }
        
        /// <summary>
        /// Get all entities that share the provided TechType.
        /// </summary>
        /// <returns>The entity ids, or null if no entity with that TechType exists.</returns>
        public List<int> GetAllIdsByTechType(TechType techType)
        {
            if (!_entityTechIds.TryGetValue(techType, out List<int> ids))
                return null;

            return ids.ShallowCopy();
        }

        /// <summary>
        /// Load and parse all entities from their respective files on disk.
        /// </summary>
        public IEnumerator ParseEntitiesFromDiskAsync()
        {
            yield return DeserializeEntitiesAsync<LogicConstructable>(EntitiesFolder, ConstructablesFile);
            yield return DeserializeEntitiesAsync<LogicDatabox>(EntitiesFolder, DataboxFile);
            yield return DeserializeEntitiesAsync<LogicFragment>(EntitiesFolder, FragmentsFile);
            yield return DeserializeEntitiesAsync<LogicInventoryItem>(EntitiesFolder, InvItemsFile);
            yield return DeserializeEntitiesAsync<LogicRecipe>(EntitiesFolder, RecipesFile);
            yield return DeserializeEntitiesAsync<LogicSpawnable>(EntitiesFolder, SpawnablesFile);
        }

        private IEnumerator DeserializeEntitiesAsync<T>(params string[] path) where T : LogicEntity
        {
            _log.Debug($"> Starting {string.Join("/", path)} at {Time.realtimeSinceStartup}");
            var task = SerdeUtils.ReadFileContents(path);
            yield return new WaitUntil(() => task.IsCompleted);
            _log.Debug($"- Raw file contents loaded for {string.Join("/", path)} at {Time.realtimeSinceStartup}");

            var entities = new TaskResult<List<T>>();
            yield return SerdeUtils.DeserializeObjectsAsync(task.Result, entities);
            AddEntities(entities.Get());
            _log.Debug($"- Completed parsing {string.Join("/", path)} at {Time.realtimeSinceStartup}");
        }

        private void AddEntities(IEnumerable<LogicEntity> entities)
        {
            foreach (var entity in entities)
            {
                AddEntity(entity);
            }
        }

        private void AddEntity(LogicEntity entity)
        {
            // The numerical ID of an entity is its registration number.
            _entityIdMap[entity.ToString()] = _entities.Count;
            if (!_entityTechIds.TryGetValue(entity.TechType, out List<int> ids))
            {
                ids = new List<int>();
                _entityTechIds[entity.TechType] = ids;
            }
            ids.Add(_entities.Count);
            _entities.Add(entity);
        }

        public IEnumerator LinkEntities()
        {
            LinkConstructables();
            LinkInventoryItems();
            LinkRecipes();
            LinkBlueprints();
            yield return null;
            ReplaceReferences(_entities);
        }

        private void LinkConstructables()
        {
            foreach (var constructable in GetAllEntities<LogicConstructable>())
            {
                var recipe = Find<LogicRecipe>(constructable.TechType);
                if (recipe != null)
                    constructable.AddRecipe(recipe);
            }
        }

        private void LinkInventoryItems()
        {
            foreach (var iitem in GetAllEntities<LogicInventoryItem>())
            {
                var recipe = Find<LogicRecipe>(iitem.TechType);
                if (recipe != null)
                    iitem.AddRecipe(recipe);
                
                var spawnable = Find<LogicSpawnable>(iitem.TechType);
                if (spawnable != null)
                    iitem.AddSpawnable(spawnable);
            }
        }

        private void LinkRecipes()
        {
            foreach (var recipe in GetAllEntities<LogicRecipe>())
            {
                var blueprint = Find<LogicBlueprint>(recipe.TechType);
                if (blueprint != null)
                    recipe.AddBlueprint(blueprint);
                
                recipe.LinkVanillaRecipe(this, _log);
            }
        }

        private void LinkBlueprints()
        {
            // Add spawnables to fragments for cases like seamoth fragments.
            foreach (var fragment in GetAllEntities<LogicFragment>())
            {
                var spawnable = Find<LogicSpawnable>(fragment.TechType);
                if (spawnable != null)
                    fragment.Dependencies.Add(spawnable);
            }
        }

        /// <summary>
        /// Try to replace all <see cref="LogicEntityReference"/> with the proper <see cref="LogicEntity"/>.
        /// </summary>
        private void ReplaceReferences(List<LogicEntity> entities)
        {
            foreach (var entity in entities)
            {
                if (entity.Dependencies is null || entity.Dependencies.Count == 0)
                    continue;
                
                for (int i = entity.Dependencies.Count - 1; i >= 0; i--)
                {
                    if (entity.Dependencies[i] is LogicEntityReference dep)
                    {
                        var replacement = Find(dep.EntityType, dep.TechType);
                        if (replacement is null)
                        {
                            _log.Warn($"Failed to replace reference entity in dependencies of {entity} --> {dep}");
                            entity.Dependencies.RemoveAt(i);
                            continue;
                        }

                        entity.Dependencies[i] = replacement;
                    }
                }
            }
        }
    }
}