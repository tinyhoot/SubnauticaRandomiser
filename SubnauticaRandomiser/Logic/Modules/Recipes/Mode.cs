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
        /// <param name="mandatory">The ingredients that <em>must</em> be included.</param>
        /// <param name="validIngredients">All valid ingredients that can be chosen for the recipe.</param>
        /// <returns>The same modified entity.</returns>
        public LogicRecipe RandomiseIngredients(IRandomHandler rng, LogicRecipe recipe,
            List<LogicInventoryItem> mandatory, List<LogicInventoryItem> validIngredients)
        {
            recipe.Recipe.Ingredients = new List<Ingredient>();
            recipe.AssignedValue = 0;
            int totalSize = 0;

            // Get ingredients from the subclass one at a time.
            foreach (var ingredient in YieldRandomIngredients(rng, recipe, validIngredients))
            {
                _log.Debug($"Proposing next ingredient {ingredient}");
                LogicInventoryItem item = ingredient;
                // During the first loop(s), prioritise mandatory ingredients.
                if (mandatory?.Count > 0)
                {
                    item = mandatory[0];
                    mandatory.RemoveAt(0);
                    _log.Debug($"Prioritising mandatory ingredient {item}");
                }
                
                if (recipe.Recipe.Ingredients.Count > 0 && CheckForConfigStop(recipe))
                    break;
                // Something may go wrong in the subclass, so just to be sure.
                if (item is null)
                    break;

                int amount = GetIngredientAmt(rng, recipe, item);
                // If the amount of this ingredient is less than 1, we hit a config limit and should stop.
                if (amount <= 0)
                    break;
                
                recipe.Recipe.Ingredients.Add(new Ingredient(item.TechType, amount));
                totalSize += GetItemSize(item.TechType) * amount;
                recipe.AssignedValue += item.Value * amount;
                _log.Debug($"> Adding ingredient: {item}, {amount}, size: {totalSize}, value: {recipe.AssignedValue}");
                UpdateNumUsed(item);
            }
            
            // Update the total size of everything needed to build a basic outpost.
            _outpostSize += totalSize * _outpostPieces.GetOrDefault(recipe.TechType, 0);
            // Keep the number of items crafted per click consistent with the vanilla game.
            recipe.Recipe.CraftAmount = CraftDataHandler.GetRecipeData(recipe.TechType)?.craftAmount ?? 1;
            // If the recipe is for an item that could itself end up as an ingredient, update its value.
            var recipeInvItem = _entityManager.Find<LogicInventoryItem>(recipe.TechType);
            if (recipeInvItem != null)
                recipeInvItem.Value = recipe.AssignedValue;
            
            return recipe;
        }

        /// <summary>
        /// Yield ingredients one at a time. The base mode inspects each ingredient and ensures that config values are
        /// respected across all deriving modes. It may also mandate an early stop without exhausting this method.<br/>
        /// A lazy approach using an iterator is strongly recommended.
        /// </summary>
        /// <param name="rng">The RNG for this seed.</param>
        /// <param name="recipe">The recipe to randomise ingredients for.</param>
        /// <param name="validIngredients">The potential ingredients to choose from.</param>
        /// <returns>The ingredients for the recipe.</returns>
        protected abstract IEnumerable<LogicInventoryItem> YieldRandomIngredients(IRandomHandler rng, LogicRecipe recipe,
            List<LogicInventoryItem> validIngredients);

        /// <summary>
        /// Get the definitive amount of an ingredient based on Mode parameters and config values.
        /// </summary>
        private int GetIngredientAmt(IRandomHandler rng, LogicRecipe recipe, LogicInventoryItem ingredient)
        {
            int desired = GetRandomIngredientAmt(rng, recipe, ingredient);
            int max = GetMaxAllowed(recipe, ingredient);
            _log.Debug($"Desired: {desired}, Max: {max}");
            return Mathf.Min(desired, max);
        }

        /// <summary>
        /// For a new ingredient about to be added to the recipe, how many should be required?
        /// </summary>
        protected abstract int GetRandomIngredientAmt(IRandomHandler rng, LogicRecipe recipe,
            LogicInventoryItem ingredient);

        /// <summary>
        /// Check whether conditions have been reached that mandate an early stop as defined by config values.
        /// </summary>
        /// <param name="recipe">The recipe to randomise ingredients for.</param>
        /// <returns>True if the loop needs to stop, false if it can continue running.</returns>
        private bool CheckForConfigStop(LogicRecipe recipe)
        {
            // Respect the maximum number of ingredients set in the config.
            if (recipe.Recipe.Ingredients.Count >= _config.MaxIngredientsPerRecipe.Value)
            {
                _log.Debug("! Recipe has reached maximum allowed number of ingredients, stopping.");
                return true;
            }
            
            // If a recipe starts requiring too much space, shut it down early.
            if (GetRecipeSize(recipe) >= _config.MaxInventorySizePerRecipe.Value)
            {
                _log.Debug("! Recipe is getting too large, stopping.");
                return true;
            }
            
            // For special case of outpost base parts, be conservative with ingredients.
            if (_outpostPieces.ContainsKey(recipe.TechType)
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
        /// <returns>A positive integer.</returns>
        private int GetMaxAllowed(LogicRecipe recipe, LogicInventoryItem ingredient)
        {
            var totalSize = GetRecipeSize(recipe);
            if (totalSize >= _config.MaxInventorySizePerRecipe.Value)
                return 1;
            
            // Do not allow more ingredients than set in the config.
            int max = _config.MaxNumberPerIngredient.Value;
            // Account for how much space this new ingredient would take up.
            max = Math.Min(max, (_config.MaxInventorySizePerRecipe.Value - totalSize) / GetItemSize(ingredient.TechType));
            
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

        private int GetRecipeSize(LogicRecipe recipe)
        {
            return recipe.Recipe.Ingredients.Sum(i => GetItemSize(i.techType));
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
