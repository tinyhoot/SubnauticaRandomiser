using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using HootLib.Interfaces;
using JetBrains.Annotations;
using Nautilus.Handlers;
using SubnauticaRandomiser.Configuration;
using SubnauticaRandomiser.Handlers;
using SubnauticaRandomiser.Interfaces;
using SubnauticaRandomiser.Objects;
using SubnauticaRandomiser.Objects.Enums;

namespace SubnauticaRandomiser.Logic.Modules.Recipes
{
    /// <summary>
    /// The base class for deciding how or which ingredients are chosen in recipe randomisation.
    /// </summary>
    internal abstract class Mode
    {
        protected readonly CoreLogic _coreLogic;
        protected readonly RecipeLogic _recipeLogic;
        protected Config _config => _coreLogic._Config;
        protected EntityHandler _entityHandler => _coreLogic.EntityHandler;
        protected IRandomHandler _random => _coreLogic.Random;
        protected ILogHandler _log;

        protected List<TechType> _blacklist = new List<TechType>();
        protected List<TechTypeCategory> _categoryBlacklist = new List<TechTypeCategory>();
        protected BaseTheme _baseTheme;
        private int _basicOutpostSize;
        protected RandomDistribution _distribution;

        protected Mode(CoreLogic coreLogic, RecipeLogic recipeLogic)
        {
            _coreLogic = coreLogic;
            _recipeLogic = recipeLogic;
            _log = PrefixLogHandler.Get("[R]");

            if (_config.BaseTheming.Value)
                _baseTheme = new BaseTheme(_entityHandler, _log, _random);
            _distribution = _config.DistributionWeighting.Value;
        }

        /// <summary>
        /// Fill a given recipe with ingredients in-place.
        /// </summary>
        /// <param name="recipe">The recipe to randomise ingredients for.</param>
        /// <returns>The same modified entity.</returns>
        public LogicEntity RandomiseIngredients(ref LogicEntity recipe)
        {
            List<RandomiserIngredient> ingredients = new List<RandomiserIngredient>();
            bool isDuplicate(TechType t) => ingredients.Exists(ing => ing.techType.Equals(t));
            UpdateBlacklist(recipe);

            int totalSize = HandleSpecialIngredients(ingredients, recipe);

            // Get ingredients from the subclass one at a time.
            foreach ((LogicEntity ingredient, int number) in YieldRandomIngredients(recipe, ingredients.AsReadOnly(), isDuplicate))
            {
                if (ingredients.Count > 0 && CheckForConfigStop(ingredients, recipe, totalSize))
                    break;
                if (ingredient is null || number < 1)
                    continue;

                // Ensure no number of ingredients can exceed the maximum config value.
                int max = FindMaximum(ingredient, totalSize);
                // If the maximum of allowable ingredients is less than 1, we hit a config limit and should stop.
                if (max <= 0)
                    break;
                int chosenNum = Math.Min(number, max);
                AddIngredientWithMaxUsesCheck(ingredients, ingredient, chosenNum);
                totalSize += ingredient.GetItemSize() * chosenNum;
                _log.Debug($"> Adding ingredient: {ingredient}, {chosenNum}, size: {totalSize}");
            }
            
            // Update the total size of everything needed to build a basic outpost.
            _basicOutpostSize += totalSize * _recipeLogic.BasicOutpostPieces.GetOrDefault(recipe.TechType, 0);

            recipe.Recipe.Ingredients = ingredients;
            recipe.Recipe.CraftAmount = CraftDataHandler.GetRecipeData(recipe.TechType)?.craftAmount ?? 1;
            return recipe;
        }

        /// <summary>
        /// Yield ingredients one at a time. The base mode inspects each ingredient and ensures that config values are
        /// respected across all deriving modes. It may also mandate an early stop without exhausting this method.<br/>
        /// A lazy approach using an iterator is strongly recommended.
        /// </summary>
        /// <param name="recipe">The recipe to randomise ingredients for.</param>
        /// <param name="ingredients">The existing ingredients already added before this method was called.</param>
        /// <param name="isDuplicate">A function that checks whether the given techtype is already present in the recipe.</param>
        /// <returns>The ingredients for the recipe.</returns>
        protected abstract IEnumerable<(LogicEntity, int)> YieldRandomIngredients(LogicEntity recipe,
            ReadOnlyCollection<RandomiserIngredient> ingredients, Func<TechType, bool> isDuplicate);

