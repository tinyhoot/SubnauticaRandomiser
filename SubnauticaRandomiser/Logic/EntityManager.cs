using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SubnauticaRandomiser.Logic.LogicObjects;

namespace SubnauticaRandomiser.Logic
{
    /// <summary>
    /// Responsible for constructing, linking and providing access to <see cref="LogicEntity"/>.
    /// </summary>
    internal class EntityManager
    {
        private List<LogicEntity> _entities = new List<LogicEntity>();

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
            throw new NotImplementedException();
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