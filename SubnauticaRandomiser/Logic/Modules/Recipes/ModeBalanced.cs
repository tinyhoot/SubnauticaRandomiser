using System.Collections.Generic;
using System.Linq;
using SubnauticaRandomiser.Configuration;
using SubnauticaRandomiser.Handlers;
using SubnauticaRandomiser.Interfaces;
using SubnauticaRandomiser.Logic.LogicObjects;
using UnityEngine;
using ILogHandler = HootLib.Interfaces.ILogHandler;

namespace SubnauticaRandomiser.Logic.Modules.Recipes
{
    /// <summary>
    /// Aims to provide a balanced, curated, sane approach to recipe randomisation. Has checks and balances in
    /// place to prevent recipes from becoming grindy or unfun.
    /// </summary>
    internal class ModeBalanced : Mode
    {
        protected override ILogHandler _log => PrefixLogHandler.Get("[RM-Balanced]");

        public ModeBalanced(Config config, EntityManager manager, Dictionary<TechType, int> outpostPieces)
            : base(config, manager, outpostPieces)
        {
        }

        protected override IEnumerable<LogicInventoryItem> YieldRandomIngredients(IRandomHandler rng, LogicRecipe recipe, 
            List<LogicInventoryItem> validIngredients)
        {
            // Only choose a primary ingredient if no ingredient has been chosen previously.
            if (recipe.Recipe.Ingredients.Count == 0)
            {
                var primaryIngredient = ChoosePrimaryIngredient(rng, recipe, validIngredients);
                yield return primaryIngredient;
                _log.Debug("> Adding primary ingredient " + primaryIngredient);
            }

            // Now fill up with random materials until the value threshold is more or less met, as defined by fuzziness.
            while ((recipe.TargetValue - recipe.AssignedValue) > (recipe.TargetValue * _config.RecipeValueVariance.Value / 2))
            {
                // Failsafe, otherwise this would hang.
                if (recipe.Recipe.Ingredients.Count >= validIngredients.Count - 1)
                    yield break;
                
                var ingredient = rng.Choice(validIngredients);
                if (ingredient is null || recipe.Recipe.Ingredients.Any(i => i.techType == ingredient.TechType))
                    continue;
                
                yield return ingredient;
            }
        }

        protected override int GetRandomIngredientAmt(IRandomHandler rng, LogicRecipe recipe, LogicInventoryItem ingredient)
        {
            return rng.Next(1, FindMaximum(ingredient, recipe.TargetValue, recipe.AssignedValue));
        }

        public override TechType GetScrapMetalReplacement(IRandomHandler rng, List<LogicInventoryItem> validItems)
        {
            // Only include spawnables that are accessible early on.
            var options = validItems.Where(ii => ii.Recipe is null && ii.Sphere <= 1);
            return rng.Choice(options.ToList()).TechType;
        }

        /// <summary>
        /// Find a primary ingredient for the recipe. Its value should be a percentage of the total value of the entire
        /// recipe as defined in the config, +-10%.
        /// </summary>
        private LogicInventoryItem ChoosePrimaryIngredient(IRandomHandler rng, LogicRecipe recipe,
            List<LogicInventoryItem> validIngredients)
        {
            double maxValue = recipe.TargetValue * (_config.PrimaryIngredientValue.Value + 0.1);
            double minValue = recipe.TargetValue * (_config.PrimaryIngredientValue.Value - 0.1);
            List<LogicInventoryItem> pIngredientCandidates = validIngredients
                .Where(e => minValue < e.Value && e.Value < maxValue).ToList();

            // If we had no luck, just pick a random one.
            if (pIngredientCandidates.Count == 0)
                pIngredientCandidates.Add(rng.Choice(validIngredients));

            return rng.Choice(pIngredientCandidates);
        }
        
        /// <summary>
        /// Find the maximum amount of one ingredient the recipe can contain.
        /// </summary>
        private int FindMaximum(LogicInventoryItem ingredient, float targetValue, float currentValue)
        {
            int max = (int)((targetValue + ((targetValue * _config.RecipeValueVariance.Value) / 2)) - currentValue) / ingredient.Value;
            max = Mathf.Max(max, 1);
            max = Mathf.Min(_config.MaxNumberPerIngredient.Value, max);
            
            return max;
        }
    }
}
