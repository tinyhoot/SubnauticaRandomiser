using System;
using System.Collections.Generic;

namespace SubnauticaRandomiser.Logic
{
    internal abstract class Mode
    {
        protected RandomiserConfig _config;
        protected Materials _materials;
        protected ProgressionTree _tree;
        protected Random _random;
        protected List<RandomiserIngredient> _ingredients = new List<RandomiserIngredient>();
        protected List<ETechTypeCategory> _blacklist = new List<ETechTypeCategory>();
        protected RandomiserRecipe _baseTheme;

        protected Mode(RandomiserConfig config, Materials materials, ProgressionTree tree, Random random)
        {
            _config = config;
            _materials = materials;
            _tree = tree;
            _random = random;

            _baseTheme = ChooseBaseTheme(100);
            LogHandler.Debug("Chosen " + _baseTheme.TechType.AsString() + " as base theme.");
        }

        internal abstract RandomiserRecipe RandomiseIngredients(RandomiserRecipe recipe);

        // Add an ingredient to the list of ingredients used to form a recipe,
        // but ensure its MaxUses field is respected.
        protected void AddIngredientWithMaxUsesCheck(RandomiserRecipe recipe, int amount)
        {
            _ingredients.Add(new RandomiserIngredient(recipe.TechType, amount));
            recipe._usedInRecipes++;

            if (!recipe.HasUsesLeft())
            {
                _materials.GetReachable().Remove(recipe);
                LogHandler.Debug("!   Removing " + recipe.TechType.AsString() + " from materials list due to max uses reached: " + recipe._usedInRecipes);
            }
        }

        protected RandomiserRecipe GetRandom(List<RandomiserRecipe> list, List<ETechTypeCategory> blacklist = null)
        {
            if (list == null || list.Count == 0)
                throw new InvalidOperationException("Failed to get valid recipe from materials list: list is null or empty.");

            RandomiserRecipe randomRecipe = null;
            while (true)
            {
                randomRecipe = list[_random.Next(0, list.Count)];

                if (blacklist != null && blacklist.Count > 0)
                {
                    if (blacklist.Contains(randomRecipe.Category))
                        continue;
                }
                break;
            }

            return randomRecipe;
        }

        // If base theming is enabled and this is a base piece, yield the base
        // theming ingredient.
        protected RandomiserRecipe CheckForBaseTheming(RandomiserRecipe recipe)
        {
            if (_config.bDoBaseTheming && _baseTheme != null && recipe.Category.Equals(ETechTypeCategory.BaseBasePieces))
                return _baseTheme;

            return null;
        }

        // If vanilla upgrade chains are enabled, yield that which this recipe
        // upgrades from (e.g. yields Knife when given HeatBlade recipe)
        protected RandomiserRecipe CheckForVanillaUpgrades(RandomiserRecipe recipe)
        {
            RandomiserRecipe result = null;

            if (_config.bVanillaUpgradeChains)
            {
                TechType basicUpgrade = _tree.GetUpgradeChain(recipe.TechType);
                if (!basicUpgrade.Equals(TechType.None))
                {
                    result = _materials.GetAll().Find(x => x.TechType.Equals(basicUpgrade));
                }
            }

            return result;
        }

        // Choose a theming ingredient for the base from among a range of easily
        // available options.
        private RandomiserRecipe ChooseBaseTheme(int depth)
        {
            List<RandomiserRecipe> options = new List<RandomiserRecipe>();

            options.AddRange(_materials.GetAll().FindAll(x => x.Category.Equals(ETechTypeCategory.RawMaterials)
                                                     && x.Depth < depth
                                                     && (x.Prerequisites == null || x.Prerequisites.Count == 0)
                                                     && x.MaxUsesPerGame == 0
                                                     && x.GetItemSize() == 1));

            if (_config.bUseFish)
            {
                options.AddRange(_materials.GetAll().FindAll(x => x.Category.Equals(ETechTypeCategory.Fish)
                                                         && x.Depth < depth
                                                         && (x.Prerequisites == null || x.Prerequisites.Count == 0)
                                                         && x.MaxUsesPerGame == 0
                                                         && x.GetItemSize() == 1));
            }

            return GetRandom(options);
        }

        protected void UpdateBlacklist(RandomiserRecipe recipe)
        {
            _blacklist = new List<ETechTypeCategory>();

            if (_config.iEquipmentAsIngredients == 0 || (_config.iEquipmentAsIngredients == 1 && recipe.CanFunctionAsIngredient()))
                _blacklist.Add(ETechTypeCategory.Equipment);
            if (_config.iToolsAsIngredients == 0 || (_config.iToolsAsIngredients == 1 && recipe.CanFunctionAsIngredient()))
                _blacklist.Add(ETechTypeCategory.Tools);
            if (_config.iUpgradesAsIngredients == 0 || (_config.iUpgradesAsIngredients == 1 && recipe.CanFunctionAsIngredient()))
            {
                _blacklist.Add(ETechTypeCategory.VehicleUpgrades);
                _blacklist.Add(ETechTypeCategory.WorkBenchUpgrades);
            }
        }
    }
}
