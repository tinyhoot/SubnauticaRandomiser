using System;
using System.Collections.Generic;
using HarmonyLib;
using JetBrains.Annotations;
using SubnauticaRandomiser.Interfaces;
using SubnauticaRandomiser.Logic.Recipes;
using SubnauticaRandomiser.Objects;
using SubnauticaRandomiser.Objects.Enums;
using SubnauticaRandomiser.Objects.Events;
using UnityEngine;
using ILogHandler = SubnauticaRandomiser.Interfaces.ILogHandler;

namespace SubnauticaRandomiser.Logic
{
    /// <summary>
    /// Acts as the core for handling all randomising logic in the mod, and turning modules on/off as needed.
    /// </summary>
    internal class CoreLogic : MonoBehaviour
    {
        internal RandomiserConfig _Config { get; private set; }
        internal ILogHandler _Log { get; private set; }
        internal EntitySerializer _Serializer { get; private set; }
        internal EntityHandler _EntityHandler { get; private set; }
        internal IRandomHandler _Random { get; private set; }

        private ProgressionManager _manager;
        private SpoilerLog _spoilerLog;

        private readonly AlternateStartLogic _altStartLogic;
        private readonly AuroraLogic _auroraLogic;
        private readonly DataboxLogic _databoxLogic;
        internal readonly FragmentLogic _fragmentLogic;
        private RecipeLogic _recipeLogic;

        private Dictionary<EntityType, ILogicModule> _entityHandlers;
        private List<ILogicModule> _modules;
        private List<LogicEntity> _priorityEntities;
        
        /// <summary>
        /// Invoked during the setup stage, before the main loop begins.
        /// </summary>
        public event EventHandler OnSetup;

        /// <summary>
        /// Invoked during the setup stage. Use this event to add LogicEntities to the main loop.
        /// </summary>
        public event EventHandler<CollectEntitiesEventArgs> OnCollectRandomisableEntities;

        /// <summary>
        /// Invoked just before every logic module is called up to randomise everything which does not require
        /// access to the main loop.
        /// </summary>
        public event EventHandler OnOutOfLoopRandomisationBegin;
        
        /// <summary>
        /// Invoked once the next entity to be randomised has been determined.
        /// </summary>
        public event EventHandler<EntityEventArgs> OnEntityChosen;

        /// <summary>
        /// Invoked whenever an entity has been successfully randomised and added to the logic.
        /// </summary>
        public event EventHandler<EntityEventArgs> OnEntityRandomised;

        /// <summary>
        /// Invoked once the main loop has successfully completed.
        /// </summary>
        public event EventHandler OnMainLoopComplete;

        /// <summary>
        /// Invoked once all randomisation has taken place and completed.
        /// </summary>
        public event EventHandler OnPostRandomisation;
        
        public void Awake()
        {
            _entityHandlers = new Dictionary<EntityType, ILogicModule>();
            _modules = new List<ILogicModule>();
            _priorityEntities = new List<LogicEntity>();
            
            _Config = Initialiser._Config;
            _Log = Initialiser._Log;
            // This makes the async function run sync, but in this specific case that's necessary for more setup.
            List<LogicEntity> allMaterials = CSVReader.ParseDataFileAsync(Initialiser._RecipeFile, CSVReader.ParseRecipeLine).Result;
            _EntityHandler = new EntityHandler(allMaterials, _Log);
            _Serializer = new EntitySerializer(_Log);
            _Random = new RandomHandler(_Config.iSeed);
            
            _manager = gameObject.EnsureComponent<ProgressionManager>();
            _spoilerLog = gameObject.EnsureComponent<SpoilerLog>();
            EnableModules();
        }

        private void EnableModules()
        {
            if (!_Config.sSpawnPoint.Equals("Vanilla"))
                RegisterModule<AlternateStartLogic>();
            if (_Config.bRandomiseDataboxes)
                RegisterModule<DataboxLogic>();
            if (_Config.bRandomiseFragments || _Config.bRandomiseNumFragments || _Config.bRandomiseDuplicateScans)
                RegisterModule<FragmentLogic>();
            if (_Config.bRandomiseRecipes)
                RegisterModule<RecipeLogic>();
        }

