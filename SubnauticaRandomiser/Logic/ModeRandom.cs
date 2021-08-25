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

        internal override RandomiserRecipe RandomiseIngredients(RandomiserRecipe recipe)
        {
            int number = _random.Next(1, 6);
            int totalInvSize = 0;
            _ingredients = new List<RandomiserIngredient>();
            UpdateBlacklist(recipe);

            for (int i = 1; i <= number; i++)
            {
                RandomiserRecipe r = GetRandom(_reachableMaterials, _blacklist);

                // Prevent duplicates.
                if (_ingredients.Exists(x => x.techType == r.TechType))
                {
                    i--;
                    continue;
                }
                RandomiserIngredient ing = new RandomiserIngredient(r.TechType, _random.Next(1, _config.iMaxAmountPerIngredient + 1));

                AddIngredientWithMaxUsesCheck(r, ing.amount);
                totalInvSize += r.GetItemSize() * ing.amount;

                LogHandler.Debug("    Adding ingredient: " + ing.techType.AsString() + ", " + ing.amount);

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
    }
}
