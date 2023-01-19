using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using SMLHelper.V2.Handlers;
using SubnauticaRandomiser.Objects;
using SubnauticaRandomiser.Objects.Enums;

namespace SubnauticaRandomiser.Logic.Recipes
{
    internal class ModeBalanced : Mode
    {
        private int _basicOutpostSize;
        private HashSet<LogicEntity> _validIngredients => _recipeLogic.ValidIngredients;

        public ModeBalanced(CoreLogic coreLogic, RecipeLogic recipeLogic) : base(coreLogic, recipeLogic)
        {
            _basicOutpostSize = 0;
        }
        
        /// <summary>
        /// Fill a given recipe with ingredients in-place. This class uses a value arithmetic to balance hard to reach 
        /// materials against easier ones, and tries to provide a well-rounded, curated experience.
        /// </summary>
        /// <param name="entity">The recipe to randomise ingredients for.</param>
        /// <returns>The modified entity.</returns>
        [NotNull]
        public override LogicEntity RandomiseIngredients(LogicEntity entity)
        {
            _ingredients = new List<RandomiserIngredient>();
            UpdateBlacklist(entity);
            int currentValue = 0;
            int totalSize = 0;

            _log.Debug("[R] Figuring out ingredients for " + entity);

            LogicEntity primaryIngredient = ChoosePrimaryIngredient(entity);
            AddIngredientWithMaxUsesCheck(primaryIngredient, 1);
            currentValue += primaryIngredient.Value;

            _log.Debug("[R] > Adding primary ingredient " + primaryIngredient);

            // Now fill up with random materials until the value threshold is more or less met, as defined by fuzziness.
            while ((entity.Value - currentValue) > (entity.Value * _config.dRecipeValueVariance / 2))
            {
                // If a config value mandates an early stop, stop.
                if (CheckForConfigStop(entity, totalSize))
                    break;

                (LogicEntity ingredient, int number) = ChooseSecondaryIngredient(entity, currentValue, totalSize);
                if (ingredient is null)
                    continue;
                
                AddIngredientWithMaxUsesCheck(ingredient, number);
                currentValue += ingredient.Value * number;
                totalSize += ingredient.GetItemSize() * number;

                _log.Debug($"[R] > Adding ingredient: {ingredient}, {number}");
            }

            // Update the total size of everything needed to build a basic outpost.
            if (_recipeLogic.BasicOutpostPieces.ContainsKey(entity.TechType))
                _basicOutpostSize += (totalSize * _recipeLogic.BasicOutpostPieces[entity.TechType]);
            
            _log.Debug($"[R] > Recipe is now valued {currentValue} out of {entity.Value}");
            entity.Value = currentValue;
            entity.Recipe.Ingredients = _ingredients;
            entity.Recipe.CraftAmount = CraftDataHandler.GetTechData(entity.TechType).craftAmount;
            return entity;
        }

        /// <summary>
        /// Check whether conditions have been reached that mandate an early stop as defined by config values.
        /// </summary>
        /// <param name="entity">The recipe to randomise ingredients for.</param>
        /// <param name="totalSize">The current size required by all previously chosen ingredients for the recipe.</param>
        /// <returns>True if the loop needs to stop, false if it can continue running.</returns>
        private bool CheckForConfigStop(LogicEntity entity, int totalSize)
        {
            // Respect the maximum number of ingredients set in the config.
            if (_ingredients.Count >= _config.iMaxIngredientsPerRecipe)
            {
                _log.Debug("[R] ! Recipe has reached maximum allowed number of ingredients, stopping.");
                return true;
            }
            
            // If a recipe starts requiring too much space, shut it down early.
            if (totalSize >= _config.iMaxInventorySizePerRecipe)
            {
                _log.Debug("[R] ! Recipe is getting too large, stopping.");
                return true;
            }
            
            // For special case of outpost base parts, be conservative with ingredients.
            if (_recipeLogic.BasicOutpostPieces.ContainsKey(entity.TechType)
                && _basicOutpostSize > _config.iMaxBasicOutpostSize * 0.7)
            {
                _log.Debug("[R] ! Basic outpost size is getting too large, stopping.");
                return true;
            }

            return false;
        }

