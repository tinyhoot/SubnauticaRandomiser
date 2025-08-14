using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HarmonyLib;
using HootLib.Objects;
using JetBrains.Annotations;
using Nautilus.Crafting;
using Nautilus.Handlers;
using SubnauticaRandomiser.Configuration;
using SubnauticaRandomiser.Handlers;
using SubnauticaRandomiser.Interfaces;
using SubnauticaRandomiser.Objects;
using SubnauticaRandomiser.Objects.Enums;
using SubnauticaRandomiser.Objects.Events;
using SubnauticaRandomiser.Serialization;
using SubnauticaRandomiser.Serialization.Modules;
using UnityEngine;
using ILogHandler = HootLib.Interfaces.ILogHandler;

namespace SubnauticaRandomiser.Logic.Modules.Recipes
{
    /// <summary>
    /// Handles everything related to randomising recipes.
    /// </summary>
    [RequireComponent(typeof(CoreLogic), typeof(ProgressionManager))]
    internal class RecipeLogic : MonoBehaviour, ILogicModule
    {
        private CoreLogic _coreLogic;
        private ProgressionManager _manager;

        private Config _config;
        private ILogHandler _log;
        private EntityHandler _entityHandler;
        private Mode _mode;
        private NautilusShell<TechType, RecipeData> _recipeCache;

        public Dictionary<TechType, int> BasicOutpostPieces { get; private set; }
        public Dictionary<TechType, TechType> UpgradeChains { get; private set; }
        public HashSet<LogicEntity> ValidIngredients { get; private set; }
        
        private void Awake()
        {
            _coreLogic = GetComponent<CoreLogic>();
            _manager = GetComponent<ProgressionManager>();
            _config = _coreLogic._Config;
            _entityHandler = _coreLogic.EntityHandler;
            _log = PrefixLogHandler.Get("[R]");
            _recipeCache = new NautilusShell<TechType, RecipeData>(
                CraftDataHandler.SetRecipeData,
                CraftDataHandler.GetRecipeData);
            ValidIngredients = new HashSet<LogicEntity>(new LogicEntityEqualityComparer());
            
            // Register events.
            _coreLogic.EntityCollecting += OnEntityCollecting;
            _coreLogic.SetupBeginning += OnSetupBeginning;
            _entityHandler.EntityEnteredLogic += OnEntityEnteredLogic;
            _manager.HasProgressed += OnProgression;
            _manager.SetupPriority += OnSetupPriorityEntities;
            _manager.SetupProgression += OnSetupProgressionEntitites;
            // Register this module as handler for recipe type entities.
            _coreLogic.RegisterEntityHandler(EntityType.Craftable, this);
        }
        
        public IEnumerable<Task> LoadFiles()
        {
            return Enumerable.Empty<Task>();
        }

        public BaseModuleSaveData SetupSaveData()
        {
            return new RecipeSaveData();
        }
        
        /// <summary>
        /// Apply all recipe changes stored in the serializer to the game.
        /// </summary>
        public void ApplySerializedChanges(SaveData saveData)
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

        public void UndoSerializedChanges(SaveData saveData)
        {
            _recipeCache.UndoChanges();
            
            RecipeSaveData recipeSave = saveData.GetModuleData<RecipeSaveData>();
            if (recipeSave.RecipeDict is null || recipeSave.RecipeDict.Count == 0)
                return;
            ChangeScrapMetalResult(TechType.Titanium, recipeSave.ScrapMetalResult);
        }

        public void RandomiseOutOfLoop(IRandomHandler rng, SaveData saveData)
        {
            _mode.ChooseBaseTheme(100, _config.UseFish.Value);
            saveData.GetModuleData<RecipeSaveData>().ScrapMetalResult = _mode.GetScrapMetalReplacement();
        }

        /// <summary>
        /// Randomise a recipe entity.
        /// </summary>
        /// <returns>True if successful, false if something went wrong.</returns>
        public bool RandomiseEntity(IRandomHandler rng, ref LogicEntity entity)
        {
            // Does this recipe have all of its prerequisites fulfilled? Skip this check if the recipe is a priority.
            if (!(entity.IsPriority
                  || (entity.CheckBlueprintFulfilled(_coreLogic, _manager.ReachableDepth) && entity.CheckPrerequisitesFulfilled(_coreLogic))))
            {
                _log.Debug($"--- Recipe [{entity}] did not fulfill requirements, skipping.");
                return false;
            }
            
            _log.Debug("Figuring out ingredients for " + entity);
            _mode.RandomiseIngredients(ref entity);
            Bootstrap.SaveData.GetModuleData<RecipeSaveData>().AddRecipe(entity.Recipe.TechType, entity.Recipe);
            _log.Debug($"[+] Randomised recipe for [{entity}].");

            return true;
        }
        
        // All recipe related changes can be sent via Nautilus, no harmony patches are necessary.
        public void SetupHarmonyPatches(Harmony harmony, SaveData saveData) { }

