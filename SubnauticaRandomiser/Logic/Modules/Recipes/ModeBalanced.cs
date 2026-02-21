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
    /// Aims to provide a balanced, curated, sane approach to recipe randomisation. Has many checks and balances in
    /// place to prevent recipes from becoming grindy or unfun.
    /// </summary>
    internal class ModeBalanced : Mode
    {
        protected override ILogHandler _log => PrefixLogHandler.Get("[RM-Balanced]");

        public ModeBalanced(Config config, EntityManager manager, Dictionary<TechType, int> outpostPieces)
            : base(config, manager, outpostPieces)
        {
        }

        protected override IEnumerable<LogicIngredient> YieldRandomIngredients(IRandomHandler rng, LogicRecipe recipe, 
            List<LogicIngredient> ingredients, List<LogicInventoryItem> validIngredients)
        {
            int currentValue = 0;
            
            // Only choose a primary ingredient if no ingredient has been chosen previously.
            if (ingredients.Count == 0)
            {
                var primaryIngredient = ChoosePrimaryIngredient(rng, recipe, validIngredients);
                yield return new LogicIngredient(primaryIngredient, 1);
                currentValue += primaryIngredient.Value;
                _log.Debug("> Adding primary ingredient " + primaryIngredient);
            }

            // Now fill up with random materials until the value threshold is more or less met, as defined by fuzziness.
            while ((recipe.TargetValue - currentValue) > (recipe.TargetValue * _config.RecipeValueVariance.Value / 2))
            {
                var ingredient = ChooseSecondaryIngredient(rng, recipe, validIngredients, currentValue);
                if (ingredient.Item is null || ingredients.Any(i => i.Item.TechType == ingredient.Item.TechType))
                    continue;
                
                yield return ingredient;
                currentValue += ingredient.Item.Value * ingredient.Amount;
            }

            _log.Debug($"> Recipe is now valued {currentValue} out of {recipe.TargetValue}");
        }

        public override TechType GetScrapMetalReplacement()
        {
            // TODO
            return TechType.Copper;

            // if (_baseTheme?.GetBaseTheme() != null)
            //     return _baseTheme.GetBaseTheme().TechType;
            //
            // var options = _entityHandler.GetAllRawMaterials();
            // return _rng.Choice(options).TechType;
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

            var primaryIngredient = rng.Choice(pIngredientCandidates);

            return primaryIngredient;
        }

        /// <summary>
        /// Find a secondary ingredient for the recipe.
        /// </summary>
        private LogicIngredient ChooseSecondaryIngredient(IRandomHandler rng, LogicRecipe recipe,
            List<LogicInventoryItem> validIngredients,  int currentValue)
        {
            var ingredient = rng.Choice(validIngredients);

            // What's the maximum number of this ingredient the recipe can still sustain?
            int max = FindMaximum(ingredient, recipe.TargetValue, currentValue);
            // Figure out how many to actually use.
            int number = rng.Next(1, max + 1, _distribution);

            return new LogicIngredient(ingredient, number);
        }
        
        /// <inheritdoc cref="Mode.FindMaxIngredientNum"/>
        private int FindMaximum(LogicInventoryItem ingredient, float targetValue, float currentValue)
        {
            int max = (int)((targetValue + ((targetValue * _config.RecipeValueVariance.Value) / 2)) - currentValue) / ingredient.Value;
            max = Mathf.Max(max, 1);
            max = Mathf.Min(_config.MaxNumberPerIngredient.Value, max);
            
            return max;
        }
        
        // /// <summary>
        // /// Replace an undesirable ingredient with one of similar value. Start with a range of 10% in each direction,
        // /// increasing if no valid replacement can be found.
        // /// </summary>
        // /// <param name="undesirable">The ingredient to replace.</param>
        // /// <returns>A different ingredient of roughly similar value, or a random raw material as fallback.</returns>
        // private LogicEntity ReplaceWithSimilarValue(LogicEntity undesirable)
        // {
        //     int value = undesirable.Value;
        //     double range = 0.1;
        //
        //     List<LogicEntity> betterOptions = new List<LogicEntity>();
        //     _log.Debug("Replacing undesirable ingredient " + undesirable);
        //
        //     // Progressively increase the search radius if no replacement is found,
        //     // but stop before it gets out of hand.
        //     while (betterOptions.Count == 0 && range < 1.0)
        //     {
        //         double maxValue = undesirable.Value + (undesirable.Value * range);
        //         double minValue = undesirable.Value - (undesirable.Value * range);
        //         // Add all items of the same category with value +- range%
        //         betterOptions.AddRange(_validIngredients.Where(x => x.Category.Equals(undesirable.Category)
        //                                                             && minValue < x.Value
        //                                                             && x.Value < maxValue
        //         ));
        //         range += 0.2;
        //     }
        //
        //     // If the loop above exited due to the range getting too large, just
        //     // use any unlocked raw material instead.
        //     if (betterOptions.Count == 0)
        //         betterOptions.AddRange(_validIngredients.Where(x => x.Category.Equals(TechTypeCategory.RawMaterials)));
        //
        //     return _rng.Choice(betterOptions);
        // }
    }
}
