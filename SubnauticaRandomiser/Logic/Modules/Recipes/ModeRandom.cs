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
                    // *But* check if there are even any options left at all.
                    if (recipe.Recipe.Ingredients.Count >= validIngredients.Count - 1)
                        yield break;
                    
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

        public override TechType GetScrapMetalReplacement(IRandomHandler rng, List<LogicInventoryItem> validItems)
        {
            // Choosing any item with a recipe will always skip at least *some* progression, so do not include those.
            return rng.Choice(validItems.Where(ii => ii.Recipe is null).ToList()).TechType;
        }
    }
}
