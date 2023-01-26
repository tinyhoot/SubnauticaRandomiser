using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using SMLHelper.V2.Crafting;
using SMLHelper.V2.Handlers;
using SubnauticaRandomiser.Handlers;
using SubnauticaRandomiser.Interfaces;
using SubnauticaRandomiser.Objects;
using SubnauticaRandomiser.Objects.Enums;

namespace SubnauticaRandomiser.Logic.Recipes
{
    /// <summary>
    /// The base class for deciding how or which ingredients are chosen in recipe randomisation.
    /// </summary>
    internal abstract class Mode
    {
        protected readonly CoreLogic _coreLogic;
        protected readonly RecipeLogic _recipeLogic;
        protected RandomiserConfig _config => _coreLogic._Config;
        protected EntityHandler _entityHandler => _coreLogic.EntityHandler;
        protected IRandomHandler _random => _coreLogic.Random;
        protected ILogHandler _log => _coreLogic._Log;
        
        protected List<RandomiserIngredient> _ingredients = new List<RandomiserIngredient>();
        protected List<TechTypeCategory> _blacklist = new List<TechTypeCategory>();
        protected BaseTheme _baseTheme;

        protected Mode(CoreLogic coreLogic, RecipeLogic recipeLogic)
        {
            _coreLogic = coreLogic;
            _recipeLogic = recipeLogic;

            if (_config.bDoBaseTheming)
                _baseTheme = new BaseTheme(_entityHandler, _log, _random);

            //InitMod.s_masterDict.DictionaryInstance.Add(TechType.Titanium, _baseTheme.GetSerializableRecipe());
            //ChangeScrapMetalResult(_baseTheme);
        }

        /// <summary>
        /// Choose a base theme once off-loop randomisation begins.
        /// </summary>
        public void ChooseBaseTheme(int depth, bool useFish)
        {
            _baseTheme?.ChooseBaseTheme(depth, useFish);
        }

        public abstract LogicEntity RandomiseIngredients(LogicEntity entity);
        
        /// <summary>
        /// Add an ingredient to the list of ingredients used to form a recipe, but ensure its MaxUses field is
        /// respected.
        /// </summary>
        /// <param name="entity">The entity to add.</param>
        /// <param name="amount">The number of uses to consume.</param>
        protected void AddIngredientWithMaxUsesCheck(LogicEntity entity, int amount)
        {
            // Ensure that limited ingredients are not overused. Particularly
            // intended for cuddlefish.
            int remainder = entity.MaxUsesPerGame - entity.UsedInRecipes;
            if (entity.MaxUsesPerGame != 0 && remainder > 0 && remainder < amount)
                amount = remainder;

            _ingredients.Add(new RandomiserIngredient(entity.TechType, amount));
            entity.UsedInRecipes++;

            if (!entity.HasUsesLeft())
            {
                _recipeLogic.ValidIngredients.Remove(entity);
                _log.Debug($"[R] ! Removing {entity} ingredients list due to " + 
                           $"max uses reached: {entity.UsedInRecipes}");
                RemoveParentRecipes(entity);
            }
        }

        /// <summary>
        /// Get a random entity from a list, ensuring that it is not part of a given blacklist.
        /// TODO: Install safeguards to prevent infinite loops.
        /// </summary>
        /// <param name="list">The list to get a random element from.</param>
        /// <param name="blacklist">The blacklist of forbidden elements to not ever consider.</param>
        /// <returns>A random, non-blacklisted element from the list.</returns>
        /// <exception cref="InvalidOperationException">Raised if the list is null or empty.</exception>
        [NotNull]
        protected LogicEntity GetRandom(ICollection<LogicEntity> list, List<TechTypeCategory> blacklist = null)
        {
            if (list == null || list.Count == 0)
                throw new InvalidOperationException("Failed to get valid entity from materials list: "
                                                    + "list is null or empty.");

            LogicEntity randomEntity = null;
            while (true)
            {
                randomEntity = _random.Choice(list);

                if (blacklist?.Count > 0)
                {
                    if (blacklist.Contains(randomEntity.Category))
                        continue;
                }
                break;
            }

            return randomEntity;
        }