        /// <summary>
        /// Add an ingredient to the list of ingredients used to form a recipe, but ensure its MaxUses field is
        /// respected.
        /// </summary>
        /// <param name="ingredients">The current list of ingredients.</param>
        /// <param name="entity">The entity to add.</param>
        /// <param name="number">The number of uses to consume.</param>
        private void AddIngredientWithMaxUsesCheck(List<RandomiserIngredient> ingredients, LogicEntity entity, int number)
        {
            // Ensure that limited ingredients are not overused. Particularly
            // intended for cuddlefish.
            int remainder = entity.MaxUsesPerGame - entity.UsedInRecipes;
            if (entity.MaxUsesPerGame != 0 && remainder > 0 && remainder < number)
                number = remainder;

            ingredients.Add(new RandomiserIngredient(entity.TechType, number));
            entity.UsedInRecipes++;

            if (!entity.HasUsesLeft())
            {
                _recipeLogic.ValidIngredients.Remove(entity);
                _log.Debug($"! Removing {entity} ingredients list due to " + 
                           $"max uses reached: {entity.UsedInRecipes}");
                RemoveParentRecipes(entity);
            }
        }

        /// <summary>
        /// Check whether conditions have been reached that mandate an early stop as defined by config values.
        /// </summary>
        /// <param name="ingredients">The current list of ingredients.</param>
        /// <param name="entity">The recipe to randomise ingredients for.</param>
        /// <param name="totalSize">The current size required by all previously chosen ingredients for the recipe.</param>
        /// <returns>True if the loop needs to stop, false if it can continue running.</returns>
        private bool CheckForConfigStop(List<RandomiserIngredient> ingredients, LogicEntity entity, int totalSize)
        {
            // Respect the maximum number of ingredients set in the config.
            if (ingredients.Count >= _config.MaxIngredientsPerRecipe.Value)
            {
                _log.Debug("! Recipe has reached maximum allowed number of ingredients, stopping.");
                return true;
            }
            
            // If a recipe starts requiring too much space, shut it down early.
            if (totalSize >= _config.MaxInventorySizePerRecipe.Value)
            {
                _log.Debug("! Recipe is getting too large, stopping.");
                return true;
            }
            
            // For special case of outpost base parts, be conservative with ingredients.
            if (_recipeLogic.BasicOutpostPieces.ContainsKey(entity.TechType)
                && _basicOutpostSize > _config.MaxBasicOutpostSize.Value * 0.7)
            {
                _log.Debug("! Basic outpost size is getting too large, stopping.");
                return true;
            }

            return false;
        }
        
        /// <summary>
        /// Choose a base theme once off-loop randomisation begins.
        /// </summary>
        public void ChooseBaseTheme(int depth, bool useFish)
        {
            _baseTheme?.ChooseBaseTheme(depth, useFish);
        }

        /// <summary>
        /// Find the highest number of the given ingredient which the recipe can sustain given all config values.
        /// </summary>
        /// <param name="ingredient">The ingredient to consider.</param>
        /// <param name="totalSize">The total size of all ingredients added so far.</param>
        /// <returns>A positive integer.</returns>
        protected int FindMaximum(LogicEntity ingredient, int totalSize = 0)
        {
            if (totalSize >= _config.MaxInventorySizePerRecipe.Value)
                return 1;
            
            // Do not allow more ingredients than set in the config.
            int max = _config.MaxNumberPerIngredient.Value;
            // Account for how much space this new ingredient would take up.
            max = Math.Min(max, (_config.MaxInventorySizePerRecipe.Value - totalSize) / ingredient.GetItemSize());
            _log.Debug($"Calc max: {max}");
            
            // Tools and upgrades do not stack, but if the recipe would require several and you have more than one in
            // inventory, it will consume all of them.
            if (ingredient.Category.Equals(TechTypeCategory.Tools) 
                || ingredient.Category.Equals(TechTypeCategory.VehicleUpgrades) 
                || ingredient.Category.Equals(TechTypeCategory.WorkBenchUpgrades))
                max = Math.Min(max, 1);
            
            // Never require more than one (default) egg. That's tedious.
            if (ingredient.Category.Equals(TechTypeCategory.Eggs))
                max = Math.Min(max, _config.MaxEggsAsSingleIngredient.Value);

            return max;
        }

        /// <summary>
        /// Get the number of ingredients to be used for the base theme, if any.
        /// </summary>
        protected abstract int GetBaseThemeIngredientNumber(LogicEntity baseTheme);

        /// <summary>
        /// Get the TechType of the material to deconstruct scrap metal into.
        /// </summary>
        public abstract TechType GetScrapMetalReplacement();

