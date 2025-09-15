using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HootLib;
using Nautilus.Handlers;
using SubnauticaRandomiser.Configuration;
using SubnauticaRandomiser.Handlers;
using SubnauticaRandomiser.Interfaces;
using SubnauticaRandomiser.Logic.LogicObjects;
using SubnauticaRandomiser.Objects.Enums;
using SubnauticaRandomiser.Objects.Events;
using SubnauticaRandomiser.Serialization;
using UnityEngine;
using ILogHandler = HootLib.Interfaces.ILogHandler;
using LogicEntity = SubnauticaRandomiser.Objects.LogicEntity;

namespace SubnauticaRandomiser.Logic
{
    /// <summary>
    /// Acts as the core for handling all randomising logic in the mod while invoking vital events along the way.
    /// </summary>
    [DisallowMultipleComponent]
    internal class CoreLogic : MonoBehaviour
    {
        public static CoreLogic Main;
        
        internal Config _Config { get; private set; }
        public EntityHandler EntityHandler { get; private set; }
        private IRandomHandler _rng { get; set; }
        
        private ILogHandler _log;
        private ProgressionManager _manager;
        private SpoilerLog _spoilerLog;
        
        private readonly Dictionary<EntityType, ILogicModule> _handlingModules = new Dictionary<EntityType, ILogicModule>();
        private List<LogicEntity> _priorityEntities;


        private LogicMonitor _monitor;
        private List<LogicObjects.LogicEntity> _entities;

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
            
            _priorityEntities = new List<LogicEntity>();
            
            _Config = Initialiser._Config;
            _log = PrefixLogHandler.Get("[Core]");
            EntityHandler = new EntityHandler();
            
            _manager = gameObject.EnsureComponent<ProgressionManager>();
            _spoilerLog = gameObject.EnsureComponent<SpoilerLog>();
        }
        
        /// <summary>
        /// Parse the current config settings into a numeric seed.
        /// </summary>
        private int GetSeedFromConfig()
        {
            // Ensure an empty seed is replaced with something random.
            if (string.IsNullOrEmpty(_Config.Seed.Value))
                return (int)(Time.realtimeSinceStartup * 1000f);
            if (int.TryParse(_Config.Seed.Value, out int seed))
                return seed;
            _log.Warn("Seed was non-numeric value, substituting current time.");
            return (int)(Time.realtimeSinceStartup * 1000f);
        }

        
        #region logic-rework

        internal void RandomiseNew(EntityManager entityManager, RegionManager regionManager)
        {
            // Create new sphere
            // Explore all regions and transitions as far as possible
            // Prune inward facing transitions (only keep ones to new regions) and keep a list of all transitions
            // Fill priority items
            // Fill regular items
            // Repeat
            
            // Initially, the list of unrandomised entities is just the list of all entities.
            _entities = entityManager.GetAllEntities();
            
            // Set up the context with vanilla information.
            var context = new RandomisationContext(regionManager.GetRegion("SafeShallows"));
            // If modules like randomised start need to change the context, they can do so through this event.
            _monitor.TriggerContextCreated(context);

            List<Sphere> spheres = new List<Sphere>();
            Sphere start = new Sphere(context);
            spheres.Add(start);

            // Keep going until every last entity has been randomised.
            while (_entities.Count > 0)
            {
                // Get the newest, outermost sphere.
                var sphere = spheres.Last();
                // Do fills
                // After every fill, check whether a transition lock can be unlocked. If yes, new sphere.
            }
        }

        #endregion logic-rework
        

