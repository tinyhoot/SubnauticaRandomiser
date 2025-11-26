using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HootLib;
using Newtonsoft.Json;
using SubnauticaRandomiser.Handlers;
using SubnauticaRandomiser.Logic.LogicObjects;

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

        public async Task ParseEntitiesFromDisk()
        {
            // Load the file and parse the baseline data for each entity
            // Perform linking - hook up recipes and blueprints, etc.
            // Set up entity dependencies
            // Validate that everything has been linked up and no stragglers are missing buddies
            // TODO
            
            try
            {
                var databoxes = await DeserializeLogicObjects<LogicDatabox>(Path.Combine(EntitiesFolder, DataboxFile));
                AddEntities(databoxes);
                var spawnables = await DeserializeLogicObjects<LogicSpawnable>(Path.Combine(EntitiesFolder, SpawnablesFile));
                AddEntities(spawnables);
                var fragments = await DeserializeLogicObjects<LogicFragment>(Path.Combine(EntitiesFolder, FragmentsFile));
                AddEntities(fragments);
                var recipes = await DeserializeLogicObjects<LogicRecipe>(Path.Combine(EntitiesFolder, RecipesFile));
                AddEntities(recipes);

                var constructables = await DeserializeLogicObjects<LogicConstructable>(Path.Combine(EntitiesFolder, ConstructablesFile));
                AddEntities(constructables);
                var iitems = await DeserializeLogicObjects<LogicInventoryItem>(Path.Combine(EntitiesFolder, InvItemsFile));
                AddEntities(iitems);
                
                LinkRecipes(recipes);
                LinkSpawnables(fragments, iitems);
                ReplaceReferences(_entities);
            }
            catch (Exception ex)
            {
                _log.Error($"{ex.GetType()}: {ex.Message}\n{ex.StackTrace}");
            }
        }

        public static async Task<List<T>> DeserializeLogicObjects<T>(string fileName, params JsonConverter[] converters)
        {
            var json = await ReadFileContents(fileName);
            var logicObjects = JsonConvert.DeserializeObject<List<T>>(json, converters);
            return logicObjects;
        }

        private static async Task<string> ReadFileContents(string fileName)
        {
            var path = Path.Combine(Hootils.GetModDirectory(), "Assets", fileName);
            using StreamReader reader = new StreamReader(File.OpenRead(path));
            return await reader.ReadToEndAsync();
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

        private void LinkRecipes(List<LogicRecipe> recipes)
        {
            foreach (var recipe in recipes)
            {
                var blueprint = Find<LogicBlueprint>(recipe.TechType);
                if (blueprint != null)
                    recipe.AddBlueprint(blueprint);
                
                recipe.LinkVanillaRecipe(this, _log);
            }
        }

        private void LinkSpawnables(List<LogicFragment> fragments, List<LogicInventoryItem> items)
        {
            // Add spawnables to inventory items for cases like rubies or quartz.
            foreach (var item in items)
            {
                var spawnable = Find<LogicSpawnable>(item.TechType);
                if (spawnable != null)
                    item.AddSpawnable(spawnable);
            }

            // Add spawnables to fragments for cases like seamoth fragments.
            foreach (var fragment in fragments)
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