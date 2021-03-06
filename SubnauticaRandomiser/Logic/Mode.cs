using System;
using System.Collections.Generic;
using SMLHelper.V2.Crafting;
using SMLHelper.V2.Handlers;
using SubnauticaRandomiser.RandomiserObjects;

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
        protected LogicEntity _baseTheme;

        protected Mode(RandomiserConfig config, Materials materials, ProgressionTree tree, Random random)
        {
            _config = config;
            _materials = materials;
            _tree = tree;
            _random = random;

            _baseTheme = ChooseBaseTheme(100);
            LogHandler.Debug("Chosen " + _baseTheme.TechType.AsString() + " as base theme.");
            //InitMod.s_masterDict.DictionaryInstance.Add(TechType.Titanium, _baseTheme.GetSerializableRecipe());
            //ChangeScrapMetalResult(_baseTheme);
        }

        internal abstract LogicEntity RandomiseIngredients(LogicEntity entity);

        // Add an ingredient to the list of ingredients used to form a recipe,
        // but ensure its MaxUses field is respected.
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
                LogHandler.Debug("!   Removing " + entity.TechType.AsString() + " from materials list due to max uses reached: " + entity._usedInRecipes);
            }
        }

        protected LogicEntity GetRandom(List<LogicEntity> list, List<ETechTypeCategory> blacklist = null)
        {
            if (list == null || list.Count == 0)
                throw new InvalidOperationException("Failed to get valid entity from materials list: list is null or empty.");

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

        // If base theming is enabled and this is a base piece, yield the base
        // theming ingredient.
        protected LogicEntity CheckForBaseTheming(LogicEntity entity)
        {
            if (_config.bDoBaseTheming && _baseTheme != null && entity.Category.Equals(ETechTypeCategory.BaseBasePieces))
                return _baseTheme;

            return null;
        }

        // If vanilla upgrade chains are enabled, yield that which this recipe
        // upgrades from (e.g. yields Knife when given HeatBlade)
        protected LogicEntity CheckForVanillaUpgrades(LogicEntity entity)
        {
            LogicEntity result = null;

            if (_config.bVanillaUpgradeChains)
            {
                TechType basicUpgrade = _tree.GetUpgradeChain(entity.TechType);
                if (!basicUpgrade.Equals(TechType.None))
                {
                    result = _materials.GetAll().Find(x => x.TechType.Equals(basicUpgrade));
                }
            }

            return result;
        }

        // Choose a theming ingredient for the base from among a range of easily
        // available options.
        private LogicEntity ChooseBaseTheme(int depth)
        {
            List<LogicEntity> options = new List<LogicEntity>();

            options.AddRange(_materials.GetAll().FindAll(x => x.Category.Equals(ETechTypeCategory.RawMaterials)
                                                     && x.AccessibleDepth < depth
                                                     && !x.HasPrerequisites
                                                     && x.MaxUsesPerGame == 0
                                                     && x.GetItemSize() == 1));

            if (_config.bUseFish)
            {
                options.AddRange(_materials.GetAll().FindAll(x => x.Category.Equals(ETechTypeCategory.Fish)
                                                         && x.AccessibleDepth < depth
                                                         && !x.HasPrerequisites
                                                         && x.MaxUsesPerGame == 0
                                                         && x.GetItemSize() == 1));
            }

            LogHandler.Debug("LIST OF BASE THEME OPTIONS:");
            foreach (LogicEntity ent in options)
            {
                LogHandler.Debug(ent.TechType.AsString());
            }
            LogHandler.Debug("END LIST");

            return GetRandom(options);
        }

        // This function changes the output of the metal salvage recipe by removing
        // the titanium one and replacing it with the new one.
        // As a minor caveat, the new recipe shows up at the bottom of the tree.
        internal static void ChangeScrapMetalResult(Recipe replacement)
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

            LogHandler.Debug("!!! TechType contained in replacement: " + replacement.TechType.AsString());
            foreach (RandomiserIngredient i in replacement.Ingredients)
            {
                LogHandler.Debug("!!! Ingredient: " + i.techType.AsString() + ", " + i.amount);
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
