using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HootLib;
using Newtonsoft.Json;
using SubnauticaRandomiser.Handlers;
using SubnauticaRandomiser.Logic.LogicObjects;
using SubnauticaRandomiser.Logic.LogicObjects.Transitions;
using SubnauticaRandomiser.Serialization.Converters;

namespace SubnauticaRandomiser.Logic
{
    /// <summary>
    /// Responsible for constructing, linking and providing access to <see cref="LogicEntity"/>.
    /// </summary>
    internal class EntityManager
    {
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
        /// Get all entities that are instances or subclasses of T.
        /// </summary>
        public IEnumerable<T> GetAllEntities<T>() where T : LogicEntity
        {
            return _entities.OfType<T>();
        }

        public async Task ParseFromDisk()
        {
            // Load the file and parse the baseline data for each entity
            // Perform linking - hook up recipes and blueprints, etc.
            // Set up entity dependencies
            // Validate that everything has been linked up and no stragglers are missing buddies
            // TODO
            var converter = new StringEntityConverter(this);

            try
            {
                var iitemsJson = await ReadFileContents(Path.Combine("Entities", InvItemsFile));
                var iitems = JsonConvert.DeserializeObject<List<LogicInventoryItem>>(iitemsJson);
                _log.Debug($"InventoryItems: {iitems.Count}");
                _entities.AddRange(iitems);
                
                var regionsJson = await ReadFileContents("regions.json");
                var regions = JsonConvert.DeserializeObject<List<Region>>(regionsJson, converter);
                _log.Debug($"Regions: {regions?.Count}");

                var transitionsJson = await ReadFileContents("transitions.json");
                var transitions = JsonConvert.DeserializeObject<List<Transition>>(transitionsJson, new StringRegionConverter(regions));
                _log.Debug($"Transitions: {transitions?.Count}");
            }
            catch (Exception ex)
            {
                _log.Error($"{ex.GetType()}: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private async Task<string> ReadFileContents(string fileName)
        {
            var path = Path.Combine(Hootils.GetModDirectory(), "Assets", fileName);
            using StreamReader reader = new StreamReader(File.OpenRead(path));
            return await reader.ReadToEndAsync();
        }

        private void LinkRecipesToBlueprints()
        {
            var recipes = GetAllEntities<LogicRecipe>();
            var blueprints = GetAllEntities<LogicBlueprint>().ToList();

            foreach (var recipe in recipes)
            {
                recipe.AddBlueprint(blueprints.Find(bp => bp.TechType == recipe.TechType));
            }
        }
    }
}