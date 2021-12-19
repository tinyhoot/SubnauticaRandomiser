using System;
using System.Collections.Generic;
using SMLHelper.V2.Handlers;
using SubnauticaRandomiser.RandomiserObjects;

namespace SubnauticaRandomiser.Logic
{
    internal class ModeRandom : Mode
    {
        private List<LogicEntity> _reachableMaterials;

        internal ModeRandom(RandomiserConfig config, Materials materials, ProgressionTree tree, Random random) : base(config, materials, tree, random)
        {
            _reachableMaterials = _materials.GetReachable();
        }

        // Fill a given recipe with ingredients. This algorithm mostly uses random
        // number generation to fill in the gaps.
        internal override LogicEntity RandomiseIngredients(LogicEntity entity)
        {
            int number = _random.Next(1, _config.iMaxIngredientsPerRecipe + 1);
            int totalInvSize = 0;
            _ingredients = new List<RandomiserIngredient>();
            UpdateBlacklist(entity);

            for (int i = 1; i <= number; i++)
            {
                LogicEntity ingredientEntity = GetRandom(_reachableMaterials, _blacklist);

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

                LogHandler.Debug("    Adding ingredient: " + ingredient.techType.AsString() + ", " + ingredient.amount);

                if (totalInvSize > _config.iMaxInventorySizePerRecipe)
                {
                    LogHandler.Debug("!   Recipe is getting too large, stopping.");
                    break;
                }
            }

            entity.Recipe.Ingredients = _ingredients;
            entity.Recipe.CraftAmount = CraftDataHandler.GetTechData(entity.TechType).craftAmount;
            return entity;
        }

        private int FindMaximum(LogicEntity entity)
        {
            int max = _config.iMaxAmountPerIngredient;

            // Tools and upgrades do not stack, but if the recipe would
            // require several and you have more than one in inventory,
            // it will consume all of them.
            if (entity.Category.Equals(ETechTypeCategory.Tools) || entity.Category.Equals(ETechTypeCategory.VehicleUpgrades) || entity.Category.Equals(ETechTypeCategory.WorkBenchUpgrades))
                max = 1;

            // Never require more than one (default) egg. That's tedious.
            if (entity.Category.Equals(ETechTypeCategory.Eggs))
                max = _config.iMaxEggsAsSingleIngredient;

            return max;
        }
    }
}
