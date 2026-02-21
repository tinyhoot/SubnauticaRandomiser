using System;
using System.Collections.Generic;
using System.Linq;
using Nautilus.Handlers;
using SubnauticaRandomiser.Configuration;
using SubnauticaRandomiser.Interfaces;
using SubnauticaRandomiser.Logic.LogicObjects;
using SubnauticaRandomiser.Objects.Enums;
using UnityEngine;
using ILogHandler = HootLib.Interfaces.ILogHandler;

namespace SubnauticaRandomiser.Logic.Modules.Recipes
{
    /// <summary>
    /// The base class for deciding how or which ingredients are chosen in recipe randomisation.
    /// </summary>
    internal abstract class Mode
    {
        protected abstract ILogHandler _log { get; }
        protected Config _config;
        protected EntityManager _entityManager;
        private Dictionary<TechType, int> _outpostPieces;
        private int _outpostSize;
        protected RandomDistribution _distribution;

        protected Mode(Config config, EntityManager manager, Dictionary<TechType, int> outpostPieces)
        {
            _config = config;
            _entityManager = manager;
            _outpostPieces = outpostPieces;
            _distribution = _config.DistributionWeighting.Value;
        }

        /// <summary>
        /// Fill a given recipe with ingredients in-place.
        /// </summary>
        /// <param name="rng">The RNG of this seed.</param>
        /// <param name="recipe">The recipe to randomise ingredients for.</param>
        /// <param name="validIngredients">All valid ingredients that can be chosen for the recipe.</param>
        /// <returns>The same modified entity.</returns>
        public LogicRecipe RandomiseIngredients(IRandomHandler rng, LogicRecipe recipe, List<LogicInventoryItem> validIngredients)
        {
            recipe.Recipe.Ingredients = new List<Ingredient>();
            List<LogicIngredient> ingredients = new List<LogicIngredient>();
            int totalSize = 0;
            int totalValue = 0;

            // Get ingredients from the subclass one at a time.
            foreach (var ingredient in YieldRandomIngredients(rng, recipe, ingredients, validIngredients))
            {
                if (ingredients.Count > 0 && CheckForConfigStop(ingredients, recipe, totalSize))
                    break;
                if (ingredient.Item is null || ingredient.Amount < 1)
                    continue;

                // Ensure no number of ingredients can exceed the maximum config value.
                int max = FindMaxIngredientNum(ingredient.Item, totalSize);
                // If the maximum of allowable ingredients is less than 1, we hit a config limit and should stop.
                if (max <= 0)
                    break;

                int amount = Mathf.Min(ingredient.Amount, max);
                ingredients.Add(new LogicIngredient(ingredient.Item, amount));
                totalSize += GetItemSize(ingredient.Item.TechType) * amount;
                recipe.Value += ingredient.Item.Value * amount;
                _log.Debug($"> Adding ingredient: {ingredient.Item}, {amount}, size: {totalSize}");
                UpdateNumUsed(ingredient.Item);
            }
            
            // Update the total size of everything needed to build a basic outpost.
            _outpostSize += totalSize * _outpostPieces.GetOrDefault(recipe.TechType, 0);
            recipe.Recipe.Ingredients = ingredients.Select(i => new Ingredient(i.Item.TechType, i.Amount)).ToList();
            recipe.Recipe.CraftAmount = CraftDataHandler.GetRecipeData(recipe.TechType)?.craftAmount ?? 1;
            // Set the recipe's value as the sum total of the value of its ingredients.
            recipe.Value = totalValue;
            return recipe;
        }

        /// <summary>
        /// Yield ingredients one at a time. The base mode inspects each ingredient and ensures that config values are
        /// respected across all deriving modes. It may also mandate an early stop without exhausting this method.<br/>
        /// A lazy approach using an iterator is strongly recommended.
        /// </summary>
        /// <param name="rng">The RNG for this seed.</param>
        /// <param name="recipe">The recipe to randomise ingredients for.</param>
        /// <param name="ingredients">The existing ingredients of the recipe.</param>
        /// <param name="validIngredients">The potential ingredients to choose from.</param>
        /// <returns>The ingredients for the recipe.</returns>
        protected abstract IEnumerable<LogicIngredient> YieldRandomIngredients(IRandomHandler rng, LogicRecipe recipe,
            List<LogicIngredient> ingredients, List<LogicInventoryItem> validIngredients);

