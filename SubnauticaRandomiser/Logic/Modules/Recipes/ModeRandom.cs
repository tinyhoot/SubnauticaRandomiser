using System.Collections.Generic;
using System.Linq;
using SubnauticaRandomiser.Configuration;
using SubnauticaRandomiser.Handlers;
using SubnauticaRandomiser.Interfaces;
using SubnauticaRandomiser.Logic.LogicObjects;
using ILogHandler = HootLib.Interfaces.ILogHandler;

namespace SubnauticaRandomiser.Logic.Modules.Recipes
{
    /// <summary>
    /// A mode for recipe randomisation with few to no checks in place. Unpredictable.
    /// </summary>
    internal class ModeRandom : Mode
    {
        protected override ILogHandler _log => PrefixLogHandler.Get("[RM-Random]");

        public ModeRandom(Config config, EntityManager manager, Dictionary<TechType, int> outpostPieces)
            : base(config, manager, outpostPieces)
        {
        }

        protected override IEnumerable<LogicInventoryItem> YieldRandomIngredients(IRandomHandler rng, LogicRecipe recipe,
            List<LogicInventoryItem> validIngredients)
        {
            int number = rng.Next(1, _config.MaxIngredientsPerRecipe.Value + 1, _distribution);

            for (int i = 1; i <= number; i++)
            {
                LogicInventoryItem item = rng.Choice(validIngredients);

                // Prevent duplicates.
                if (recipe.Recipe.Ingredients.Any(ing => ing.techType == item.TechType))
                {
                    i--;
                    continue;
                }
                
                yield return item;
            }
        }

        protected override int GetRandomIngredientAmt(IRandomHandler rng, LogicRecipe recipe, LogicInventoryItem ingredient)
        {
            int max = _config.MaxNumberPerIngredient.Value;
            return rng.Next(1, max + 1, _distribution);
        }

        public override TechType GetScrapMetalReplacement()
        {
            // TODO
            return TechType.AcidMushroom;
            
            // var options = _entityHandler.GetAllRawMaterials();
            // return _rng.Choice(options).TechType;
        }
    }
}
