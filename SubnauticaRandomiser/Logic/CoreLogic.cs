﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HarmonyLib;
using SubnauticaRandomiser.Configuration;
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
    /// Acts as the core for handling all randomising logic in the mod, registering or enabling modules, and invoking
    /// the most important events.
    /// </summary>
    [DisallowMultipleComponent]
    internal class CoreLogic : MonoBehaviour
    {
        public static CoreLogic Main;
        
        internal Config _Config { get; private set; }
        internal ILogHandler _Log { get; private set; }
        internal static EntitySerializer _Serializer { get; private set; }
        public EntityHandler EntityHandler { get; private set; }
        public IRandomHandler Random { get; private set; }

        private Harmony _harmony;
        private ProgressionManager _manager;
        private SaveFile _saveFile;
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
        public event EventHandler<CollectEntitiesEventArgs> EntityCollecting;

        /// <summary>
        /// Invoked just before every logic module is called up to randomise everything which does not require
        /// access to the main loop.
        /// </summary>
        public event EventHandler PreLoopRandomising;
        
        /// <summary>
        /// Invoked once the next entity to be randomised has been determined.
        /// </summary>
        public event EventHandler<EntityEventArgs> EntityChosen;

        /// <summary>
        /// Invoked whenever an entity has been successfully randomised and added to the logic.
        /// </summary>
        public event EventHandler<EntityEventArgs> EntityRandomised;

        /// <summary>
        /// Invoked at the beginning of the main loop.
        /// </summary>
        public event EventHandler MainLoopRandomising;

        /// <summary>
        /// Invoked once the main loop has successfully completed.
        /// </summary>
        public event EventHandler MainLoopCompleted;

        private void Awake()
        {
            Main = this;
            
            _fileTasks = new List<Task>();
            _handlingModules = new Dictionary<EntityType, ILogicModule>();
            _modules = new List<ILogicModule>();
            _priorityEntities = new List<LogicEntity>();
            
            _Config = Initialiser._Config;
            _Log = Initialiser._Log;
            EntityHandler = new EntityHandler(_Log);
            Random = new RandomHandler(_Config.Seed.Value);
            _saveFile = Initialiser._SaveFile;
            
            _manager = gameObject.EnsureComponent<ProgressionManager>();
            _spoilerLog = gameObject.EnsureComponent<SpoilerLog>();
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }

        private void EnableModules()
        {
            if (_Config.EnableAlternateStartModule.Value && !_Config.SpawnPoint.Value.Equals("Vanilla"))
                RegisterModule<AlternateStartLogic>();
            if (_Config.RandomiseDoorCodes.Value || _Config.RandomiseSupplyBoxes.Value)
                RegisterModule<AuroraLogic>();
            if (_Config.RandomiseDataboxes.Value)
                RegisterModule<DataboxLogic>();
            if (_Config.EnableFragmentModule.Value &&
                (_Config.RandomiseFragments.Value || _Config.RandomiseNumFragments.Value || _Config.RandomiseDuplicateScans.Value))
                RegisterModule<FragmentLogic>();
            if (_Config.EnableRecipeModule.Value && _Config.RandomiseRecipes.Value)
            {
                RegisterModule<RawMaterialLogic>();
                RegisterModule<RecipeLogic>();
            }
        }

        /// <summary>
        /// Run the randomiser and start randomising!
        /// </summary>
        internal void Run()
        {
            _Serializer = null;
            RegisterFileLoadTask(EntityHandler.ParseDataFileAsync(Initialiser._RecipeFile));
            EnableModules();
            // Wait and periodically check whether all file loading has completed. Only continue once that is done.
            StartCoroutine(WaitUntilFilesLoaded());
        }

        /// <summary>
        /// Once all datafiles have completed loading, start up the logic.
        /// Running this as a coroutine spaces the logic out over several frames, which prevents the game from
        /// locking up / freezing.
        /// </summary>
        private IEnumerator Randomise()
        {
            _Serializer = new EntitySerializer(_Log);
            List<LogicEntity> mainEntities = Setup();
            RandomisePreLoop();
            
            // Force a new frame before the main loop.
            yield return null;
            yield return Utils.WrapCoroutine(RandomiseMainEntities(mainEntities), Initialiser.FatalError);
            yield return null;
            ApplyAllChanges();
            
            _Serializer.EnabledModules = _modules.Select(module => module.GetType()).ToList();
            _Serializer.Serialize(_saveFile, Initialiser._ExpectedSaveVersion);
            
            _Log.InGameMessage("Finished randomising! Please restart your game for all changes to take effect.");
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
            EntityCollecting?.Invoke(this, args);
            return args.ToBeRandomised;
        }

        /// <summary>
        /// Call the generic randomisation method of each registered module.
        /// </summary>
        private void RandomisePreLoop()
        {
            _Log.Info("[Core] Randomising: Pre-loop content");
            PreLoopRandomising?.Invoke(this, EventArgs.Empty);
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
        private IEnumerator RandomiseMainEntities(List<LogicEntity> notRandomised)
        {
            _Log.Info("[Core] Randomising: Entering main loop");
            MainLoopRandomising?.Invoke(this, EventArgs.Empty);

            int circuitbreaker = 0;
            while (notRandomised.Count > 0)
            {
                circuitbreaker++;
                // Stop calculating and wait for the next frame every so often. Slower, but doesn't block the game.
                if (circuitbreaker % 50 == 0)
                    yield return null;
                if (circuitbreaker > 3000)
                {
                    _Log.InGameMessage("[Core] Failed to randomise entities: stuck in infinite loop!");
                    _Log.Fatal("[Core] Encountered infinite loop, aborting!");
                    throw new TimeoutException("Encountered infinite loop while randomising!");
                }

                LogicEntity nextEntity = ChooseNextEntity(notRandomised);
                if (nextEntity is null)
                    continue;
                // Try to get a handler for this type of entity.
                ILogicModule handler = _handlingModules.GetOrDefault(nextEntity.EntityType, null);
                if (handler is null)
                {
                    _Log.Warn($"[Core] Unhandled entity in main loop: {nextEntity.EntityType} {nextEntity}");
                    // Add the unhandled entity into logic as a stopgap solution, for cases where a prerequisite check
                    // would fail because it expects unhandled entities to be in logic first.
                    notRandomised.Remove(nextEntity);
                    EntityHandler.AddToLogic(nextEntity);
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
        }
        
        /// <summary>
        /// Add the prerequisites of the given entity to the priority queue.
        /// </summary>
        public void AddPrerequisitesAsPriority(LogicEntity entity)
        {
            List<TechType> newPriorities = new List<TechType>();
            newPriorities.AddRange(entity.Prerequisites ?? Enumerable.Empty<TechType>());
            newPriorities.AddRange(entity.Blueprint?.Fragments ?? Enumerable.Empty<TechType>());
            newPriorities.AddRange(entity.Blueprint?.UnlockConditions ?? Enumerable.Empty<TechType>());
            
            // Insert any prerequisites at the front of the queue.
            foreach (TechType techType in newPriorities)
            {
                LogicEntity prereq = EntityHandler.GetEntity(techType);
                if (!HasRandomised(prereq))
                {
                    _priorityEntities.Insert(0, prereq);
                    // Ensure that the prerequisites' requirements are also fulfilled.
                    AddPrerequisitesAsPriority(prereq);
                }
            }
        }

        /// <summary>
        /// Add one or more entities to prioritise on the next main loop cycle.
        /// </summary>
        public void AddPriorityEntities(IEnumerable<LogicEntity> entities)
        {
            foreach (LogicEntity entity in entities ?? Enumerable.Empty<LogicEntity>())
            {
                if (!_priorityEntities.Contains(entity))
                    _priorityEntities.Add(entity);
            }
        }

        /// <summary>
        /// Apply any changes the randomiser has decided on to the game. Also used for re-applying a saved state.
        /// </summary>
        /// <exception cref="InvalidDataException">Raised if the serializer is null.</exception>
        internal void ApplyAllChanges()
        {
            if (_Serializer is null)
                throw new InvalidDataException("Cannot apply randomisation changes: Serializer is null!");
            
            // Load changes stored in the serializer.
            foreach (ILogicModule module in _modules)
            {
                module.ApplySerializedChanges(_Serializer);
            }

            // Load any changes that rely on harmony patches.
            EnableHarmony();
        }

        /// <summary>
        /// Get the next entity to be randomised, prioritising essential or elective ones.
        /// </summary>
        /// <returns>The next entity.</returns>
        private LogicEntity ChooseNextEntity(List<LogicEntity> notRandomised)
        {
            // Make sure the list of absolutely essential entities is exhausted first.
            LogicEntity next = null;
            if (_priorityEntities.Count > 0)
            {
                next = _priorityEntities[0];
                // Ensure that any priority entity's prerequisites are always done first.
                while (!next.CheckPrerequisitesFulfilled(this))
                {
                    _Log.Debug($"[Core] Adding prerequisites for {next} to priority queue.");
                    AddPrerequisitesAsPriority(next);
                    next = _priorityEntities[0];
                }
                _priorityEntities.RemoveAt(0);
                next.IsPriority = true;
            }
            next ??= Random.Choice(notRandomised);
            while (HasRandomised(next))
            {
                _Log.Debug($"[Core] Found duplicate entity in main loop, removing: {next}");
                notRandomised.Remove(next);
                next = Random.Choice(notRandomised);
            }

            // Invoke the associated event.
            EntityChosen?.Invoke(this, new EntityEventArgs(next));

            return next;
        }

        /// <summary>
        /// Enables all necessary harmony patches based on the randomisation state in the serialiser.
        /// </summary>
        private void EnableHarmony()
        {
            _harmony = new Harmony(Initialiser.GUID);
            foreach (ILogicModule module in _modules)
            {
                module.SetupHarmonyPatches(_harmony);
            }
            // Always apply bugfixes.
            _harmony.PatchAll(typeof(VanillaBugfixes));
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
        /// Add one or more entities to prioritise on the next main loop cycle at a specific list index. Low values
        /// are processed first.
        /// </summary>
        public void InsertPriorityEntities(int index, IEnumerable<LogicEntity> entities)
        {
            foreach (var entity in entities)
            {
                _priorityEntities.Insert(index, entity);
            }
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
            if (string.IsNullOrEmpty(Initialiser._SaveFile.Base64Save))
            {
                _Log.Debug("[Core] base64 seed is empty.");
                return false;
            }

            _Log.Debug("[Core] Trying to decode base64 string...");
            EntitySerializer serializer = EntitySerializer.FromBase64String(Initialiser._SaveFile.Base64Save);

            if (serializer?.SpawnDataDict is null || serializer.RecipeDict is null)
            {
                _Log.Error("[Core] base64 seed is invalid; could not deserialize.");
                return false;
            }
            _Serializer = serializer;

            // Re-enable all modules that were active when the save data was generated.
            foreach (Type module in _Serializer.EnabledModules ?? Enumerable.Empty<Type>())
            {
                Component component = gameObject.EnsureComponent(module);
                _modules.Add(component as ILogicModule);
            }
            
            _Log.Debug("[Core] Save data restored.");
            return true;
        }
        
        private IEnumerator WaitUntilFilesLoaded()
        {
            yield return new WaitUntil(() => _fileTasks.TrueForAll(task => task.IsCompleted));
            StartCoroutine(Utils.WrapCoroutine(Randomise(), Initialiser.FatalError));
        }
    }
}
