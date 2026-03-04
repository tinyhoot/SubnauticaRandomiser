using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HarmonyLib;
using HootLib;
using HootLib.Objects;
using Nautilus.Crafting;
using Nautilus.Handlers;
using SubnauticaRandomiser.Configuration;
using SubnauticaRandomiser.Interfaces;
using SubnauticaRandomiser.Logic.LogicObjects;
using SubnauticaRandomiser.Objects;
using SubnauticaRandomiser.Objects.Enums;
using SubnauticaRandomiser.Objects.Exceptions;
using SubnauticaRandomiser.Patches;
using SubnauticaRandomiser.Serialization;
using SubnauticaRandomiser.Serialization.Modules;
using UnityEngine;
using ILogHandler = HootLib.Interfaces.ILogHandler;
using LogicEntity = SubnauticaRandomiser.Logic.LogicObjects.LogicEntity;

namespace SubnauticaRandomiser.Logic.Modules.Recipes
{
    /// <summary>
    /// Handles everything related to randomising recipes.
    /// </summary>
    internal class RecipeModule : BaseLogicModule
    {
        private Mode _mode;
        private NautilusShell<TechType, RecipeData> _recipeCache = new NautilusShell<TechType, RecipeData>(
            CraftDataHandler.SetRecipeData,
            CraftDataHandler.GetRecipeData);

        public override Type HandledEntityType => typeof(LogicRecipe);
        public override string LogPrefix => "[Recipe]";

        private const string SubFolderName = "RecipeModule";
        private const string OutpostFile = "outpostParts.json";
        private const string UpgradesFile = "upgrades.json";
        private Dictionary<TechType, int> _basicOutpostPieces = new Dictionary<TechType, int>();
        private Dictionary<TechType, TechType> _upgradeChains = new Dictionary<TechType, TechType>();
        private Dictionary<TechType, LogicInventoryItem> _upgradeItems = new Dictionary<TechType, LogicInventoryItem>();
        private List<LogicInventoryItem> _validIngredients = new List<LogicInventoryItem>();
        private LogicInventoryItem _baseTheme;
        private EntityManager _entityManager;
        
        internal override void OnRegisterModule(Config config, ILogHandler logger, LogicMonitor monitor)
        {
            base.OnRegisterModule(config, logger, monitor);
            _monitor.ContextCreated += OnContextCreated;
            _monitor.EntityRandomised += OnEntityRandomised;
        }

        public override IEnumerable<Task> LoadFilesAsync()
        {
            return new List<Task>
            {
                LoadOutpostFile(),
                LoadUpgradesFile()
            };
        }

        private async Task LoadOutpostFile()
        {
            var json = await SerdeUtils.ReadFileContents(SubFolderName, OutpostFile);
            _basicOutpostPieces = SerdeUtils.DeserializeObject<Dictionary<TechType, int>>(json);
            _log.Debug("Loaded outpost pieces:");
            foreach (var (piece, amt) in _basicOutpostPieces)
            {
                _log.Debug($"- {piece.AsString()}: {amt}");
            }
        }

        private async Task LoadUpgradesFile()
        {
            var json = await SerdeUtils.ReadFileContents(SubFolderName, UpgradesFile);
            _upgradeChains = SerdeUtils.DeserializeObject<Dictionary<TechType, TechType>>(json);
            _log.Debug("Loaded upgrade chains:");
            foreach (var (upgrade, prior) in _upgradeChains)
            {
                _log.Debug($"- {upgrade.AsString()} made from {prior.AsString()}");
            }
        }

        public override BaseModuleSaveData SetupSaveData()
        {
            return new RecipeSaveData();
        }

        public override void PrepareRandomisation(EntityManager manager)
        {
            _entityManager = manager;
            _validIngredients = new List<LogicInventoryItem>();
            
            switch (_config.RecipeMode.Value)
            {
                case RecipeDifficultyMode.Balanced:
                    _mode = new ModeBalanced(_config, manager, _basicOutpostPieces);
                    break;
                case RecipeDifficultyMode.Chaotic:
                    _mode = new ModeRandom(_config, manager, _basicOutpostPieces);
                    break;
                default:
                    throw new RandomisationException("Invalid recipe mode: " + _config.RecipeMode.Value);
            }

            _mode.RemoveValidIngredient += OnRemoveValidIngredient;
            
            CopyTags(manager);
            AddDependencies(manager);
            SetVanillaRecipeValues(manager);
            PrepareUpgradeChains(manager);
        }