        /// <summary>
        /// Find a primary ingredient for the recipe. Its value should be a percentage of the total value of the entire
        /// recipe as defined in the config, +-10%.
        /// </summary>
        /// <param name="entity">The recipe to randomise ingredients for.</param>
        /// <returns>The LogicEntity representing the primary ingredient.</returns>
        [NotNull]
        private LogicEntity ChoosePrimaryIngredient(LogicEntity entity)
        {
            double maxValue = entity.Value * (_config.dPrimaryIngredientValue + 0.1);
            double minValue = entity.Value * (_config.dPrimaryIngredientValue - 0.1);
            List<LogicEntity> pIngredientCandidates = _validIngredients
                .Where(e => minValue < e.Value && e.Value < maxValue && !_blacklist.Contains(e.Category)).ToList();

            // If we had no luck, just pick a random one.
            if (pIngredientCandidates.Count == 0)
                pIngredientCandidates.Add(GetRandom(_validIngredients, _blacklist));

            LogicEntity primaryIngredient = _random.Choice(pIngredientCandidates);

            // If base theming is enabled and this is a base piece, replace
            // the primary ingredient with a theming ingredient.
            primaryIngredient = _baseTheme?.GetBaseTheme(entity) ?? primaryIngredient;

            // If vanilla upgrade chains are set to be preserved, replace
            // the primary ingredient with the base item.
            primaryIngredient = _recipeLogic.GetBaseOfUpgrade(entity.TechType, _entityHandler) ?? primaryIngredient;
            
            // Disallow the builder tool from being used in base pieces.
            if (entity.Category.IsBasePiece() && primaryIngredient.TechType.Equals(TechType.Builder))
                primaryIngredient = ReplaceWithSimilarValue(primaryIngredient);

            return primaryIngredient;
        }

        /// <summary>
        /// Find a secondary ingredient for the recipe.
        /// </summary>
        /// <param name="entity">The recipe to randomise ingredients for.</param>
        /// <param name="currentValue">The current value of all previously chosen ingredients for the recipe.</param>
        /// <param name="totalSize">The current size required by all previously chosen ingredients for the recipe.</param>
        /// <returns>The LogicEntity representing the chosen ingredient along with how many.</returns>
        private (LogicEntity ingredient, int number) ChooseSecondaryIngredient(LogicEntity entity, int currentValue,
            int totalSize)
        {
            LogicEntity ingredient = GetRandom(_validIngredients, _blacklist);

            // Prevent duplicates.
            if (_ingredients.Exists(x => x.techType == ingredient.TechType))
                return (null, -1);

            // Disallow the builder tool from being used in base pieces.
            if (entity.Category.IsBasePiece() && ingredient.TechType.Equals(TechType.Builder))
                return (null, -1);

            // What's the maximum amount of this ingredient the recipe can still sustain?
            int max = FindMaximum(ingredient, entity.Value, currentValue);

            // Figure out how many to actually use.
            int number = _random.Next(1, max + 1);

            // If a recipe starts requiring a lot of inventory space to complete, try to minimise adding more
            // ingredients.
            if (totalSize + (ingredient.GetItemSize() * number) > _config.iMaxInventorySizePerRecipe)
                number = 1;
            
            return (ingredient, number);
        }
        
        /// <summary>
        /// Find the highest number of the given ingredient which the recipe can sustain.
        /// </summary>
        /// <param name="ingredient">The ingredient to consider.</param>
        /// <param name="targetValue">The overall target value of the recipe.</param>
        /// <param name="currentValue">The current value of all ingredients chosen thus far.</param>
        /// <returns>A positive integer.</returns>
        private int FindMaximum(LogicEntity ingredient, double targetValue, double currentValue)
        {
            int max = (int)((targetValue + ((targetValue * _config.dRecipeValueVariance) / 2)) - currentValue) / ingredient.Value;
            max = max > 0 ? max : 1;
            max = max > _config.iMaxAmountPerIngredient ? _config.iMaxAmountPerIngredient : max;

            // Tools and upgrades do not stack, but if the recipe would require several and you have more than one in
            // inventory, it will consume all of them.
            if (ingredient.Category.Equals(TechTypeCategory.Tools) 
                || ingredient.Category.Equals(TechTypeCategory.VehicleUpgrades) 
                || ingredient.Category.Equals(TechTypeCategory.WorkBenchUpgrades))
                max = 1;

            // Never require more than one (default) egg. That's tedious.
            if (ingredient.Category.Equals(TechTypeCategory.Eggs))
                max = _config.iMaxEggsAsSingleIngredient;

            return max;
        }
        
        /// <summary>
        /// Replace an undesirable ingredient with one of similar value. Start with a range of 10% in each direction,
        /// increasing if no valid replacement can be found.
        /// </summary>
        /// <param name="undesirable">The ingredient to replace.</param>
        /// <returns>A different ingredient of roughly similar value, or a random raw material as fallback.</returns>
        [NotNull]
        private LogicEntity ReplaceWithSimilarValue(LogicEntity undesirable)
        {
            int value = undesirable.Value;
            double range = 0.1;

            List<LogicEntity> betterOptions = new List<LogicEntity>();
            _log.Debug("[R] Replacing undesirable ingredient " + undesirable);

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