        public void StartRandomisation()
        {
            List<LogicEntity> mainEntities = Setup();
            RandomisePreLoop();
            RandomiseMainEntities(mainEntities);
            EnableHarmony();
        }

        /// <summary>
        /// Set up all the necessary structures for later.
        /// </summary>
        private List<LogicEntity> Setup()
        {
            _Log.Info("[Core] Setting up...");
            OnSetup(this, EventArgs.Empty);
            _manager.TriggerSetupEvents();
            
            // Set up the list of entities that need to be randomised in the main loop.
            CollectEntitiesEventArgs args = new CollectEntitiesEventArgs();
            OnCollectRandomisableEntities(this, args);
            //return args.ToBeRandomised;
            
            // ----- OLD STUFF BELOW -----
            
            // Init the progression tree.
            _Tree.SetupVanillaTree();
            _altStartLogic?.RandomiseOutOfLoop(_Serializer);
            if (_Config.bRandomiseDoorCodes)
                _auroraLogic.RandomiseDoorCodes();
            if (_Config.bRandomiseSupplyBoxes)
                _auroraLogic.RandomiseSupplyBoxes();

            if (_databoxLogic != null)
            {
                // Just randomise those flat out for now, instead of including them in the core loop.
                _databoxLogic.RandomiseDataboxes();
                _databoxLogic.UpdateBlueprints(_EntityHandler.GetAll());
                _databoxLogic.LinkCyclopsHullModules(_EntityHandler);
            }

            if (_fragmentLogic != null)
            {
                if (_Config.bRandomiseFragments)
                {
                    _Tree.SetupFragments();
                    // Initialise the fragment cache and remove vanilla spawns.
                    FragmentLogic.Init();
                    // Queue up all fragments to be randomised.
                    notRandomised.AddRange(_EntityHandler.GetAllFragments());
                }
                
                // Randomise the number of fragment scans required per blueprint.
                if (_Config.bRandomiseNumFragments)
                    _fragmentLogic.RandomiseNumFragments(_EntityHandler.GetAllFragments());
                
                // Randomise duplicate scan rewards.
                if (_Config.bRandomiseDuplicateScans)
                    _fragmentLogic.CreateDuplicateScanYieldDict();
            }
            
            if (_recipeLogic != null)
            {
                _recipeLogic.UpdateReachableMaterials(0);
                // Queue up all craftables to be randomised.
                notRandomised.AddRange(_EntityHandler.GetAllCraftables());
                
                // Update the progression tree with recipes.
                _Tree.SetupRecipes(_Config.bVanillaUpgradeChains);
                if (_Config.bVanillaUpgradeChains)
                    _Tree.ApplyUpgradeChainToPrerequisites(_EntityHandler.GetAll());
            }
        }

        /// <summary>
        /// Call the generic randomisation method of each registered module.
        /// </summary>
        private void RandomisePreLoop()
        {
            _Log.Info("[Core] Randomising: Pre-loop content");
            foreach (ILogicModule module in _modules)
            {
                module.RandomiseOutOfLoop(_Serializer);
            }
        }
        
