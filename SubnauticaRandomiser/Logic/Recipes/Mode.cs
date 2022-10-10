using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using SMLHelper.V2.Crafting;
using SMLHelper.V2.Handlers;
using SubnauticaRandomiser.Interfaces;
using SubnauticaRandomiser.RandomiserObjects;
using SubnauticaRandomiser.RandomiserObjects.Enums;

namespace SubnauticaRandomiser.Logic.Recipes
{
    internal abstract class Mode
    {
        protected readonly CoreLogic _logic;
        protected RandomiserConfig _config => _logic._config;
        protected Materials _materials => _logic._materials;
        protected ProgressionTree _tree => _logic._tree;
        protected Random _random => _logic._random;
        protected ILogHandler _log => _logic._log;
        
        protected List<RandomiserIngredient> _ingredients = new List<RandomiserIngredient>();
        protected List<ETechTypeCategory> _blacklist = new List<ETechTypeCategory>();
        protected BaseTheme _baseTheme;

        protected Mode(CoreLogic logic)
        {
            _logic = logic;

            if (_config.bDoBaseTheming)
            {
                _baseTheme = new BaseTheme(_materials, _log, _random);
                _baseTheme.ChooseBaseTheme(100, _config.bUseFish);
            }
            
            //InitMod.s_masterDict.DictionaryInstance.Add(TechType.Titanium, _baseTheme.GetSerializableRecipe());
            //ChangeScrapMetalResult(_baseTheme);
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
            int remainder = entity.MaxUsesPerGame - entity._usedInRecipes;
            if (entity.MaxUsesPerGame != 0 && remainder > 0 && remainder < amount)
                amount = remainder;

            _ingredients.Add(new RandomiserIngredient(entity.TechType, amount));
            entity._usedInRecipes++;

            if (!entity.HasUsesLeft())
            {
                _materials.GetReachable().Remove(entity);
                _log.Debug($"[R] ! Removing {entity} from materials list due to " + 
                           $"max uses reached: {entity._usedInRecipes}");
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
        protected LogicEntity GetRandom(List<LogicEntity> list, List<ETechTypeCategory> blacklist = null)
        {
            if (list == null || list.Count == 0)
                throw new InvalidOperationException("Failed to get valid entity from materials list: "
                                                    + "list is null or empty.");

            LogicEntity randomEntity = null;
            while (true)
            {
                randomEntity = list[_random.Next(0, list.Count)];

                if (blacklist != null && blacklist.Count > 0)
                {
                    if (blacklist.Contains(randomEntity.Category))
                        continue;
                }
                break;
            }

            return randomEntity;
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
            _blacklist = new List<ETechTypeCategory>();

            if (_config.iEquipmentAsIngredients == 0 || (_config.iEquipmentAsIngredients == 1 && entity.CanFunctionAsIngredient()))
                _blacklist.Add(ETechTypeCategory.Equipment);
            if (_config.iToolsAsIngredients == 0 || (_config.iToolsAsIngredients == 1 && entity.CanFunctionAsIngredient()))
                _blacklist.Add(ETechTypeCategory.Tools);
            if (_config.iUpgradesAsIngredients == 0 || (_config.iUpgradesAsIngredients == 1 && entity.CanFunctionAsIngredient()))
            {
                _blacklist.Add(ETechTypeCategory.VehicleUpgrades);
                _blacklist.Add(ETechTypeCategory.WorkBenchUpgrades);
            }
        }
    }
}
