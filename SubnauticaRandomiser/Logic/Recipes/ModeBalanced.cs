using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using SMLHelper.V2.Handlers;
using SubnauticaRandomiser.RandomiserObjects;
using SubnauticaRandomiser.RandomiserObjects.Enums;

namespace SubnauticaRandomiser.Logic.Recipes
{
    internal class ModeBalanced : Mode
    {
        private int _basicOutpostSize;
        private List<LogicEntity> _reachableMaterials;

        internal ModeBalanced(CoreLogic logic) : base(logic)
        {
            _basicOutpostSize = 0;
            _reachableMaterials = _materials.GetReachable();
        }
        
        /// <summary>
        /// Fill a given recipe with ingredients in-place. This class uses a value arithmetic to balance hard to reach 
        /// materials against easier ones, and tries to provide a well-rounded, curated experience.
        /// </summary>
        /// <param name="entity">The recipe to randomise ingredients for.</param>
        /// <returns>The modified entity.</returns>
        [NotNull]
        internal override LogicEntity RandomiseIngredients(LogicEntity entity)
        {
            _ingredients = new List<RandomiserIngredient>();
            UpdateBlacklist(entity);
            double targetValue = entity.Value;
            int currentValue = 0;
            entity.Value = 0;
            int totalSize = 0;

            LogHandler.Debug("[R] Figuring out ingredients for " + entity);

            LogicEntity primaryIngredient = ChoosePrimaryIngredient(entity, targetValue);

            // Disallow the builder tool from being used in base pieces.
            if (entity.Category.IsBasePiece() && primaryIngredient.TechType.Equals(TechType.Builder))
                primaryIngredient = ReplaceWithSimilarValue(primaryIngredient);

            AddIngredientWithMaxUsesCheck(primaryIngredient, 1);
            currentValue += primaryIngredient.Value;

            LogHandler.Debug("[R] > Adding primary ingredient " + primaryIngredient);

            // Now fill up with random materials until the value threshold is more or less met, as defined by fuzziness.
            // Using a do-while since we want this to happen at least once.
            do
            {
                LogicEntity ingredient = GetRandom(_reachableMaterials, _blacklist);

                // Prevent duplicates.
                if (_ingredients.Exists(x => x.techType == ingredient.TechType))
                    continue;

                // Disallow the builder tool from being used in base pieces.
                if (entity.Category.IsBasePiece() && ingredient.TechType.Equals(TechType.Builder))
                    continue;

                // What's the maximum amount of this ingredient the recipe can
                // still sustain?
                int max = FindMaximum(ingredient, targetValue, currentValue);

                // Figure out how many to actually use.
                int number = _random.Next(1, max + 1);

                // If a recipe starts requiring a lot of inventory space to
                // complete, try to minimise adding more ingredients.
                if (totalSize + (ingredient.GetItemSize() * number) > _config.iMaxInventorySizePerRecipe)
                    number = 1;

                AddIngredientWithMaxUsesCheck(ingredient, number);
                currentValue += ingredient.Value * number;
                totalSize += ingredient.GetItemSize() * number;

                LogHandler.Debug($"[R] > Adding ingredient: {ingredient}, {number}");

                // If a recipe starts getting out of hand, shut it down early.
                if (totalSize >= _config.iMaxInventorySizePerRecipe)
                {
                    LogHandler.Debug("[R] ! Recipe is getting too large, stopping.");
                    break;
                }
                
                // Same thing for special case of outpost base parts.
                if (_tree.BasicOutpostPieces.ContainsKey(entity.TechType) && _basicOutpostSize > _config.iMaxBasicOutpostSize * 0.6)
                {
                    LogHandler.Debug("[R] ! Basic outpost size is getting too large, stopping.");
                    break;
                }
                
                // Also, respect the maximum number of ingredients set in the config.
                if (_config.iMaxIngredientsPerRecipe <= _ingredients.Count)
                {
                    LogHandler.Debug("[R] ! Recipe has reached maximum allowed number of ingredients, stopping.");
                    break;
                }
            } while ((targetValue - currentValue) > (targetValue * _config.dRecipeValueVariance / 2));

            // Update the total size of everything needed to build a basic outpost.
            if (_tree.BasicOutpostPieces.ContainsKey(entity.TechType))
                _basicOutpostSize += (totalSize * _tree.BasicOutpostPieces[entity.TechType]);

            entity.Value = currentValue;
            LogHandler.Debug($"[R] > Recipe is now valued {currentValue} out of {targetValue}");

            entity.Recipe.Ingredients = _ingredients;
            entity.Recipe.CraftAmount = CraftDataHandler.GetTechData(entity.TechType).craftAmount;
            return entity;
        }
        
        /// <summary>
        /// Find a primary ingredient for the recipe. Its value should be a percentage of the total value of the entire
        /// recipe as defined in the config, +-10%.
        /// </summary>
        /// <param name="entity">The recipe to randomise ingredients for.</param>
        /// <param name="targetValue">The target value of all ingredients for the recipe.</param>
        /// <returns>The randomised recipe, modified in-place.</returns>
        [NotNull]
        private LogicEntity ChoosePrimaryIngredient(LogicEntity entity, double targetValue)
        {
            List<LogicEntity> pIngredientCandidates = _reachableMaterials.FindAll(
                                                                     x => (targetValue * (_config.dPrimaryIngredientValue + 0.1)) > x.Value
                                                                       && (targetValue * (_config.dPrimaryIngredientValue - 0.1)) < x.Value
                                                                       && !_blacklist.Contains(x.Category)
                                                                       );

            // If we had no luck, just pick a random one.
            if (pIngredientCandidates.Count == 0)
                pIngredientCandidates.Add(GetRandom(_reachableMaterials, _blacklist));

            LogicEntity primaryIngredient = GetRandom(pIngredientCandidates);

            // If base theming is enabled and this is a base piece, replace
            // the primary ingredient with a theming ingredient.
            primaryIngredient = CheckForBaseTheming(entity) ?? primaryIngredient;

            // If vanilla upgrade chains are set to be preserved, replace
            // the primary ingredient with the base item.
            primaryIngredient = CheckForVanillaUpgrades(entity) ?? primaryIngredient;

            return primaryIngredient;
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
            if (ingredient.Category.Equals(ETechTypeCategory.Tools) 
                || ingredient.Category.Equals(ETechTypeCategory.VehicleUpgrades) 
                || ingredient.Category.Equals(ETechTypeCategory.WorkBenchUpgrades))
                max = 1;

            // Never require more than one (default) egg. That's tedious.
            if (ingredient.Category.Equals(ETechTypeCategory.Eggs))
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
            LogHandler.Debug("[R] Replacing undesirable ingredient " + undesirable);

            // Progressively increase the search radius if no replacement is found,
            // but stop before it gets out of hand.
            while (betterOptions.Count == 0 && range < 1.0)
            {
                // Add all items of the same category with value +- range%
                betterOptions.AddRange(_reachableMaterials.FindAll(x => x.Category.Equals(undesirable.Category)
                                                                     && x.Value < undesirable.Value + (undesirable.Value * range)
                                                                     && x.Value > undesirable.Value - (undesirable.Value * range)
                                                                     ));
                range += 0.2;
            }

            // If the loop above exited due to the range getting too large, just
            // use any unlocked raw material instead.
            if (betterOptions.Count == 0)
                betterOptions.AddRange(_reachableMaterials.FindAll(x => x.Category.Equals(ETechTypeCategory.RawMaterials)));

            return GetRandom(betterOptions);
        }
    }
}