        /// <summary>
        /// Start the main loop of the randomisation process.
        /// </summary>
        /// <returns>A serialisation instance containing all changes made.</returns>
        /// <exception cref="TimeoutException">Raised to prevent infinite loops if the core loop takes too long to find
        /// a valid solution.</exception>
        private EntitySerializer RandomiseMainEntities(List<LogicEntity> notRandomised)
        {
            _Log.Info("[Core] Randomising: Entering main loop");

            int circuitbreaker = 0;
            while (notRandomised.Count > 0)
            {
                circuitbreaker++;
                if (circuitbreaker > 3000)
                {
                    _Log.InGameMessage("[Core] Failed to randomise items: stuck in infinite loop!");
                    _Log.Fatal("[Core] Encountered infinite loop, aborting!");
                    throw new TimeoutException("Encountered infinite loop while randomising!");
                }

                LogicEntity nextEntity = ChooseNextEntity(notRandomised);
                // Choose a logic appropriate to the entity.
                ILogicModule handler = _entityHandlers.GetOrDefault(nextEntity.EntityType, null);
                if (handler is null)
                {
                    _Log.Warn($"[Core] Unsupported entity in main loop: {nextEntity.EntityType} {nextEntity}");
                    notRandomised.Remove(nextEntity);
                    continue;
                }

                bool success = handler.RandomiseEntity(ref nextEntity);
                if (success)
                {
                    notRandomised.Remove(nextEntity);
                    _EntityHandler.AddToLogic(nextEntity);
                    OnEntityRandomised(this, new EntityEventArgs(nextEntity));
                    _manager.TriggerProgressionEvents(nextEntity);
                }
            }
            
            _Log.Info($"[Core] Finished randomising within {circuitbreaker} cycles!");
            OnMainLoopComplete(this, EventArgs.Empty);

            return _Serializer;
        }

        /// <summary>
        /// Add one or more entities to prioritise on the next main loop cycle.
        /// </summary>
        public void AddPriorityEntities(IEnumerable<LogicEntity> entities)
        {
            _priorityEntities.AddRange(entities);
        }

        /// <summary>
        /// Get the next entity to be randomised, prioritising essential or elective ones.
        /// </summary>
        /// <returns>The next entity.</returns>
        [NotNull]
        private LogicEntity ChooseNextEntity(List<LogicEntity> notRandomised)
        {
            // Make sure the list of absolutely essential items is done first, for each depth level. This guarantees
            // certain recipes are done by a certain depth, e.g. waterparks by 500m.
            LogicEntity next = null;
            if (_priorityEntities.Count > 0)
            {
                next = _priorityEntities[0];
                _priorityEntities.RemoveAt(0);
            }
            next ??= _Random.Choice(notRandomised);

            // Invoke the associated event.
            OnEntityChosen(this, new EntityEventArgs(next));

            return next;
        }

        private void EnableHarmony()
        {
            Harmony harmony = new Harmony(Initialiser.GUID);
            foreach (ILogicModule module in _modules)
            {
                module.SetupHarmonyPatches(harmony);
            }
        }

        /// <summary>
        /// Check whether the given LogicEntity has already been randomised.
        /// </summary>
        public bool HasRandomised(LogicEntity entity)
        {
            return entity.InLogic;
        }

        /// <summary>
        /// Check whether the given TechType has already been randomised.
        /// TODO: Improve to not look up entity every single time.
        /// </summary>
        /// <exception cref="ArgumentNullException">If the TechType does not have an associated LogicEntity.</exception>
        public bool HasRandomised(TechType techType)
        {
            LogicEntity entity = _EntityHandler.GetEntity(techType);
            if (entity is null)
                throw new ArgumentNullException(nameof(techType), $"There is no LogicEntity corresponding to {techType}!");
            return entity.InLogic;
        }

        /// <summary>
        /// Register a logic module as a handler for a specific entity type. This will cause that handler's
        /// RandomiseEntity() method to be called whenever an entity of that type needs to be randomised.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown if a handler for the given type of entity already
        /// exists. There can be only one per type.</exception>
        public void RegisterEntityHandler(EntityType type, ILogicModule module)
        {
            if (_entityHandlers.ContainsKey(type))
                throw new ArgumentException($"A handler for entity type '{type}' already exists: "
                                            + $"{_entityHandlers[type].GetType()}");
            
            _entityHandlers.Add(type, module);
        }

        /// <summary>
        /// Register a component module for use with the randomiser.
        /// </summary>
        /// <returns>The instantiated component.</returns>
        public TLogicModule RegisterModule<TLogicModule>() where TLogicModule : MonoBehaviour, ILogicModule
        {
            TLogicModule component = gameObject.EnsureComponent<TLogicModule>();
            _modules.Add(component);
            return component;
        }
    }
}
