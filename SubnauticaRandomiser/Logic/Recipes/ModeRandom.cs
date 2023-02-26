using System.Collections.Generic;
using SMLHelper.V2.Handlers;
using SubnauticaRandomiser.Objects;
using SubnauticaRandomiser.Objects.Enums;

namespace SubnauticaRandomiser.Logic.Recipes
{
    /// <summary>
    /// A mode for recipe randomisation with few to no checks in place. Unpredictable.
    /// </summary>
    internal class ModeRandom : Mode
    {
        internal ModeRandom(CoreLogic coreLogic, RecipeLogic recipeLogic) : base(coreLogic, recipeLogic)
        {
        }
        
        /// <summary>
        /// Fill a given recipe with ingredients in-place. This algorithm mostly uses pure RNG to fill in the gaps.
        /// </summary>
        /// <param name="entity">The recipe to randomise ingredients for.</param>
        /// <returns>The modified entity.</returns>
        public override LogicEntity RandomiseIngredients(LogicEntity entity)
        {
            int number = _random.Next(1, _config.iMaxIngredientsPerRecipe + 1);
            int totalInvSize = 0;
            _ingredients = new List<RandomiserIngredient>();
            UpdateBlacklist(entity);

            for (int i = 1; i <= number; i++)
            {
                LogicEntity ingredientEntity = GetRandom(_recipeLogic.ValidIngredients, _blacklist);

                // Prevent duplicates.
                if (_ingredients.Exists(x => x.techType == ingredientEntity.TechType))
                {
                    i--;
                    continue;
                }

                // Disallow the builder tool from being used in base pieces.
                if (entity.Category.IsBasePiece() && ingredientEntity.TechType.Equals(TechType.Builder))
                {
                    i--;
                    continue;
                }

                int max = FindMaximum(ingredientEntity);

                RandomiserIngredient ingredient = new RandomiserIngredient(ingredientEntity.TechType, _random.Next(1, max + 1));

                AddIngredientWithMaxUsesCheck(ingredientEntity, ingredient.amount);
                totalInvSize += ingredientEntity.GetItemSize() * ingredient.amount;

                _log.Debug($"[R] > Adding ingredient: {ingredient.techType.AsString()}, {ingredient.amount}");

                if (totalInvSize > _config.iMaxInventorySizePerRecipe)
                {
                    _log.Debug("[R] ! Recipe is getting too large, stopping.");
                    break;
                }
            }

            entity.Recipe.Ingredients = _ingredients;
            entity.Recipe.CraftAmount = CraftDataHandler.GetTechData(entity.TechType)?.craftAmount ?? 1;
            return entity;
        }

        /// <summary>
        /// Find the highest number allowed for the given ingredient.
        /// </summary>
        /// <param name="entity">The ingredient to consider.</param>
        /// <returns>A positive integer.</returns>
        private int FindMaximum(LogicEntity entity)
        {
            int max = _config.iMaxAmountPerIngredient;

            // Tools and upgrades do not stack, but if the recipe would require several and you have more than one in
            // inventory, it will consume all of them.
            if (entity.Category.Equals(TechTypeCategory.Tools) 
                || entity.Category.Equals(TechTypeCategory.VehicleUpgrades) 
                || entity.Category.Equals(TechTypeCategory.WorkBenchUpgrades))
                max = 1;

            // Never require more than one (default) egg. That's tedious.
            if (entity.Category.Equals(TechTypeCategory.Eggs))
                max = _config.iMaxEggsAsSingleIngredient;

            return max;
        }
    }
}
