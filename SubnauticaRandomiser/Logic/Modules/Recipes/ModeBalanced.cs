using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using SubnauticaRandomiser.Objects;
using SubnauticaRandomiser.Objects.Enums;
using UnityEngine;

namespace SubnauticaRandomiser.Logic.Modules.Recipes
{
    /// <summary>
    /// Aims to provide a balanced, curated, sane approach to recipe randomisation. Has many checks and balances in
    /// place to prevent recipes from becoming grindy or unfun.
    /// </summary>
    internal class ModeBalanced : Mode
    {
        private HashSet<LogicEntity> _validIngredients => _recipeLogic.ValidIngredients;

        public ModeBalanced(CoreLogic coreLogic, RecipeLogic recipeLogic) : base(coreLogic, recipeLogic) { }
        
        protected override IEnumerable<(LogicEntity, int)> YieldRandomIngredients(LogicEntity recipe,
            ReadOnlyCollection<RandomiserIngredient> ingredients, Func<TechType, bool> isDuplicate)
        {
            int currentValue = 0;
            
            // Only choose a primary ingredient if no ingredient has been chosen previously.
            if (ingredients.Count > 1)
            {
                foreach (var ingredient in ingredients)
                {
                    LogicEntity entity = _entityHandler.GetEntity(ingredient.techType);
                    if (entity != null)
                        currentValue += entity.Value * ingredient.amount;
                }
            }
            else
            {
                LogicEntity primaryIngredient = ChoosePrimaryIngredient(recipe);
                yield return (primaryIngredient, 1);
                currentValue += primaryIngredient.Value;
                _log.Debug("> Adding primary ingredient " + primaryIngredient);
            }

            // Now fill up with random materials until the value threshold is more or less met, as defined by fuzziness.
            while ((recipe.Value - currentValue) > (recipe.Value * _config.RecipeValueVariance.Value / 2))
            {
                (LogicEntity ingredient, int number) = ChooseSecondaryIngredient(recipe, currentValue);
                if (ingredient is null || isDuplicate(ingredient.TechType))
                    continue;
                
                yield return (ingredient, number);
                currentValue += ingredient.Value * number;
            }

            _log.Debug($"> Recipe is now valued {currentValue} out of {recipe.Value}");
            recipe.Value = currentValue;
        }

        public override TechType GetScrapMetalReplacement()
        {
            if (_baseTheme?.GetBaseTheme() != null)
                return _baseTheme.GetBaseTheme().TechType;

            var options = _entityHandler.GetAllRawMaterials();
            return _random.Choice(options).TechType;
        }

        /// <summary>
        /// Find a primary ingredient for the recipe. Its value should be a percentage of the total value of the entire
        /// recipe as defined in the config, +-10%.
        /// </summary>
        /// <param name="entity">The recipe to randomise ingredients for.</param>
        /// <returns>The LogicEntity representing the primary ingredient.</returns>
        private LogicEntity ChoosePrimaryIngredient(LogicEntity entity)
        {
            double maxValue = entity.Value * (_config.PrimaryIngredientValue.Value + 0.1);
            double minValue = entity.Value * (_config.PrimaryIngredientValue.Value - 0.1);
            List<LogicEntity> pIngredientCandidates = _validIngredients
                .Where(e => minValue < e.Value && e.Value < maxValue && !IsBlacklisted(e)).ToList();

            // If we had no luck, just pick a random one.
            if (pIngredientCandidates.Count == 0)
                pIngredientCandidates.Add(GetRandom(_validIngredients));

            LogicEntity primaryIngredient = _random.Choice(pIngredientCandidates);

            return primaryIngredient;
        }

        /// <summary>
        /// Find a secondary ingredient for the recipe.
        /// </summary>
        /// <param name="entity">The recipe to randomise ingredients for.</param>
        /// <param name="currentValue">The current value of all previously chosen ingredients for the recipe.</param>
        /// <returns>The LogicEntity representing the chosen ingredient along with how many.</returns>
        private (LogicEntity ingredient, int number) ChooseSecondaryIngredient(LogicEntity entity, int currentValue)
        {
            LogicEntity ingredient = GetRandom(_validIngredients);

            // What's the maximum number of this ingredient the recipe can still sustain?
            int max = FindMaximum(ingredient, entity.Value, currentValue);
            // Figure out how many to actually use.
            int number = _random.Next(1, max + 1, _distribution);

            return (ingredient, number);
        }
        
        /// <inheritdoc cref="Mode.FindMaximum"/>
        /// <param name="targetValue">The overall target value of the recipe.</param>
        /// <param name="currentValue">The current value of all ingredients chosen thus far.</param>
        private int FindMaximum(LogicEntity ingredient, float targetValue, float currentValue)
        {
            int max = (int)((targetValue + ((targetValue * _config.RecipeValueVariance.Value) / 2)) - currentValue) / ingredient.Value;
            max = Mathf.Max(max, 1);
            max = Mathf.Min(_config.MaxNumberPerIngredient.Value, max);
            
            return max;
        }

        protected override int GetBaseThemeIngredientNumber(LogicEntity baseTheme)
        {
            return _random.Next(1, (int)Mathf.Ceil(_config.MaxNumberPerIngredient.Value / 2f), _distribution);
        }
        
        /// <summary>
        /// Replace an undesirable ingredient with one of similar value. Start with a range of 10% in each direction,
        /// increasing if no valid replacement can be found.
        /// </summary>
        /// <param name="undesirable">The ingredient to replace.</param>
        /// <returns>A different ingredient of roughly similar value, or a random raw material as fallback.</returns>
        private LogicEntity ReplaceWithSimilarValue(LogicEntity undesirable)
        {
            int value = undesirable.Value;
            double range = 0.1;

            List<LogicEntity> betterOptions = new List<LogicEntity>();
            _log.Debug("Replacing undesirable ingredient " + undesirable);

            // Progressively increase the search radius if no replacement is found,
            // but stop before it gets out of hand.
            while (betterOptions.Count == 0 && range < 1.0)
            {
                double maxValue = undesirable.Value + (undesirable.Value * range);
                double minValue = undesirable.Value - (undesirable.Value * range);
                // Add all items of the same category with value +- range%
                betterOptions.AddRange(_validIngredients.Where(x => x.Category.Equals(undesirable.Category)
                                                                    && minValue < x.Value
                                                                    && x.Value < maxValue
                ));
                range += 0.2;
            }

            // If the loop above exited due to the range getting too large, just
            // use any unlocked raw material instead.
            if (betterOptions.Count == 0)
                betterOptions.AddRange(_validIngredients.Where(x => x.Category.Equals(TechTypeCategory.RawMaterials)));

            return _random.Choice(betterOptions);
        }
    }
}