        /// <summary>
        /// Check whether conditions have been reached that mandate an early stop as defined by config values.
        /// </summary>
        /// <param name="ingredients">The current list of ingredients.</param>
        /// <param name="entity">The recipe to randomise ingredients for.</param>
        /// <param name="totalSize">The current size required by all previously chosen ingredients for the recipe.</param>
        /// <returns>True if the loop needs to stop, false if it can continue running.</returns>
        private bool CheckForConfigStop(List<LogicIngredient> ingredients, LogicRecipe entity, int totalSize)
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
            if (_outpostPieces.ContainsKey(entity.TechType)
                && _outpostSize > _config.MaxBasicOutpostSize.Value * 0.7)
            {
                _log.Debug("! Basic outpost size is getting too large, stopping.");
                return true;
            }

            return false;
        }

        /// <summary>
        /// Find the highest number of the given ingredient which the recipe can sustain given all config values.
        /// </summary>
        /// <param name="ingredient">The ingredient to consider.</param>
        /// <param name="totalSize">The total size of all ingredients added so far.</param>
        /// <returns>A positive integer.</returns>
        protected int FindMaxIngredientNum(LogicInventoryItem ingredient, int totalSize = 0)
        {
            if (totalSize >= _config.MaxInventorySizePerRecipe.Value)
                return 1;
            
            // Do not allow more ingredients than set in the config.
            int max = _config.MaxNumberPerIngredient.Value;
            // Account for how much space this new ingredient would take up.
            max = Math.Min(max, (_config.MaxInventorySizePerRecipe.Value - totalSize) / GetItemSize(ingredient.TechType));
            _log.Debug($"Calc max: {max}");
            
            // TODO: Replace with tagging system
            // Tools and upgrades do not stack, but if the recipe would require several and you have more than one in
            // inventory, it will consume all of them.
            // if (ingredient.Category.Equals(TechTypeCategory.Tools) 
            //     || ingredient.Category.Equals(TechTypeCategory.VehicleUpgrades) 
            //     || ingredient.Category.Equals(TechTypeCategory.WorkBenchUpgrades))
            //     max = Math.Min(max, 1);
            //
            // // Never require more than one (default) egg. That's tedious.
            // if (ingredient.Category.Equals(TechTypeCategory.Eggs))
            //     max = Math.Min(max, _config.MaxEggsAsSingleIngredient.Value);

            return max;
        }

        private int GetItemSize(TechType item)
        {
            var size = TechData.GetItemSize(item);
            return size.x * size.y;
        }

        private void UpdateNumUsed(LogicInventoryItem item)
        {
            // Only do this for items that actually need tracking.
            if (item.MaxRecipeUses < 0)
                return;

            item.TimesUsedInRecipes++;
            if (item.MaxRecipeUses - item.TimesUsedInRecipes <= 0)
            {
                // TODO: Remove from valid ingredients, remove parent recipe too.
            }
        }

        /// <summary>
        /// Get the TechType of the material to deconstruct scrap metal into.
        /// </summary>
        public abstract TechType GetScrapMetalReplacement();

        protected bool IsAllowedAsIngredient(LogicRecipe recipe, TechType ingredient)
        {
            // TODO: Check for tags of constructable, equipment, tools, upgrade.
            return true;
        }

        /// <summary>
        /// Remove all entities from the valid ingredients list which contain the given entity as an ingredient.
        /// </summary>
        private void RemoveParentRecipes(LogicEntity entity)
        {
            // TODO
        }

        /// <summary>
        /// Exists for convenience, and so that we don't have to look up the InventoryItem via the manager all the time.
        /// </summary>
        protected struct LogicIngredient
        {
            public LogicInventoryItem Item;
            public int Amount;

            public LogicIngredient(LogicInventoryItem item, int amount)
            {
                Item = item;
                Amount = amount;
            }
        }
    }
}