        /// <summary>
        /// Add all recipes to the main loop.
        /// </summary>
        private void OnEntityCollecting(object sender, CollectEntitiesEventArgs args)
        {
            args.ToBeRandomised.AddRange(_entityHandler.GetAllCraftables());
            if (_config.UseEggs.Value)
            {
                args.ToBeRandomised.AddRange(_entityHandler.GetByCategory(TechTypeCategory.Eggs));
                args.ToBeRandomised.AddRange(_entityHandler.GetByCategory(TechTypeCategory.EggsHatched));
            }
        }

        /// <summary>
        /// When an entity enters the logic, add it is an ingredient if possible.
        /// </summary>
        private void OnEntityEnteredLogic(object sender, EntityEventArgs args)
        {
            LogicEntity entity = args.LogicEntity;
            if (entity.CanFunctionAsIngredient() && entity.HasUsesLeft())
                ValidIngredients.Add(entity);
        }

        /// <summary>
        /// When a knife or the waterpark are successfully randomised, ensure that seeds and eggs are made available.
        /// </summary>
        private void OnProgression(object sender, EntityEventArgs args)
        {
            UpdateValidIngredients(_manager.ReachableDepth);
        }

        private void OnSetupBeginning(object sender, EventArgs args)
        {
            _entityHandler.UpdateEntityValues(_config.RecipeValueMult.Value);
            
            // Decide which recipe mode will be used.
            switch (_config.RecipeMode.Value)
            {
                case (RecipeDifficultyMode.Balanced):
                    _mode = new ModeBalanced(_coreLogic, this, _coreLogic.GetRNG());
                    break;
                case (RecipeDifficultyMode.Chaotic):
                    _mode = new ModeRandom(_coreLogic, this, _coreLogic.GetRNG());
                    break;
                default:
                    _log.Error("Invalid recipe mode: " + _config.RecipeMode.Value);
                    break;
            }
            
            // Assemble a dictionary of what is considered basic outpost pieces which together should not exceed
            // the total cost defined in the config.
            BasicOutpostPieces = new Dictionary<TechType, int>
            {
                { TechType.BaseCorridorI, 1 },
                { TechType.BaseHatch, 1 },
                { TechType.BaseMapRoom, 1 },
                { TechType.BaseWindow, 1 },
                { TechType.Beacon, 1 },
                { TechType.SolarPanel, 2 },
            };

            // Define the direct recipe chains that are present in vanilla.
            if (_config.VanillaUpgradeChains.Value)
            {
                UpgradeChains = new Dictionary<TechType, TechType>
                {
                    { TechType.VehicleHullModule2, TechType.VehicleHullModule1 },
                    { TechType.VehicleHullModule3, TechType.VehicleHullModule2 },
                    { TechType.ExoHullModule2, TechType.ExoHullModule1 },
                    { TechType.CyclopsHullModule2, TechType.CyclopsHullModule1 },
                    { TechType.CyclopsHullModule3, TechType.CyclopsHullModule2 },
                    { TechType.HeatBlade, TechType.Knife },
                    { TechType.RepulsionCannon, TechType.PropulsionCannon },
                    { TechType.SwimChargeFins, TechType.Fins },
                    { TechType.UltraGlideFins, TechType.Fins },
                    { TechType.DoubleTank, TechType.Tank },
                    { TechType.PlasteelTank, TechType.DoubleTank },
                    { TechType.HighCapacityTank, TechType.DoubleTank },
                };
                ApplyUpgradeChainPrerequisites(_entityHandler, UpgradeChains);
            }
            else
            {
                UpgradeChains = new Dictionary<TechType, TechType>();
            }
            AddEggWaterParkPrerequisite(Bootstrap.SaveData.GetModuleData<RecipeSaveData>());
            
            // Add basic raw materials into the logic.
            UpdateValidIngredients(0);
        }

        /// <summary>
        /// Ensure that certain recipes are always randomised by a certain depth.
        /// </summary>
        private void OnSetupPriorityEntities(object sender, SetupPriorityEventArgs args)
        {
            // Ensure this setup is only done when the event is called from the manager itself.
            if (!(sender is ProgressionManager manager))
                return;
            
            manager.AddEssentialEntities(0, new []
            {
                TechType.Scanner,
                TechType.Welder,
                TechType.SmallStorage,
                TechType.BaseHatch,
                TechType.Fabricator,
            });
            manager.AddEssentialEntities(100, new []
            {
                TechType.Builder,
                TechType.BaseRoom,
                TechType.Seaglide,
                TechType.Tank,
            });
            manager.AddEssentialEntities(300, new []
            {
                TechType.BaseWaterPark,
            });
            
            manager.AddElectiveEntities(100, new []
            {
                new [] { TechType.Battery, TechType.BatteryCharger },
            });
            manager.AddElectiveEntities(200, new []
            {
                new [] { TechType.BaseBioReactor, TechType.SolarPanel },
                new [] { TechType.PowerCell, TechType.PowerCellCharger, TechType.SeamothSolarCharge },
                new [] { TechType.BaseBulkhead, TechType.BaseFoundation, TechType.BaseReinforcement },
            });
        }

