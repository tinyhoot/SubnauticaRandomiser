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
        private const string FragmentsFile = "fragments.json";
        private const string RecipesFile = "recipes.json";
        private const string SpawnablesFile = "spawnables.json";
        private const string InvItemsFile = "inventoryItems.json";
        
        private PrefixLogHandler _log = PrefixLogHandler.Get("[EntityManager]");
        private List<LogicEntity> _entities = new List<LogicEntity>();

        /// <summary>
        /// Try to find a specific entity. Will return null if none match the search criteria.
        /// </summary>
        public T Find<T>(TechType techType) where T : LogicEntity
        {
            return _entities.OfType<T>().FirstOrDefault(ent => ent.TechType == techType);
        }

        /// <inheritdoc cref="Find{T}"/>
        public LogicEntity Find(Type type, TechType techType)
        {
            return _entities.FirstOrDefault(ent => ent.TechType == techType && ent.GetType() == type);
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

        public async Task ParseEntitiesFromDisk()
        {
            // Load the file and parse the baseline data for each entity
            // Perform linking - hook up recipes and blueprints, etc.
            // Set up entity dependencies
            // Validate that everything has been linked up and no stragglers are missing buddies
            // TODO
            
            try
            {
                var spawnables = await DeserializeLogicObjects<LogicSpawnable>(Path.Combine(EntitiesFolder, SpawnablesFile));
                _entities.AddRange(spawnables);
                var fragments = await DeserializeLogicObjects<LogicFragment>(Path.Combine(EntitiesFolder, FragmentsFile));
                _entities.AddRange(fragments);
                var recipes = await DeserializeLogicObjects<LogicRecipe>(Path.Combine(EntitiesFolder, RecipesFile));
                _entities.AddRange(recipes);
                
                var iitems = await DeserializeLogicObjects<LogicInventoryItem>(Path.Combine(EntitiesFolder, InvItemsFile));
                _entities.AddRange(iitems);
                
                LinkRecipesToBlueprints(recipes, new List<LogicBlueprint>(fragments));
                LinkSpawnables(spawnables, fragments, iitems);
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

        private void LinkRecipesToBlueprints(List<LogicRecipe> recipes, List<LogicBlueprint> blueprints)
        {
            foreach (var recipe in recipes)
            {
                var blueprint = blueprints.Find(bp => bp.TechType == recipe.TechType);
                if (blueprint != null)
                    recipe.AddBlueprint(blueprint);
            }
        }

        private void LinkSpawnables(List<LogicSpawnable> spawnables, List<LogicFragment> fragments, List<LogicInventoryItem> items)
        {
            // Add spawnables to inventory items for cases like rubies or quartz.
            foreach (var item in items)
            {
                var spawnable = spawnables.Find(spawn => spawn.TechType == item.TechType);
                if (spawnable != null)
                    item.AddSpawnable(spawnable);
            }

            // Add spawnables to fragments for cases like seamoth fragments.
            foreach (var fragment in fragments)
            {
                var spawnable = spawnables.Find(spawn => spawn.TechType == fragment.SpawnableTechType);
                if (spawnable != null)
                    fragment.Dependencies.Add(spawnable);
            }
        }
    }
}