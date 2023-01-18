using System;
using System.Collections.Generic;
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
        internal readonly Materials _Materials;
        internal IRandomHandler _Random { get; private set; }
        internal readonly SpoilerLog _SpoilerLog;
        internal readonly ProgressionTree _Tree;
        
        private ProgressionManager _manager;

        private readonly AlternateStartLogic _altStartLogic;
        private readonly AuroraLogic _auroraLogic;
        private readonly DataboxLogic _databoxLogic;
        internal readonly FragmentLogic _fragmentLogic;
        private RecipeLogic _recipeLogic;

        private Dictionary<EntityType, ILogicModule> _entityHandlers;
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
        
        public void Awake()
        {
            _manager = gameObject.EnsureComponent<ProgressionManager>();
            
            _entityHandlers = new Dictionary<EntityType, ILogicModule>();
            _priorityEntities = new List<LogicEntity>();
            
            _Config = Initialiser._Config;
            _Log = Initialiser._Log;
            _Serializer = new EntitySerializer(_Log);
            _Random = new RandomHandler(_Config.iSeed);
        }

        public void Start()
        {
            
        }

        public CoreLogic(IRandomHandler random, RandomiserConfig config, ILogHandler logger, List<LogicEntity> allMaterials,
            Dictionary<EBiomeType, List<float[]>> alternateStarts, List<BiomeCollection> biomes = null,
            List<Databox> databoxes = null)
        {
            _Config = config;
            _Log = logger;
            _Serializer = new EntitySerializer(logger);
            _Materials = new Materials(allMaterials, logger);
            _Random = random;
            _SpoilerLog = new SpoilerLog(config, logger, _Serializer);
            
            if (!_Config.sSpawnPoint.StartsWith("Vanilla"))
                _altStartLogic = new AlternateStartLogic(alternateStarts, config, logger, random);
            _auroraLogic = new AuroraLogic(this);
            if (_Config.bRandomiseDataboxes)
                _databoxLogic = new DataboxLogic(this, databoxes);
            if (_Config.bRandomiseFragments || _Config.bRandomiseNumFragments || _Config.bRandomiseDuplicateScans)
                _fragmentLogic = new FragmentLogic(this, biomes);
            if (_Config.bRandomiseRecipes)
                _recipeLogic = new RecipeLogic(this);
            _Tree = new ProgressionTree();
        }

        /// <summary>
        /// Set up all the necessary structures for later.
        /// </summary>
        private void Setup(List<LogicEntity> notRandomised)
        {
            _manager.TriggerSetupEvents();
            
            // ----- OLD STUFF BELOW -----
            
            // Init the progression tree.
            _Tree.SetupVanillaTree();
            _altStartLogic?.Randomise(_Serializer);
            if (_Config.bRandomiseDoorCodes)
                _auroraLogic.RandomiseDoorCodes();
            if (_Config.bRandomiseSupplyBoxes)
                _auroraLogic.RandomiseSupplyBoxes();

            if (_databoxLogic != null)
            {
                // Just randomise those flat out for now, instead of including them in the core loop.
                _databoxLogic.RandomiseDataboxes();
                _databoxLogic.UpdateBlueprints(_Materials.GetAll());
                _databoxLogic.LinkCyclopsHullModules(_Materials);
            }

            if (_fragmentLogic != null)
            {
                if (_Config.bRandomiseFragments)
                {
                    _Tree.SetupFragments();
                    // Initialise the fragment cache and remove vanilla spawns.
                    FragmentLogic.Init();
                    // Queue up all fragments to be randomised.
                    notRandomised.AddRange(_Materials.GetAllFragments());
                }
                
                // Randomise the number of fragment scans required per blueprint.
                if (_Config.bRandomiseNumFragments)
                    _fragmentLogic.RandomiseNumFragments(_Materials.GetAllFragments());
                
                // Randomise duplicate scan rewards.
                if (_Config.bRandomiseDuplicateScans)
                    _fragmentLogic.CreateDuplicateScanYieldDict();
            }
            
            if (_recipeLogic != null)
            {
                _recipeLogic.UpdateReachableMaterials(0);
                // Queue up all craftables to be randomised.
                notRandomised.AddRange(_Materials.GetAllCraftables());
                
                // Update the progression tree with recipes.
                _Tree.SetupRecipes(_Config.bVanillaUpgradeChains);
                if (_Config.bVanillaUpgradeChains)
                    _Tree.ApplyUpgradeChainToPrerequisites(_Materials.GetAll());
            }
        }
        
        /// <summary>
        /// Start the randomisation process.
        /// </summary>
        /// <returns>A serialisation instance containing all changes made.</returns>
        /// <exception cref="TimeoutException">Raised to prevent infinite loops if the core loop takes too long to find
        /// a valid solution.</exception>
        internal EntitySerializer Randomise()
        {
            _Log.Info("Randomising using logic-based system...");
            
            List<LogicEntity> notRandomised = new List<LogicEntity>();
            Dictionary<TechType, bool> unlockedProgressionItems = new Dictionary<TechType, bool>();

            // Set up basic structures.
            Setup(notRandomised);

            int circuitbreaker = 0;
            int currentDepth = 0;
            int numProgressionItems = -1; // This forces a depth calculation on the first loop.
            while (notRandomised.Count > 0)
            {
                circuitbreaker++;
                if (circuitbreaker > 3000)
                {
                    _Log.InGameMessage("Failed to randomise items: stuck in infinite loop!");
                    _Log.Fatal("Encountered infinite loop, aborting!");
                    throw new TimeoutException("Encountered infinite loop while randomising!");
                }
                
                // Update depth and reachable materials.
                currentDepth = UpdateReachableDepth(currentDepth, unlockedProgressionItems, numProgressionItems);
                numProgressionItems = unlockedProgressionItems.Count;

                LogicEntity nextEntity = ChooseNextEntity(notRandomised, currentDepth);

                // Choose a logic appropriate to the entity.
                bool? success = null;
                if (nextEntity.IsFragment)
                    success = _fragmentLogic.RandomiseFragment(nextEntity, unlockedProgressionItems, currentDepth);
                else if (nextEntity.HasRecipe)
                    success = _recipeLogic.RandomiseRecipe(nextEntity, unlockedProgressionItems, currentDepth);
                
                if (success == true)
                {
                    notRandomised.Remove(nextEntity);
                    nextEntity.InLogic = true;
                    OnEntityRandomised(this, new EntityEventArgs(nextEntity));
                    _manager.TriggerProgressionEvents(nextEntity);
                }

                if (success is null)
                    _Log.Warn("Unsupported entity in loop: " + nextEntity);
            }

            _Log.Info($"Finished randomising within {circuitbreaker} cycles!");
            _SpoilerLog.WriteLog();

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
        private LogicEntity ChooseNextEntity(List<LogicEntity> notRandomised, int depth)
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
            LogicEntity entity = _Materials.Find(techType);
            if (entity is null)
                throw new ArgumentNullException(nameof(techType), $"There is no LogicEntity corresponding to {techType}!");
            return entity.InLogic;
        }

        /// <summary>
        /// Update the depth that can be reached and trigger any changes that need to happen if a new significant
        /// threshold has been passed.
        /// </summary>
        /// <param name="currentDepth">The previously reachable depth.</param>
        /// <param name="progressionItems">The unlocked progression items.</param>
        /// <param name="numItems">The number of progression items on the previous cycle.</param>
        /// <returns>The new maximum depth.</returns>
        private int UpdateReachableDepth(int currentDepth, Dictionary<TechType, bool> progressionItems, int numItems)
        {
            if (progressionItems.Count <= numItems)
                return currentDepth;
            
            int newDepth = CalculateReachableDepth(progressionItems, _Config.iDepthSearchTime);
            _SpoilerLog.UpdateLastProgressionEntry(newDepth);
            currentDepth = Math.Max(currentDepth, newDepth);
            _recipeLogic?.UpdateReachableMaterials(currentDepth);

            return currentDepth;
        }

        /// <summary>
        /// Check whether all TechTypes given in the array are present in the given dictionary.
        /// </summary>
        /// <param name="dict">The dictionary to check.</param>
        /// <param name="types">The array of TechTypes.</param>
        /// <returns>True if all TechTypes are present in the dictionary, false otherwise.</returns>
        private static bool CheckDictForAllTechTypes(Dictionary<TechType, bool> dict, TechType[] types)
        {
            bool allItemsPresent = true;

            foreach (TechType t in types)
            {
                allItemsPresent &= dict.ContainsKey(t);
                if (!allItemsPresent)
                    break;
            }

            return allItemsPresent;
        }

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
        public T RegisterModule<T>() where T : MonoBehaviour, ILogicModule
        {
            return gameObject.EnsureComponent<T>();
        }
    }
}