        /// <summary>
        /// Ensure that knife and waterpark are considered progression items since they unlock seeds and eggs.
        /// </summary>
        private void OnSetupProgressionEntitites(object sender, SetupProgressionEventArgs args)
        {
            // Ensure this setup is only done when the event is called from the manager itself.
            if (!(sender is ProgressionManager))
                return;
            
            args.ProgressionEntities.Add(TechType.BaseWaterPark);
            args.ProgressionEntities.Add(TechType.HeatBlade);
            args.ProgressionEntities.Add(TechType.Knife);
            args.ProgressionEntities.Add(TechType.RadiationSuit);
        }
        
        /// <summary>
        /// Add the Alien Containment Unit as a prerequisite to all eggs.
        /// </summary>
        private void AddEggWaterParkPrerequisite(RecipeSaveData saveData)
        {
            saveData.DiscoverEggs = _config.DiscoverEggs.Value;
            if (!_config.DiscoverEggs.Value)
                _entityHandler.AddCategoryPrerequisite(TechTypeCategory.Eggs, TechType.BaseWaterPark);
            // Always add this requirement to fish hatched in containment.
            _entityHandler.AddCategoryPrerequisite(TechTypeCategory.EggsHatched, TechType.BaseWaterPark);
        }

        /// <summary>
        /// Add early elements of an upgrade chain as prerequisites of the later pieces to ensure that they are always
        /// randomised in order, and no Knife can require a Heatblade as ingredient.
        /// </summary>
        private void ApplyUpgradeChainPrerequisites(EntityHandler entityHandler, Dictionary<TechType, TechType> upgradeChains)
        {
            if (entityHandler is null || upgradeChains is null || upgradeChains.Count == 0)
                return;
            
            foreach (TechType upgrade in upgradeChains.Keys)
            {
                TechType ingredient = upgradeChains[upgrade];
                LogicEntity entity = entityHandler.GetEntity(upgrade);
                if (entity is null)
                    continue;
                if (!entity.HasPrerequisites)
                    entity.Prerequisites = new List<TechType>();
                entity.Prerequisites.Add(ingredient);
            }
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
            recipe.Ingredients = new List<RandomiserIngredient>();
            recipe.Ingredients.Add(new RandomiserIngredient(TechType.ScrapMetal, 1));
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
        
        /// <summary>
        /// Get the ingredient required for a given upgrade, if any. E.g. Seamoth Depth MK2 will return MK1.
        /// </summary>
        /// <param name="upgrade">The "Tier 2" entity to investigate for ingredients.</param>
        /// <returns>The TechType of the required "Tier 1" ingredient, or TechType.None if no such requirement exists.
        /// </returns>
        public TechType GetBaseOfUpgrade(TechType upgrade)
        {
            if (UpgradeChains.TryGetValue(upgrade, out TechType type))  
                return type;

            return TechType.None;
        }
        
        /// <summary>
        /// If vanilla upgrade chains are enabled, return that which this recipe upgrades from.
        /// <example>Returns the basic Knife when given HeatBlade.</example>
        /// </summary>
        /// <param name="upgrade">The upgrade to check for a base.</param>
        /// <param name="entityHandler">The list of all materials.</param>
        /// <returns>A LogicEntity if the given upgrade has a base it upgrades from, null otherwise.</returns>
        [CanBeNull]
        public LogicEntity GetBaseOfUpgrade(TechType upgrade, EntityHandler entityHandler)
        {
            TechType basicEntity = GetBaseOfUpgrade(upgrade);
            if (basicEntity.Equals(TechType.None))
                return null;

            return entityHandler.GetEntity(basicEntity);
        }

        /// <summary>
        /// Check whether any type of knife has been randomised and made accessible.
        /// </summary>
        private bool IsAnyKnifeRandomised()
        {
            return _coreLogic.HasRandomised(TechType.HeatBlade) || _coreLogic.HasRandomised(TechType.Knife);
        }

        /// <summary>
        /// Add non-craftable ingredients into the logic up to the given depth.
        /// This triggers an event, which this class listens to for registering new ingredients.
        /// </summary>
        private void UpdateValidIngredients(int depth)
        {
            if (IsAnyKnifeRandomised())
                _entityHandler.AddToLogic(TechTypeCategory.RawMaterials, depth);
            else
                _entityHandler.AddToLogic(TechTypeCategory.RawMaterials, depth, TechType.Knife, true);

            if (_config.UseFish.Value)
                _entityHandler.AddToLogic(TechTypeCategory.Fish, depth);
            if (_config.UseSeeds.Value && IsAnyKnifeRandomised())
                _entityHandler.AddToLogic(TechTypeCategory.Seeds, depth);
        }
    }
}