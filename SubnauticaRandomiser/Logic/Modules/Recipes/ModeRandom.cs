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

        protected override IEnumerable<LogicIngredient> YieldRandomIngredients(IRandomHandler rng, LogicRecipe recipe,
            List<LogicIngredient> ingredients, List<LogicInventoryItem> validIngredients)
        {
            int number = rng.Next(1, _config.MaxIngredientsPerRecipe.Value + 1, _distribution);

            for (int i = 1; i <= number; i++)
            {
                LogicInventoryItem item = rng.Choice(validIngredients);

                // Prevent duplicates.
                if (ingredients.Any(ing => ing.Item.TechType == item.TechType))
                {
                    i--;
                    continue;
                }

                int max = FindMaxIngredientNum(item);
                yield return new LogicIngredient(item, rng.Next(1, max + 1, _distribution));
            }
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