        /// <summary>
        /// Take any tags from <see cref="LogicConstructable"/> and <see cref="LogicInventoryItem"/> and also put them
        /// onto the corresponding <see cref="LogicRecipe"/>.
        /// </summary>
        private void CopyTags(EntityManager manager)
        {
            foreach (var constructable in manager.GetAllEntities<LogicConstructable>())
            {
                var recipe = manager.Find<LogicRecipe>(constructable.TechType);
                if (recipe != null)
                {
                    recipe.Tags.UnionWith(constructable.Tags);
                    manager.RegisterTags(recipe);
                }
            }
            foreach (var iitem in manager.GetAllEntities<LogicInventoryItem>())
            {
                var recipe = manager.Find<LogicRecipe>(iitem.TechType);
                if (recipe != null)
                {
                    recipe.Tags.UnionWith(iitem.Tags);
                    manager.RegisterTags(recipe);
                }
            }
        }

        /// <summary>
        /// Add special dependencies specific to this module to some entities.
        /// </summary>
        private void AddDependencies(EntityManager manager)
        {
            // Add the builder tool to everything related to base pieces.
            var builder = manager.Find<LogicInventoryItem>(TechType.Builder);
            foreach (var entity in manager.GetAllWithTag(Tag.BasePiece))
            {
                _log.Debug($"Adding builder dependency to {entity}");
                entity.Dependencies.Add(builder);
            }
            
            // Require the Alien Containment for full access to any eggs.
            var acu = manager.Find<LogicConstructable>(TechType.BaseWaterPark);
            if (!_config.DiscoverEggs.Value)
            {
                foreach (var entity in manager.GetAllWithTag(Tag.Egg))
                {
                    entity.Dependencies.Add(acu);
                }
            }
            
            foreach (var entity in manager.GetAllWithTag(Tag.EggCreature))
            {
                entity.Dependencies.Add(acu);
            }
        }

        /// <summary>
        /// Set all recipes' target values based on their vanilla recipe.
        /// </summary>
        private void SetVanillaRecipeValues(EntityManager manager)
        {
            _log.Debug("Assigning recipe target values based on vanilla recipes.");
            List<LogicRecipe> recipes = manager.GetAllEntities<LogicRecipe>().ToList();
            int i = 0;
            int lastLoopCount = recipes.Count;
            while (recipes.Count > 0)
            {
                var recipe = recipes[i];
                bool ingredientsReady = true;
                int total = 0;
                
                // Check if all ingredients have been assigned a value. If not, this recipe takes ingredients which
                // themselves also have recipes (i.e. are craftables).
                var ingredients = TechData.GetIngredients(recipe.TechType);
                if (ingredients is null || ingredients.Count == 0)
                {
                    _log.Warn($"Recipe has no vanilla recipe, assigning fallback target value: {recipe.TechType}");
                    total = 100;
                }
                foreach (var ingredient in ingredients ?? Enumerable.Empty<Ingredient>())
                {
                    int value = manager.Find<LogicInventoryItem>(ingredient.techType).Value;
                    // If the IItem was not successful try to find a recipe we already assigned a target value to.
                    if (value <= 0)
                        value = manager.Find<LogicRecipe>(ingredient.techType)?.TargetValue ?? -1;
                    if (value <= 0)
                    {
                        ingredientsReady = false;
                        break;
                    }

                    total += value * ingredient.amount;
                }

                // If all ingredients had a value, update the recipe's target value.
                if (ingredientsReady)
                {
                    _log.Debug($"Assigning target value {total} to recipe {recipe.TechType}");
                    recipe.TargetValue = Mathf.FloorToInt(total * _config.RecipeValueMult.Value);
                    recipes.RemoveAt(i);
                    i--;
                }
                
                i++;
                if (i >= recipes.Count)
                {
                    // Ensure we don't get stuck infinitely if some recipes are completely isolated.
                    if (lastLoopCount == recipes.Count)
                        throw new RandomisationException("Failed to find ingredient values for all recipes! " +
                                                         $"Remaining: {recipes.ElementsToString()}");
                    i = 0;
                    lastLoopCount = recipes.Count;
                }
            }
        }

        private void PrepareUpgradeChains(EntityManager manager)
        {
            if (!_config.VanillaUpgradeChains.Value)
            {
                // Chains are disabled, clear whatever vanilla data we have on them.
                _upgradeChains.Clear();
                _upgradeItems.Clear();
                return;
            }
            
            foreach (var (upgrade, baseItem) in _upgradeChains)
            {
                var recipe = manager.Find<LogicRecipe>(upgrade);
                var item = manager.Find<LogicInventoryItem>(baseItem);
                if (recipe is null)
                {
                    _log.Warn($"Upgrade chain defined for {upgrade.AsString()} using base item {baseItem.AsString()} " +
                              $"where recipe is not defined in {nameof(LogicRecipe)}s!");
                    continue;
                }

                if (item is null)
                {
                    _log.Warn($"Upgrade chain defined for {upgrade.AsString()} using base item {baseItem.AsString()} " +
                              $"where base item is not defined in {nameof(LogicInventoryItem)}s!");
                    continue;
                }
                
                // Keep the base item ready for later.
                _upgradeItems.Add(upgrade, item);
                // Add the item to the recipe's dependencies so the item always gets randomised first.
                recipe.Dependencies.Add(item);
            }
        }

