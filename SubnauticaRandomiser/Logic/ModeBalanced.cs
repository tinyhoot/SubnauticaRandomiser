using System;
using System.Collections.Generic;
using SMLHelper.V2.Handlers;

namespace SubnauticaRandomiser.Logic
{
    internal class ModeBalanced : Mode
    {
        private int _basicOutpostSize;
        private List<RandomiserRecipe> _reachableMaterials;

        internal ModeBalanced(RandomiserConfig config, Materials materials, ProgressionTree tree, Random random) : base(config, materials, tree, random)
        {
            _basicOutpostSize = 0;
            _reachableMaterials = _materials.GetReachable();
        }

        // Fill a given recipe with ingredients. This class uses a value arithmetic
        // to balance hard to reach materials against easier ones, and tries to
        // provide a well-rounded, curated experience.
        internal override RandomiserRecipe RandomiseIngredients(RandomiserRecipe recipe)
        {
            _ingredients = new List<RandomiserIngredient>();
            UpdateBlacklist(recipe);
            double targetValue = recipe.Value;
            int currentValue = 0;
            recipe.Value = 0;
            int totalSize = 0;

            LogHandler.Debug("Figuring out ingredients for " + recipe.TechType.AsString());

            RandomiserRecipe primaryIngredient = ChoosePrimaryIngredient(recipe, targetValue);

            // Disallow the builer tool from being used in base pieces.
            if (recipe.Category.IsBasePiece() && primaryIngredient.TechType.Equals(TechType.Builder))
                primaryIngredient = ReplaceWithSimilarValue(primaryIngredient);

            AddIngredientWithMaxUsesCheck(primaryIngredient, 1);
            currentValue += primaryIngredient.Value;

            LogHandler.Debug("    Adding primary ingredient " + primaryIngredient.TechType.AsString());

            // Now fill up with random materials until the value threshold
            // is more or less met, as defined by fuzziness.
            // Converted to do-while since we want this to happen at least once.
            do
            {
                RandomiserRecipe ingredient = GetRandom(_reachableMaterials, _blacklist);

                // Prevent duplicates.
                if (_ingredients.Exists(x => x.techType == ingredient.TechType))
                    continue;

                // Disallow the builder tool from being used in base pieces.
                if (recipe.Category.IsBasePiece() && ingredient.TechType.Equals(TechType.Builder))
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

                LogHandler.Debug("    Adding ingredient: " + ingredient.TechType.AsString() + ", " + number);

                // If a recipe starts getting out of hand, shut it down early.
                if (totalSize >= _config.iMaxInventorySizePerRecipe)
                {
                    LogHandler.Debug("!   Recipe is getting too large, stopping.");
                    break;
                }
                // Same thing for special case of outpost base parts.
                if (_tree.BasicOutpostPieces.ContainsKey(recipe.TechType) && _basicOutpostSize > _config.iMaxBasicOutpostSize * 0.6)
                {
                    LogHandler.Debug("!   Basic outpost size is getting too large, stopping.");
                    break;
                }
                // Also, respect the maximum number of ingredients set in the config.
                if (_config.iMaxIngredientsPerRecipe <= _ingredients.Count)
                {
                    LogHandler.Debug("!   Recipe has reached maximum allowed number of ingredients, stopping.");
                    break;
                }
            } while ((targetValue - currentValue) > (targetValue * _config.dFuzziness / 2));

            // Update the total size of everything needed to build a basic outpost.
            if (_tree.BasicOutpostPieces.ContainsKey(recipe.TechType))
                _basicOutpostSize += (totalSize * _tree.BasicOutpostPieces[recipe.TechType]);

            recipe.Value = currentValue;
            LogHandler.Debug("    Recipe is now valued " + currentValue + " out of " + targetValue);

            recipe.Ingredients = _ingredients;
            recipe.CraftAmount = CraftDataHandler.GetTechData(recipe.TechType).craftAmount;
            return recipe;
        }

        // Find a primary ingredient for the recipe. Its value should be a
        // percentage of the total value of the entire recipe as defined in
        // the config, +-10%.
        private RandomiserRecipe ChoosePrimaryIngredient(RandomiserRecipe recipe, double targetValue)
        {
            List<RandomiserRecipe> pIngredientCandidates = _reachableMaterials.FindAll(
                                                                     x => (targetValue * (_config.dIngredientRatio + 0.1)) > x.Value
                                                                       && (targetValue * (_config.dIngredientRatio - 0.1)) < x.Value
                                                                       && !_blacklist.Contains(x.Category)
                                                                       );

            // If we had no luck, just pick a random one.
            if (pIngredientCandidates.Count == 0)
                pIngredientCandidates.Add(GetRandom(_reachableMaterials, _blacklist));

            RandomiserRecipe primaryIngredient = GetRandom(pIngredientCandidates);

            // If base theming is enabled and this is a base piece, replace
            // the primary ingredient with a theming ingredient.
            primaryIngredient = CheckForBaseTheming(recipe) ?? primaryIngredient;

            // If vanilla upgrade chains are set to be preserved, replace
            // the primary ingredient with the base item.
            primaryIngredient = CheckForVanillaUpgrades(recipe) ?? primaryIngredient;

            return primaryIngredient;
        }

        // What is the maximum amount of this ingredient the recipe can sustain?
        private int FindMaximum(RandomiserRecipe ingredient, double targetValue, double currentValue)
        {
            int max = (int)((targetValue + targetValue * _config.dFuzziness / 2) - currentValue) / ingredient.Value;
            max = max > 0 ? max : 1;
            max = max > _config.iMaxAmountPerIngredient ? _config.iMaxAmountPerIngredient : max;

            // Tools and upgrades do not stack, but if the recipe would
            // require several and you have more than one in inventory,
            // it will consume all of them.
            if (ingredient.Category.Equals(ETechTypeCategory.Tools) || ingredient.Category.Equals(ETechTypeCategory.VehicleUpgrades) || ingredient.Category.Equals(ETechTypeCategory.WorkBenchUpgrades))
                max = 1;

            // Never require more than one (default) egg. That's tedious.
            if (ingredient.Category.Equals(ETechTypeCategory.Eggs))
                max = _config.iMaxEggsAsSingleIngredient;

            return max;
        }

        // Replace an undesirable ingredient with one of similar value.
        // Start with a range of 10% in each direction, increasing if no valid
        // replacement can be found.
        private RandomiserRecipe ReplaceWithSimilarValue(RandomiserRecipe undesirable)
        {
            int value = undesirable.Value;
            double range = 0.1;

            List<RandomiserRecipe> betterOptions = new List<RandomiserRecipe>();
            LogHandler.Debug("Replacing undesirable ingredient " + undesirable.TechType.AsString());

            // Progressively increase the search radius if no replacement is found,
            // but stop before it gets out of hand.
            while (betterOptions.Count == 0 && range < 1.0)
            {
                // Add all items of the same category with value +- range%
                betterOptions.AddRange(_reachableMaterials.FindAll(x => x.Category.Equals(undesirable.Category)
                                                                     && x.Value < undesirable.Value + undesirable.Value * range
                                                                     && x.Value > undesirable.Value - undesirable.Value * range
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
