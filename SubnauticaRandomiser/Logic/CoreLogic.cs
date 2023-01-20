using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using HarmonyLib;
using JetBrains.Annotations;
using SubnauticaRandomiser.Handlers;
using SubnauticaRandomiser.Interfaces;
using SubnauticaRandomiser.Logic.Recipes;
using SubnauticaRandomiser.Objects;
using SubnauticaRandomiser.Objects.Enums;
using SubnauticaRandomiser.Objects.Events;
using SubnauticaRandomiser.Patches;
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
        public EntityHandler EntityHandler { get; private set; }
        public IRandomHandler Random { get; private set; }

        private ProgressionManager _manager;
        private SpoilerLog _spoilerLog;

        private List<Task> _fileTasks;
        private Dictionary<EntityType, ILogicModule> _handlingModules;
        private List<ILogicModule> _modules;
        private List<LogicEntity> _priorityEntities;

        /// <summary>
        /// Invoked during the setup stage, before the main loop begins.
        /// </summary>
        public event EventHandler SetupBeginning;

        /// <summary>
        /// Invoked during the setup stage. Use this event to add LogicEntities to the main loop.
        /// </summary>
        public event EventHandler<CollectEntitiesEventArgs> CollectingEntities;

        /// <summary>
        /// Invoked just before every logic module is called up to randomise everything which does not require
        /// access to the main loop.
        /// </summary>
        public event EventHandler OutOfLoopRandomisationBegin;
        
        /// <summary>
        /// Invoked once the next entity to be randomised has been determined.
        /// </summary>
        public event EventHandler<EntityEventArgs> EntityChosen;

        /// <summary>
        /// Invoked whenever an entity has been successfully randomised and added to the logic.
        /// </summary>
        public event EventHandler<EntityEventArgs> EntityRandomised;

        /// <summary>
        /// Invoked once the main loop has successfully completed.
        /// </summary>
        public event EventHandler MainLoopCompleted;

        /// <summary>
        /// Invoked once all randomisation has taken place and completed.
        /// </summary>
        public event EventHandler RandomisationCompleted;
        
        private void Awake()
        {
            _fileTasks = new List<Task>();
            _handlingModules = new Dictionary<EntityType, ILogicModule>();
            _modules = new List<ILogicModule>();
            _priorityEntities = new List<LogicEntity>();
            
            _Config = Initialiser._Config;
            _Log = Initialiser._Log;
            EntityHandler = new EntityHandler(_Log);
            Random = new RandomHandler(_Config.iSeed);
            
            _manager = gameObject.EnsureComponent<ProgressionManager>();
            _spoilerLog = gameObject.EnsureComponent<SpoilerLog>();
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

        /// <summary>
        /// Run the randomiser and start randomising!
        /// </summary>
        internal void Run()
        {
            _Serializer = new EntitySerializer(_Log);
            RegisterFileLoadTask(EntityHandler.ParseDataFileAsync(Initialiser._RecipeFile));
            EnableModules();
            // Wait and periodically check whether all file loading has completed. Only continue once that is done.
            StartCoroutine(WaitUntilFilesLoaded());
        }

        /// <summary>
        /// Once all datafiles have completed loading, start up the logic.
        /// </summary>
        private void Randomise()
        {
            List<LogicEntity> mainEntities = Setup();
            RandomisePreLoop();
            RandomiseMainEntities(mainEntities);
            ApplyAllChanges();
            _Serializer.Serialize(_Config);
        }

        /// <summary>
        /// Trigger setup events and prepare all data for starting the randomisation process.
        /// </summary>
        private List<LogicEntity> Setup()
        {
            _Log.Info("[Core] Setting up...");
            SetupBeginning?.Invoke(this, EventArgs.Empty);
            _manager.TriggerSetupEvents();
            
            // Set up the list of entities that need to be randomised in the main loop.
            CollectEntitiesEventArgs args = new CollectEntitiesEventArgs();
            CollectingEntities?.Invoke(this, args);
            return args.ToBeRandomised;
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
                // Try to get a handler for this type of entity.
                ILogicModule handler = _handlingModules.GetOrDefault(nextEntity.EntityType, null);
                if (handler is null)
                {
                    _Log.Warn($"[Core] Unhandled entity in main loop: {nextEntity.EntityType} {nextEntity}");
                    notRandomised.Remove(nextEntity);
                    continue;
                }

                // Let the module handle randomisation and report back.
                bool success = handler.RandomiseEntity(ref nextEntity);
                if (success)
                {
                    notRandomised.Remove(nextEntity);
                    EntityHandler.AddToLogic(nextEntity);
                    EntityRandomised?.Invoke(this, new EntityEventArgs(nextEntity));
                    _manager.TriggerProgressionEvents(nextEntity);
                }
            }
            
            _Log.Info($"[Core] Finished randomising within {circuitbreaker} cycles!");
            MainLoopCompleted?.Invoke(this, EventArgs.Empty);

            return _Serializer;
        }

        /// <summary>
        /// Add one or more entities to prioritise on the next main loop cycle.
        /// </summary>
        public void AddPriorityEntities(IEnumerable<LogicEntity> entities)
        {
            _priorityEntities.AddRange(entities);
        }

        internal void ApplyAllChanges()
        {
            if (_Serializer is null)
                throw new InvalidDataException("Cannot apply randomisation changes: Serializer is null!");
            
            // Load recipe changes.
            if (_Serializer.RecipeDict?.Count > 0)
                RecipeLogic.ApplyMasterDict(_Serializer);
                
            // Load fragment changes.
            if (_Serializer.SpawnDataDict?.Count > 0 || _Serializer.NumFragmentsToUnlock?.Count > 0)
            {
                FragmentLogic.ApplyMasterDict(_Serializer);
                _Log.Info("Loaded fragment state.");
            }

            // Load any changes that rely on harmony patches.
            EnableHarmony();
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
            next ??= Random.Choice(notRandomised);

            // Invoke the associated event.
            EntityChosen?.Invoke(this, new EntityEventArgs(next));

            return next;
        }

        /// <summary>
        /// Enables all necessary harmony patches based on the randomisation state in the serialiser.
        /// </summary>
        private void EnableHarmony()
        {
            Harmony harmony = new Harmony(Initialiser.GUID);
            foreach (ILogicModule module in _modules)
            {
                module.SetupHarmonyPatches(harmony);
            }
            // Always apply bugfixes.
            harmony.PatchAll(typeof(VanillaBugfixes));
        }

        /// <summary>
        /// Check whether the given LogicEntity has already been randomised.
        /// </summary>
        public bool HasRandomised(LogicEntity entity)
        {
            return EntityHandler.IsInLogic(entity);
        }

        /// <summary>
        /// Check whether the given TechType has already been randomised.
        /// </summary>
        /// <exception cref="ArgumentNullException">If the TechType does not have an associated LogicEntity.</exception>
        public bool HasRandomised(TechType techType)
        {
            return EntityHandler.IsInLogic(techType);
        }

        /// <summary>
        /// Register a task responsible for loading critical data. Randomising will only begin once all of these tasks
        /// have completed. Use this to ensure your module finishes loading data from disk before randomisation begins.
        /// </summary>
        /// <param name="task">The Task object from an async method for loading data files.</param>
        public void RegisterFileLoadTask(Task task)
        {
            _fileTasks.Add(task);
        }

        /// <summary>
        /// Register a logic module as a handler for a specific entity type. This will cause that handler's
        /// RandomiseEntity() method to be called whenever an entity of that type needs to be randomised.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown if a handler for the given type of entity already
        /// exists. There can be only one per type.</exception>
        public void RegisterEntityHandler(EntityType type, ILogicModule module)
        {
            if (_handlingModules.ContainsKey(type))
                throw new ArgumentException($"A handler for entity type '{type}' already exists: "
                                            + $"{_handlingModules[type].GetType()}");
            
            _handlingModules.Add(type, module);
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

        /// <summary>
        /// Try to restore a game state from disk.
        /// </summary>
        internal bool TryRestoreSave()
        {
            if (string.IsNullOrEmpty(_Config.sBase64Seed))
            {
                _Log.Debug("[Core] base64 seed is empty.");
                return false;
            }

            _Log.Debug("[Core] Trying to decode base64 string...");
            EntitySerializer serializer = EntitySerializer.FromBase64String(_Config.sBase64Seed);

            if (serializer?.SpawnDataDict is null || serializer.RecipeDict is null)
            {
                _Log.Error("[Core] base64 seed is invalid; could not deserialize.");
                return false;
            }
            _Serializer = serializer;
            _Log.Debug("[Core] Save data restored.");
            return true;
        }
        
        private IEnumerator WaitUntilFilesLoaded()
        {
            yield return new WaitUntil(() => _fileTasks.TrueForAll(task => task.IsCompleted));
            Randomise();
        }
    }
}
