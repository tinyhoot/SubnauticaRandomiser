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
        private List<LogicInventoryItem> _validIngredients = new List<LogicInventoryItem>();
        
        internal override void OnRegisterModule(Config config, ILogHandler logger, LogicMonitor monitor)
        {
            base.OnRegisterModule(config, logger, monitor);
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
            
            // TODO: Config related setup
            // - Upgrade chains
            // - Egg getting waterpark as dependency
            // - Assign recipe/ingredient values based on vanilla recipes?
            
            SetVanillaRecipeValues(manager);
            PrepareUpgradeChains(manager);
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
                // Add the item to the recipe's dependencies so the item always gets randomised first.
                recipe.Dependencies.Add(item);
            }
        }

        public override void PreEntityRandomisation(IRandomHandler rng, SaveData saveData)
        {
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
            if (_upgradeChains.TryGetValue(recipe.TechType, out var baseItem))
                mandatory.Add(items.Find(i => i.TechType == baseItem));
            
            return mandatory;
        }

        public override void PostEntityRandomisation(IRandomHandler rng, SaveData saveData)
        {
            saveData.GetModuleData<RecipeSaveData>().ScrapMetalResult = _mode.GetScrapMetalReplacement();
            // TODO: Modify recipes to fit with config options.
            // - Reduce recipe sizes belonging to outpost if necessary, starting with the recipes which take up the most
            //   space to craft.
            // - Replace one ingredient with upgrade chain bases
            // - Choose base theme
            // - Replace one ingredient with base theme
        }

        public override void RegisterHarmonyPatches(Harmony harmony, SaveData saveData)
        {
            // No patches necessary, it's all going through Nautilus.
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

        /// <summary>
        /// When an entity enters the logic, add it as an ingredient if possible.
        /// </summary>
        private void OnEntityRandomised(LogicEntity entity)
        {
            // Only things you can hold in your inventory can be ingredients.
            if (!(entity is LogicInventoryItem item))
                return;

            if (item.MaxRecipeUses != 0)
                _validIngredients.Add(item);
        }

        // private void OnSetupBeginning(object sender, EventArgs args)
        // {
        //     _entityHandler.UpdateEntityValues(_config.RecipeValueMult.Value);
        //     
        //     // Decide which recipe mode will be used.
        //     switch (_config.RecipeMode.Value)
        //     {
        //         case (RecipeDifficultyMode.Balanced):
        //             _mode = new ModeBalanced(_coreLogic, this, _coreLogic.GetRNG());
        //             break;
        //         case (RecipeDifficultyMode.Chaotic):
        //             _mode = new ModeRandom(_coreLogic, this, _coreLogic.GetRNG());
        //             break;
        //         default:
        //             _log.Error("Invalid recipe mode: " + _config.RecipeMode.Value);
        //             break;
        //     }
        //     
        //     // Assemble a dictionary of what is considered basic outpost pieces which together should not exceed
        //     // the total cost defined in the config.
        //     BasicOutpostPieces = new Dictionary<TechType, int>
        //     {
        //         { TechType.BaseCorridorI, 1 },
        //         { TechType.BaseHatch, 1 },
        //         { TechType.BaseMapRoom, 1 },
        //         { TechType.BaseWindow, 1 },
        //         { TechType.Beacon, 1 },
        //         { TechType.SolarPanel, 2 },
        //     };
        //
        //     // Define the direct recipe chains that are present in vanilla.
        //     if (_config.VanillaUpgradeChains.Value)
        //     {
        //         UpgradeChains = new Dictionary<TechType, TechType>
        //         {
        //             { TechType.VehicleHullModule2, TechType.VehicleHullModule1 },
        //             { TechType.VehicleHullModule3, TechType.VehicleHullModule2 },
        //             { TechType.ExoHullModule2, TechType.ExoHullModule1 },
        //             { TechType.CyclopsHullModule2, TechType.CyclopsHullModule1 },
        //             { TechType.CyclopsHullModule3, TechType.CyclopsHullModule2 },
        //             { TechType.HeatBlade, TechType.Knife },
        //             { TechType.RepulsionCannon, TechType.PropulsionCannon },
        //             { TechType.SwimChargeFins, TechType.Fins },
        //             { TechType.UltraGlideFins, TechType.Fins },
        //             { TechType.DoubleTank, TechType.Tank },
        //             { TechType.PlasteelTank, TechType.DoubleTank },
        //             { TechType.HighCapacityTank, TechType.DoubleTank },
        //         };
        //         ApplyUpgradeChainPrerequisites(_entityHandler, UpgradeChains);
        //     }
        //     else
        //     {
        //         UpgradeChains = new Dictionary<TechType, TechType>();
        //     }
        //     AddEggWaterParkPrerequisite(Bootstrap.SaveData.GetModuleData<RecipeSaveData>());
        //     
        //     // Add basic raw materials into the logic.
        //     UpdateValidIngredients(0);
        // }
        //
        // /// <summary>
        // /// Ensure that certain recipes are always randomised by a certain depth.
        // /// </summary>
        // private void OnSetupPriorityEntities(object sender, SetupPriorityEventArgs args)
        // {
        //     // Ensure this setup is only done when the event is called from the manager itself.
        //     if (!(sender is ProgressionManager manager))
        //         return;
        //     
        //     manager.AddEssentialEntities(0, new []
        //     {
        //         TechType.Scanner,
        //         TechType.Welder,
        //         TechType.SmallStorage,
        //         TechType.BaseHatch,
        //         TechType.Fabricator,
        //     });
        //     manager.AddEssentialEntities(100, new []
        //     {
        //         TechType.Builder,
        //         TechType.BaseRoom,
        //         TechType.Seaglide,
        //         TechType.Tank,
        //     });
        //     manager.AddEssentialEntities(300, new []
        //     {
        //         TechType.BaseWaterPark,
        //     });
        //     
        //     manager.AddElectiveEntities(100, new []
        //     {
        //         new [] { TechType.Battery, TechType.BatteryCharger },
        //     });
        //     manager.AddElectiveEntities(200, new []
        //     {
        //         new [] { TechType.BaseBioReactor, TechType.SolarPanel },
        //         new [] { TechType.PowerCell, TechType.PowerCellCharger, TechType.SeamothSolarCharge },
        //         new [] { TechType.BaseBulkhead, TechType.BaseFoundation, TechType.BaseReinforcement },
        //     });
        // }
        //
        // /// <summary>
        // /// Ensure that knife and waterpark are considered progression items since they unlock seeds and eggs.
        // /// </summary>
        // private void OnSetupProgressionEntitites(object sender, SetupProgressionEventArgs args)
        // {
        //     // Ensure this setup is only done when the event is called from the manager itself.
        //     if (!(sender is ProgressionManager))
        //         return;
        //     
        //     args.ProgressionEntities.Add(TechType.BaseWaterPark);
        //     args.ProgressionEntities.Add(TechType.HeatBlade);
        //     args.ProgressionEntities.Add(TechType.Knife);
        //     args.ProgressionEntities.Add(TechType.RadiationSuit);
        // }
        //
        // /// <summary>
        // /// Add the Alien Containment Unit as a prerequisite to all eggs.
        // /// </summary>
        // private void AddEggWaterParkPrerequisite(RecipeSaveData saveData)
        // {
        //     saveData.DiscoverEggs = _config.DiscoverEggs.Value;
        //     if (!_config.DiscoverEggs.Value)
        //         _entityHandler.AddCategoryPrerequisite(TechTypeCategory.Eggs, TechType.BaseWaterPark);
        //     // Always add this requirement to fish hatched in containment.
        //     _entityHandler.AddCategoryPrerequisite(TechTypeCategory.EggsHatched, TechType.BaseWaterPark);
        // }
        //
        // /// <summary>
        // /// Add early elements of an upgrade chain as prerequisites of the later pieces to ensure that they are always
        // /// randomised in order, and no Knife can require a Heatblade as ingredient.
        // /// </summary>
        // private void ApplyUpgradeChainPrerequisites(EntityHandler entityHandler, Dictionary<TechType, TechType> upgradeChains)
        // {
        //     if (entityHandler is null || upgradeChains is null || upgradeChains.Count == 0)
        //         return;
        //     
        //     foreach (TechType upgrade in upgradeChains.Keys)
        //     {
        //         TechType ingredient = upgradeChains[upgrade];
        //         LogicEntity entity = entityHandler.GetEntity(upgrade);
        //         if (entity is null)
        //             continue;
        //         if (!entity.HasPrerequisites)
        //             entity.Prerequisites = new List<TechType>();
        //         entity.Prerequisites.Add(ingredient);
        //     }
        // }
        
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
        
        // /// <summary>
        // /// Get the ingredient required for a given upgrade, if any. E.g. Seamoth Depth MK2 will return MK1.
        // /// </summary>
        // /// <param name="upgrade">The "Tier 2" entity to investigate for ingredients.</param>
        // /// <returns>The TechType of the required "Tier 1" ingredient, or TechType.None if no such requirement exists.
        // /// </returns>
        // public TechType GetBaseOfUpgrade(TechType upgrade)
        // {
        //     if (UpgradeChains.TryGetValue(upgrade, out TechType type))  
        //         return type;
        //
        //     return TechType.None;
        // }
        //
        // /// <summary>
        // /// If vanilla upgrade chains are enabled, return that which this recipe upgrades from.
        // /// <example>Returns the basic Knife when given HeatBlade.</example>
        // /// </summary>
        // /// <param name="upgrade">The upgrade to check for a base.</param>
        // /// <param name="entityHandler">The list of all materials.</param>
        // /// <returns>A LogicEntity if the given upgrade has a base it upgrades from, null otherwise.</returns>
        // [CanBeNull]
        // public LogicEntity GetBaseOfUpgrade(TechType upgrade, EntityHandler entityHandler)
        // {
        //     TechType basicEntity = GetBaseOfUpgrade(upgrade);
        //     if (basicEntity.Equals(TechType.None))
        //         return null;
        //
        //     return entityHandler.GetEntity(basicEntity);
        // }
        //
        // /// <summary>
        // /// Check whether any type of knife has been randomised and made accessible.
        // /// </summary>
        // private bool IsAnyKnifeRandomised()
        // {
        //     return _coreLogic.HasRandomised(TechType.HeatBlade) || _coreLogic.HasRandomised(TechType.Knife);
        // }
        //
        // /// <summary>
        // /// Add non-craftable ingredients into the logic up to the given depth.
        // /// This triggers an event, which this class listens to for registering new ingredients.
        // /// </summary>
        // private void UpdateValidIngredients(int depth)
        // {
        //     if (IsAnyKnifeRandomised())
        //         _entityHandler.AddToLogic(TechTypeCategory.RawMaterials, depth);
        //     else
        //         _entityHandler.AddToLogic(TechTypeCategory.RawMaterials, depth, TechType.Knife, true);
        //
        //     if (_config.UseFish.Value)
        //         _entityHandler.AddToLogic(TechTypeCategory.Fish, depth);
        //     if (_config.UseSeeds.Value && IsAnyKnifeRandomised())
        //         _entityHandler.AddToLogic(TechTypeCategory.Seeds, depth);
        // }
    }
}