        /// <summary>
        /// Get a random entity from a list, ensuring that it is not part of a previously prepared blacklist.
        /// TODO: Install safeguards to prevent infinite loops.
        /// </summary>
        /// <param name="list">The list to get a random element from.</param>
        /// <returns>A random, non-blacklisted element from the list.</returns>
        /// <exception cref="InvalidOperationException">Raised if the list is null or empty.</exception>
        [NotNull]
        protected LogicEntity GetRandom(ICollection<LogicEntity> list)
        {
            if (list == null || list.Count == 0)
                throw new InvalidOperationException("Failed to get valid entity from materials list: "
                                                    + "list is null or empty.");

            LogicEntity randomEntity;
            while (true)
            {
                randomEntity = _random.Choice(list);
                if (IsBlacklisted(randomEntity))
                    continue;
                
                break;
            }

            return randomEntity;
        }

        /// <summary>
        /// Handle any config options that result in specialised items being added to the recipe.
        /// </summary>
        /// <returns>The new total size of the recipe.</returns>
        private int HandleSpecialIngredients(List<RandomiserIngredient> ingredients, LogicEntity recipe)
        {
            int totalSize = 0;
            
            // Add the base theme first if necessary.
            if (_baseTheme?.GetThemeForEntity(recipe) != null)
            {
                LogicEntity theme = _baseTheme.GetBaseTheme();
                int number = GetBaseThemeIngredientNumber(theme);
                AddIngredientWithMaxUsesCheck(ingredients, theme, number);
                totalSize += _baseTheme.GetBaseTheme().GetItemSize() * number;
                _log.Debug($"> Added base theme {theme}.");
            }

            // If vanilla upgrade chains are set to be preserved, prioritise the thing this recipe upgrades from.
            LogicEntity vanilla;
            if (_config.VanillaUpgradeChains.Value && ((vanilla = _recipeLogic.GetBaseOfUpgrade(recipe.TechType, _entityHandler)) != null))
            {
                AddIngredientWithMaxUsesCheck(ingredients, vanilla, 1);
                totalSize += vanilla.GetItemSize();
                _log.Debug($"> Added upgrade base {vanilla}.");
            }

            return totalSize;
        }

        /// <summary>
        /// Checks whether the given ingredient is blacklisted for the recipe that is currently being randomised.
        /// </summary>
        /// <returns>True if the ingredient is blacklisted (and therefore invalid), false if it is not.</returns>
        protected bool IsBlacklisted(LogicEntity entity)
        {
            return (_categoryBlacklist?.Count > 0 && _categoryBlacklist.Contains(entity.Category)) 
                   || (_blacklist?.Count > 0 && _blacklist.Contains(entity.TechType));
        }

        /// <summary>
        /// Remove all entities from the valid ingredients list which contain the given entity as an ingredient.
        /// </summary>
        private void RemoveParentRecipes(LogicEntity entity)
        {
            int count = _recipeLogic.ValidIngredients.RemoveWhere(e =>
                e.Recipe?.Ingredients.Any(i => i.techType.Equals(entity.TechType)) ?? false);
            _log.Debug($"  Also removing {count} parent recipes.");
        }

        /// <summary>
        /// Set up the blacklist with entities that are not allowed to function as ingredients for the given entity.
        /// </summary>
        /// <param name="entity">The entity to build a blacklist against.</param>
        private void UpdateBlacklist(LogicEntity entity)
        {
            _blacklist = new List<TechType>();
            _categoryBlacklist = new List<TechTypeCategory>();

            if (_config.EquipmentAsIngredients.Value == IngredientInclusionLevel.Never
                || (_config.EquipmentAsIngredients.Value == IngredientInclusionLevel.TopLevelOnly && entity.CanFunctionAsIngredient()))
                _categoryBlacklist.Add(TechTypeCategory.Equipment);
            if (_config.ToolsAsIngredients.Value == IngredientInclusionLevel.Never
                || (_config.ToolsAsIngredients.Value == IngredientInclusionLevel.TopLevelOnly && entity.CanFunctionAsIngredient()))
                _categoryBlacklist.Add(TechTypeCategory.Tools);
            if (_config.UpgradesAsIngredients.Value == IngredientInclusionLevel.Never
                || (_config.UpgradesAsIngredients.Value == IngredientInclusionLevel.TopLevelOnly && entity.CanFunctionAsIngredient()))
            {
                _categoryBlacklist.Add(TechTypeCategory.ScannerRoom);
                _categoryBlacklist.Add(TechTypeCategory.VehicleUpgrades);
                _categoryBlacklist.Add(TechTypeCategory.WorkBenchUpgrades);
            }
            
            // Disallow the builder tool from being used in base pieces.
            if (entity.Category.IsBasePiece())
                _blacklist.Add(TechType.Builder);
        }
    }
}