        /// <summary>
        /// Once all datafiles have completed loading, start up the logic.
        /// Running this as a coroutine spaces the logic out over several frames, which prevents the game from
        /// locking up / freezing.
        /// </summary>
        internal IEnumerator Randomise(WaitScreenHandler.WaitScreenTask task, SaveData saveData)
        {
            _rng = new RandomHandler(GetSeedFromConfig());
            
            task.Status = "Randomising - Extras";
            yield return null;
            
            List<LogicEntity> mainEntities = Setup();
            RandomisePreLoop();
            
            // Force a new frame before the main loop.
            task.Status = "Randomising - Entities";
            yield return null;
            yield return Hootils.WrapCoroutine(RandomiseMainEntities(mainEntities), Initialiser.FatalError);
            
            task.Status = "Randomising - Saving state";
            yield return null;
            saveData.SetEnabledModules(Bootstrap.Main.GetActiveModuleTypes());
            saveData.Save();
            
            // This makes the loading screen longer than it needs to be but that's worth the tradeoff.
            task.Status = "Success!";
            yield return new WaitForSecondsRealtime(1f);
        }

        /// <summary>
        /// Trigger setup events and prepare all data for starting the randomisation process.
        /// </summary>
        private List<LogicEntity> Setup()
        {
            _log.Info("Setting up...");
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
            _log.Info("Randomising: Pre-loop content");
            PreLoopRandomising?.Invoke(this, EventArgs.Empty);
            foreach (ILogicModule module in Bootstrap.Main.Modules)
            {
                module.RandomiseOutOfLoop(_rng, Bootstrap.SaveData);
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
            _log.Info("Randomising: Entering main loop");
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
                    _log.InGameMessage("Failed to randomise entities: stuck in infinite loop!");
                    _log.Fatal("Encountered infinite loop, aborting!");
                    throw new TimeoutException("Encountered infinite loop while randomising!");
                }

                LogicEntity nextEntity = ChooseNextEntity(notRandomised);
                if (nextEntity is null)
                    continue;
                // Try to get a handler for this type of entity.
                ILogicModule handler = _handlingModules.GetOrDefault(nextEntity.EntityType, null);
                if (handler is null)
                {
                    _log.Warn($"Unhandled entity in main loop: {nextEntity.EntityType} {nextEntity}");
                    // Add the unhandled entity into logic as a stopgap solution, for cases where a prerequisite check
                    // would fail because it expects unhandled entities to be in logic first.
                    notRandomised.Remove(nextEntity);
                    EntityHandler.AddToLogic(nextEntity);
                    continue;
                }

                // Let the module handle randomisation and report back.
                bool success = handler.RandomiseEntity(_rng, ref nextEntity);
                if (success)
                {
                    notRandomised.Remove(nextEntity);
                    EntityHandler.AddToLogic(nextEntity);
                    EntityRandomised?.Invoke(this, new EntityEventArgs(nextEntity));
                    _manager.TriggerProgressionEvents(nextEntity);
                }
            }
            
            _log.Info($"Finished randomising within {circuitbreaker} cycles!");
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
                    _log.Debug($"Adding prerequisites for {next} to priority queue.");
                    AddPrerequisitesAsPriority(next);
                    next = _priorityEntities[0];
                }
                _priorityEntities.RemoveAt(0);
                next.IsPriority = true;
            }
            next ??= _rng.Choice(notRandomised);
            while (HasRandomised(next))
            {
                _log.Debug($"Found duplicate entity in main loop, removing: {next}");
                notRandomised.Remove(next);
                next = _rng.Choice(notRandomised);
            }

            // Invoke the associated event.
            EntityChosen?.Invoke(this, new EntityEventArgs(next));

            return next;
        }

        /// <summary>
        /// Gets the random number generator for the current seed.
        /// </summary>
        /// <exception cref="NullReferenceException">Thrown if the RNG is currently null.</exception>
        public IRandomHandler GetRNG()
        {
            if (_rng is null)
                throw new NullReferenceException("RNG is not ready!");
            return _rng;
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
        /// Register a logic module as a handler for a specific entity type. This will cause that handler's
        /// RandomiseEntity() method to be called whenever an entity of that type needs to be randomised.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown if a handler for the given type of entity already
        /// exists. There can be only one per type.</exception>
        public void RegisterEntityHandler(EntityType type, ILogicModule module)
        {
            if (_handlingModules.TryGetValue(type, out ILogicModule existingModule))
                throw new ArgumentException($"A handler for entity type '{type}' already exists: "
                                            + $"{existingModule.GetType()}");

            _handlingModules.Add(type, module);
        }
    }
}
