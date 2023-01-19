using System;
using System.Collections.Generic;
using HarmonyLib;
using JetBrains.Annotations;
using SMLHelper.V2.Handlers;
using SubnauticaRandomiser.Handlers;
using SubnauticaRandomiser.Interfaces;
using SubnauticaRandomiser.Objects;
using SubnauticaRandomiser.Objects.Enums;
using SubnauticaRandomiser.Objects.Events;
using UnityEngine;
using ILogHandler = SubnauticaRandomiser.Interfaces.ILogHandler;

namespace SubnauticaRandomiser.Logic.Recipes
{
    /// <summary>
    /// Handles everything related to randomising recipes.
    /// </summary>
    [RequireComponent(typeof(CoreLogic), typeof(ProgressionManager))]
    internal class RecipeLogic : MonoBehaviour, ILogicModule
    {
        private CoreLogic _coreLogic;
        private ProgressionManager _manager;

        private RandomiserConfig _config;
        private ILogHandler _log;
        private EntityHandler _entityHandler;
        private Mode _mode;

        public Dictionary<TechType, int> BasicOutpostPieces { get; private set; }
        public Dictionary<TechType, TechType> UpgradeChains { get; private set; }
        public HashSet<LogicEntity> ValidIngredients { get; private set; }
        

        private void Awake()
        {
            _coreLogic = GetComponent<CoreLogic>();
            _manager = GetComponent<ProgressionManager>();
            _config = _coreLogic._Config;
            _entityHandler = _coreLogic._EntityHandler;
            _log = _coreLogic._Log;
            ValidIngredients = new HashSet<LogicEntity>(new LogicEntityEqualityComparer());
            
            // Decide which recipe mode will be used.
            switch (_config.iRandomiserMode)
            {
                case (0):
                    _mode = new ModeBalanced(_coreLogic, this);
                    break;
                case (1):
                    _mode = new ModeRandom(_coreLogic, this);
                    break;
                default:
                    _log.Error("[R] Invalid recipe mode: " + _config.iRandomiserMode);
                    break;
            }
            
            // Register events.
            _coreLogic.OnCollectRandomisableEntities += OnCollectRandomisableEntities;
            _coreLogic.OnSetup += OnSetup;
            _entityHandler.OnEnterLogic += OnEntityEnterLogic;
            _manager.OnProgression += OnProgression;
            _manager.OnSetupPriority += OnSetupPriorityEntities;
            _manager.OnSetupProgression += OnSetupProgressionEntitites;
            // Register this module as handler for recipe type entities.
            _coreLogic.RegisterEntityHandler(EntityType.Recipe, this);
        }

        public void RandomiseOutOfLoop(EntitySerializer serializer)
        {
            _mode.ChooseBaseTheme(100, _config.bUseFish);
        }

        /// <summary>
        /// Randomise a recipe entity.
        /// </summary>
        /// <returns>True if successful, false if something went wrong.</returns>
        public bool RandomiseEntity(ref LogicEntity entity)
        {
            // Does this recipe have all of its prerequisites fulfilled? Skip this check if the recipe is a priority.
            if (!(_manager.IsPriorityEntity(entity, _manager.ReachableDepth)
                  || (entity.CheckBlueprintFulfilled(_coreLogic, _manager.ReachableDepth) && entity.CheckPrerequisitesFulfilled(_coreLogic))))
            {
                _log.Debug($"[R] --- Recipe [{entity}] did not fulfill requirements, skipping.");
                return false;
            }
            
            entity = _mode.RandomiseIngredients(entity);
            ApplyRandomisedRecipe(entity.Recipe);

            // Only add this entity to the ingredients list if it can actually be an ingredient.
            // TODO: Move to event, out of this class
            if (entity.CanFunctionAsIngredient())
                ValidIngredients.Add(entity);

            entity.InLogic = true;
            _log.Debug($"[R][+] Randomised recipe for [{entity}].");

            return true;
        }

        public void SetupHarmonyPatches(Harmony harmony)
        {
        }

        /// <summary>
        /// Add all recipes to the main loop.
        /// </summary>
        private void OnCollectRandomisableEntities(object sender, CollectEntitiesEventArgs args)
        {
            args.ToBeRandomised.AddRange(_entityHandler.GetAllCraftables());
        }

        /// <summary>
        /// When an entity enters the logic, add it is an ingredient if possible.
        /// </summary>
        private void OnEntityEnterLogic(object sender, EntityEventArgs args)
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
            int depth = _manager.ReachableDepth;
            
            if (IsAnyKnifeRandomised())
                _entityHandler.AddToLogic(TechTypeCategory.RawMaterials, depth);
            else
                _entityHandler.AddToLogic(TechTypeCategory.RawMaterials, depth, TechType.Knife, true);

            if (_config.bUseFish)
                _entityHandler.AddToLogic(TechTypeCategory.Fish, depth);
            if (_config.bUseEggs && _coreLogic.HasRandomised(TechType.BaseWaterPark))
                _entityHandler.AddToLogic(TechTypeCategory.Eggs, depth);
            if (_config.bUseSeeds && IsAnyKnifeRandomised())
                _entityHandler.AddToLogic(TechTypeCategory.Seeds, depth);
        }

        private void OnSetup(object sender, EventArgs args)
        {
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
            if (_config.bVanillaUpgradeChains)
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

                if (!entity.HasPrerequisites)
                    entity.Prerequisites = new List<TechType>();
                entity.Prerequisites.Add(ingredient);
            }
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
        /// Apply a randomised recipe to the in-game craft data, and store a copy in the master dictionary.
        /// </summary>
        /// <param name="recipe">The recipe to change.</param>
        private void ApplyRandomisedRecipe(Recipe recipe)
        {
            _coreLogic._Serializer.AddRecipe(recipe.TechType, recipe);
        }

        /// <summary>
        /// Apply all recipe changes stored in the masterDict to the game.
        /// </summary>
        /// <param name="serializer">The master dictionary.</param>
        internal static void ApplyMasterDict(EntitySerializer serializer)
        {
            Dictionary<TechType, Recipe>.KeyCollection keys = serializer.RecipeDict.Keys;

            foreach (TechType key in keys)
            {
                CraftDataHandler.SetTechData(key, serializer.RecipeDict[key]);
            }

            // TODO Once scrap metal is working, un-commenting this will apply the change on every startup.
            //ChangeScrapMetalResult(masterDict.DictionaryInstance[TechType.Titanium]);
        }
    }
}