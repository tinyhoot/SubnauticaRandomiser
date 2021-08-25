using System;
using System.Collections.Generic;
using SMLHelper.V2.Handlers;

namespace SubnauticaRandomiser.Logic
{
    internal class ModeRandom : Mode
    {
        private List<RandomiserRecipe> _reachableMaterials;

        internal ModeRandom(RandomiserConfig config, Materials materials, ProgressionTree tree, Random random) : base(config, materials, tree, random)
        {
            _reachableMaterials = _materials.GetReachable();
        }

        // Fill a given recipe with ingredients. This algorithm mostly uses random
        // number generation to fill in the gaps.
        internal override RandomiserRecipe RandomiseIngredients(RandomiserRecipe recipe)
        {
            int number = _random.Next(1, _config.iMaxIngredientsPerRecipe + 1);
            int totalInvSize = 0;
            _ingredients = new List<RandomiserIngredient>();
            UpdateBlacklist(recipe);

            for (int i = 1; i <= number; i++)
            {
                RandomiserRecipe randomRecipe = GetRandom(_reachableMaterials, _blacklist);

                // Prevent duplicates.
                if (_ingredients.Exists(x => x.techType == randomRecipe.TechType))
                {
                    i--;
                    continue;
                }

                int max = FindMaximum(randomRecipe);

                RandomiserIngredient ingredient = new RandomiserIngredient(randomRecipe.TechType, _random.Next(1, max + 1));

                AddIngredientWithMaxUsesCheck(randomRecipe, ingredient.amount);
                totalInvSize += randomRecipe.GetItemSize() * ingredient.amount;

                LogHandler.Debug("    Adding ingredient: " + ingredient.techType.AsString() + ", " + ingredient.amount);

                if (totalInvSize > _config.iMaxInventorySizePerRecipe)
                {
                    LogHandler.Debug("!   Recipe is getting too large, stopping.");
                    break;
                }
            }

            recipe.Ingredients = _ingredients;
            recipe.CraftAmount = CraftDataHandler.GetTechData(recipe.TechType).craftAmount;
            return recipe;
        }

        private int FindMaximum(RandomiserRecipe recipe)
        {
            int max = _config.iMaxAmountPerIngredient;

            // Tools and upgrades do not stack, but if the recipe would
            // require several and you have more than one in inventory,
            // it will consume all of them.
            if (recipe.Category.Equals(ETechTypeCategory.Tools) || recipe.Category.Equals(ETechTypeCategory.VehicleUpgrades) || recipe.Category.Equals(ETechTypeCategory.WorkBenchUpgrades))
                max = 1;

            // Never require more than one (default) egg. That's tedious.
            if (recipe.Category.Equals(ETechTypeCategory.Eggs))
                max = _config.iMaxEggsAsSingleIngredient;

            return max;
        }
    }
}