        /// <summary>
        /// Remove all entities from the valid ingredients list which contain the given entity as an ingredient.
        /// </summary>
        private void RemoveParentRecipes(LogicEntity entity)
        {
            int count = _recipeLogic.ValidIngredients.RemoveWhere(e =>
                e.Recipe?.Ingredients.Any(i => i.techType.Equals(entity.TechType)) ?? false);
            _log.Debug($"[R]   Also removing {count} parent recipes.");
        }

        // This function changes the output of the metal salvage recipe by removing
        // the titanium one and replacing it with the new one.
        // As a minor caveat, the new recipe shows up at the bottom of the tree.
        // FIXME does not function.
        internal void ChangeScrapMetalResult(Recipe replacement)
        {
            if (replacement.TechType.Equals(TechType.Titanium))
                return;

            // This techdata was used as a futile and desparate attempt to get things
            // working. It acts just like a RandomiserRecipe would though.
            TechData td = new TechData();
            td.Ingredients = new List<Ingredient>();
            td.Ingredients.Add(new Ingredient(TechType.ScrapMetal, 1));
            td.craftAmount = 1;
            TechType yeet = TechType.GasPod;

            replacement.Ingredients = new List<RandomiserIngredient>();
            replacement.Ingredients.Add(new RandomiserIngredient(TechType.ScrapMetal, 1));
            replacement.CraftAmount = 4;

            //CraftDataHandler.SetTechData(replacement.TechType, replacement);
            CraftDataHandler.SetTechData(yeet, td);

            _log.Debug("!!! TechType contained in replacement: " + replacement.TechType.AsString());
            foreach (RandomiserIngredient i in replacement.Ingredients)
            {
                _log.Debug("!!! Ingredient: " + i.techType.AsString() + ", " + i.amount);
            }

            // FIXME for whatever reason, this code works for some items, but not for others????
            // Fish seem to work, and so does lead, but every other raw material does not?
            // What's worse, CC2 has no issues with this at all despite apparently doing nothing different???
            CraftTreeHandler.RemoveNode(CraftTree.Type.Fabricator, "Resources", "BasicMaterials", "Titanium");

            //CraftTreeHandler.AddCraftingNode(CraftTree.Type.Fabricator, replacement.TechType, "Resources", "BasicMaterials");
            CraftTreeHandler.AddCraftingNode(CraftTree.Type.Fabricator, yeet, "Resources", "BasicMaterials");

            CraftDataHandler.RemoveFromGroup(TechGroup.Resources, TechCategory.BasicMaterials, TechType.Titanium);
            CraftDataHandler.AddToGroup(TechGroup.Resources, TechCategory.BasicMaterials, yeet);
        }

        /// <summary>
        /// Set up the blacklist with entities that are not allowed to function as ingredients for the given entity.
        /// </summary>
        /// <param name="entity">The entity to build a blacklist against.</param>
        protected void UpdateBlacklist(LogicEntity entity)
        {
            _blacklist = new List<TechTypeCategory>();

            if (_config.iEquipmentAsIngredients == 0 || (_config.iEquipmentAsIngredients == 1 && entity.CanFunctionAsIngredient()))
                _blacklist.Add(TechTypeCategory.Equipment);
            if (_config.iToolsAsIngredients == 0 || (_config.iToolsAsIngredients == 1 && entity.CanFunctionAsIngredient()))
                _blacklist.Add(TechTypeCategory.Tools);
            if (_config.iUpgradesAsIngredients == 0 || (_config.iUpgradesAsIngredients == 1 && entity.CanFunctionAsIngredient()))
            {
                _blacklist.Add(TechTypeCategory.ScannerRoom);
                _blacklist.Add(TechTypeCategory.VehicleUpgrades);
                _blacklist.Add(TechTypeCategory.WorkBenchUpgrades);
            }
        }
    }
}