        public override void PreEntityRandomisation(IRandomHandler rng, SaveData saveData)
        {
            var save = saveData.GetModuleData<RecipeSaveData>();
            // Grab all eggs for patching later if auto-discovery is enabled.
            if (_config.DiscoverEggs.Value)
                save.EggsToAutoDiscover = _entityManager.GetAllWithTag(Tag.Egg).Select(e => e.TechType).ToList();
        }

        public override void RandomiseEntity(IRandomHandler rng, SaveData saveData, LogicEntity entity)
        {
            var recipe = (LogicRecipe)entity;
            _log.Debug($"Figuring out ingredients for {recipe}");
            var mandatory = GetMandatoryIngredients(recipe, _validIngredients.ShallowCopy());
            _mode.RandomiseIngredients(rng, recipe, mandatory, _validIngredients.ShallowCopy());
            saveData.GetModuleData<RecipeSaveData>().AddRecipe(recipe.Recipe.TechType, recipe.Recipe);
        }

        private List<LogicInventoryItem> GetMandatoryIngredients(LogicRecipe recipe, List<LogicInventoryItem> items)
        {
            List<LogicInventoryItem> mandatory = new List<LogicInventoryItem>();
            // Try to respect vanilla upgrade chains.
            if (_upgradeItems.TryGetValue(recipe.TechType, out var baseItem))
                mandatory.Add(baseItem);
            // Add base theming for base pieces.
            if (recipe.Tags.Contains(Tag.BasePiece) && _baseTheme != null)
                mandatory.Add(_baseTheme);
            
            return mandatory;
        }

        public override void PostEntityRandomisation(IRandomHandler rng, SaveData saveData)
        {
            saveData.GetModuleData<RecipeSaveData>().ScrapMetalResult = _mode.GetScrapMetalReplacement(rng, _validIngredients);
        }

        public override void RegisterHarmonyPatches(Harmony harmony, SaveData saveData)
        {
            harmony.PatchAll(typeof(EggPatcher));
        }

        public override void ApplySerializedState(SaveData saveData)
        {
            RecipeSaveData recipeSave = saveData.GetModuleData<RecipeSaveData>();
            if (recipeSave.RecipeDict is null || recipeSave.RecipeDict.Count == 0)
                return;
            
            foreach (TechType key in recipeSave.RecipeDict.Keys)
            {
                _recipeCache.SendChanges(key, recipeSave.RecipeDict[key].ToRecipeData());
            }
            
            ChangeScrapMetalResult(recipeSave.ScrapMetalResult);
        }

        public override void UndoSerializedState(SaveData saveData)
        {
            _recipeCache.UndoChanges();
            
            RecipeSaveData recipeSave = saveData.GetModuleData<RecipeSaveData>();
            if (recipeSave.RecipeDict is null || recipeSave.RecipeDict.Count == 0)
                return;
            ChangeScrapMetalResult(TechType.Titanium, recipeSave.ScrapMetalResult);
        }

        private void OnContextCreated(RandomisationContext context)
        {
            foreach (var entity in context.StartingEntities)
            {
                if (entity is LogicInventoryItem iitem)
                    _validIngredients.Add(iitem);
            }
        }

        /// <summary>
        /// When an entity enters the logic, add it as an ingredient if possible.
        /// </summary>
        private void OnEntityRandomised(LogicEntity entity)
        {
            // Only things you can hold in your inventory can be ingredients.
            if (!(entity is LogicInventoryItem item))
                return;
            
            // Choose a base theme as soon as the builder tool enters the logic.
            if (_config.BaseTheming.Value && item.TechType == TechType.Builder)
            {
                // Choose any decently low value item that does not have special restrictions on it.
                _baseTheme = _validIngredients.First(ii => ii.Value <= 50 && ii.MaxRecipeUses <= -1);
                _log.Debug($"Chose {_baseTheme} as base theme.");
            }
            
            // This is not a valid ingredient if it has been marked as forbidden/used up.
            if (item.MaxRecipeUses == 0 || item.MaxRecipeUses - item.TimesUsedInRecipes == 0)
                return;

            // Some items may be invalid based on tags, especially in combination with config settings.
            if (!TagsAllowedAsIngredient(item))
                return;
            
            _validIngredients.Add(item);
        }
        
        /// <summary>
        /// Check whether an item's tags disqualify it from being an ingredient.
        /// </summary>
        /// <returns>True if the ingredient is allowed to be used as such.</returns>
        private bool TagsAllowedAsIngredient(LogicInventoryItem item)
        {
            if ((item.Tags.Contains(Tag.Creature) || item.Tags.Contains(Tag.EggCreature)) && !_config.UseFish.Value)
                return false;
            if (item.Tags.Contains(Tag.Egg) && !_config.UseFish.Value)
                return false;
            if (item.Tags.Contains(Tag.Seed) && !_config.UseSeeds.Value)
                return false;
            
            // If this item *itself* has these tags, do not consider it.
            if ((item.Tags.Contains(Tag.Equipment) && _config.EquipmentAsIngredients.Value == IngredientInclusionLevel.Never)
                || (item.Tags.Contains(Tag.Tool) && _config.ToolsAsIngredients.Value == IngredientInclusionLevel.Never)
                || (item.Tags.Contains(Tag.Upgrade) && _config.UpgradesAsIngredients.Value == IngredientInclusionLevel.Never))
                return false;
            
            // If this item has a recipe and any of its *ingredients* has these tags, do not consider it.
            // This ensures some tags cannot show up as part of nested recipes.
            if (item.Recipe != null)
            {
                foreach (var ingredient in item.Recipe.Recipe.Ingredients)
                {
                    // It is technically possible for an ingredient to be in this recipe but not in the list - which is
                    // actually fine, we only care about it not being in any future recipes.
                    var entity = _validIngredients.Find(ii => ii.TechType == ingredient.techType);
                    if (entity is null)
                        continue;

                    if ((entity.Tags.Contains(Tag.Equipment) &&
                         _config.EquipmentAsIngredients.Value == IngredientInclusionLevel.TopLevelOnly)
                        || (entity.Tags.Contains(Tag.Tool) &&
                            _config.ToolsAsIngredients.Value == IngredientInclusionLevel.TopLevelOnly)
                        || (entity.Tags.Contains(Tag.Upgrade) &&
                            _config.UpgradesAsIngredients.Value == IngredientInclusionLevel.TopLevelOnly))
                        return false;
                }
            }
            
            return true;
        }

        /// <summary>
        /// React to a <see cref="Mode"/> signalling it wants to remove an ingredient from the pool of valid ingredients.
        /// </summary>
        private void OnRemoveValidIngredient(LogicInventoryItem ingredient)
        {
            _validIngredients.Remove(ingredient);
        }
        
        /// <summary>
        /// Changes what kind of material scrap metal can be turned into.
        /// </summary>
        /// <param name="techType">The new material to get from scrap metal.</param>
        /// <param name="oldResult">The old outcome of the recipe. Needed to properly delete and clean up.</param>
        /// <returns>The resulting Recipe, or null if the given TechType wasn't usable.</returns>
        private Recipe ChangeScrapMetalResult(TechType techType, TechType oldResult = TechType.Titanium)
        {
            if (techType.Equals(TechType.None))
                return null;
            
            // Create the new recipe.
            Recipe recipe = new Recipe(techType);
            recipe.Ingredients = new List<Ingredient>();
            recipe.Ingredients.Add(new Ingredient(TechType.ScrapMetal, 1));
            // Always use just as many items as can fit in four slots in the inventory.
            var itemDimensions = TechData.GetItemSize(techType);
            int size = itemDimensions.x * itemDimensions.y;
            recipe.CraftAmount = Math.Max(1, (int)Math.Floor(4f / size));
            CraftDataHandler.SetRecipeData(techType, recipe.ToRecipeData());

            // Delete the old recipe and remove it from the fabricator and PDA.
            // CraftDataHandler.SetRecipeData(oldResult, null);
            CraftTreeHandler.RemoveNode(CraftTree.Type.Fabricator, "Resources", "BasicMaterials", oldResult.AsString());
            CraftDataHandler.RemoveFromGroup(TechGroup.Resources, TechCategory.BasicMaterials, TechType.Titanium);
            KnownTechHandler.RemoveDefaultUnlock(oldResult);
            
            // Add the replacement recipe in its stead.
            CraftTreeHandler.AddCraftingNode(CraftTree.Type.Fabricator, techType, "Resources", "BasicMaterials");
            CraftDataHandler.AddToGroup(TechGroup.Resources, TechCategory.BasicMaterials, techType);
            // Ensure access at game start.
            KnownTechHandler.UnlockOnStart(techType);

            return recipe;
        }
    }
}