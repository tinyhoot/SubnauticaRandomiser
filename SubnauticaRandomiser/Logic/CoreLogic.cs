using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Nautilus.Handlers;
using SubnauticaRandomiser.Configuration;
using SubnauticaRandomiser.Handlers;
using SubnauticaRandomiser.Interfaces;
using SubnauticaRandomiser.Logic.LogicObjects;
using SubnauticaRandomiser.Logic.Modules;
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
    internal class CoreLogic
    {
        public static CoreLogic Main;
        
        internal Config _Config { get; private set; }
        public EntityHandler EntityHandler { get; private set; }
        private IRandomHandler _rng { get; set; }
        
        private ILogHandler _log;
        private ProgressionManager _manager;
        private SpoilerLog _spoilerLog;
        
        private List<LogicEntity> _priorityEntities;
        
        private LogicMonitor _monitor;
        private Dictionary<Type, BaseLogicModule> _entityRandomisers = new Dictionary<Type, BaseLogicModule>();

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

        public CoreLogic(LogicMonitor monitor)
        {
            Main = this;
            _monitor = monitor;
            
            _priorityEntities = new List<LogicEntity>();
            
            _Config = Initialiser._Config;
            _log = PrefixLogHandler.Get("[Core]");
            EntityHandler = new EntityHandler();
            
            // _manager = gameObject.EnsureComponent<ProgressionManager>();
            // _spoilerLog = gameObject.EnsureComponent<SpoilerLog>();
        }
        
        /// <summary>
        /// Once all datafiles have completed loading, start up the logic.
        /// Running this as a coroutine spaces the logic out over several frames, which prevents the game from
        /// locking up / freezing.
        /// </summary>
        internal IEnumerator Randomise(WaitScreenHandler.WaitScreenTask task, SaveData saveData, RandoContext context)
        {
            _rng = new RandomHandler(GetSeedFromConfig());
            
            // Set up the context with vanilla information.
            var startingState = new StartingState(context.RegionManager.GetRegion("SafeShallows"));
            // TODO: Replace with actual data once spawning-related modules are done.
            startingState.StartingEntities.AddRange(new []
            {
                context.EntityManager.Find<LogicInventoryItem>(TechType.Titanium),
                context.EntityManager.Find<LogicInventoryItem>(TechType.Copper),
                context.EntityManager.Find<LogicInventoryItem>(TechType.AcidMushroom),
            });
            // If modules like randomised start need to change the context, they can do so through this event.
            _monitor.TriggerStartingStateCreated(startingState);
            
            task.Status = "Randomising before entities";
            yield return null;
            yield return RandomisePreEntities(saveData);
            
            task.Status = "Randomising entities (this may take a while)";
            yield return null;
            yield return RandomiseEntities(saveData, context, startingState);
            
            task.Status = "Randomising after entities";
            yield return null;
            yield return RandomisePostEntities(saveData);

            task.Status = "Saving randomised data";
            yield return null;
            saveData.SetEnabledModules(Bootstrap.Main.GetActiveModuleTypes());
            saveData.Save();
            
            // This makes the loading screen longer than it needs to be but informing the user is worth it.
            task.Status = "Success!";
            yield return new WaitForSecondsRealtime(1f);
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

        private IEnumerator RandomisePreEntities(SaveData saveData)
        {
            foreach (var module in Bootstrap.Main.Modules)
            {
                module.PreEntityRandomisation(_rng, saveData);
                yield return null;
            }
        }

        private IEnumerator RandomiseEntities(SaveData saveData, RandoContext context, StartingState startingState)
        {
            // Set up the queue with every known entity.
            var queue = new EntityQueue(context.EntityManager.GetAllEntities(), _rng, context.PriorityRules, _monitor);
            
            // Set up the starting sphere.
            Sphere sphere = new Sphere(startingState, context.EntityManager, context.TravelManager);
            List<Sphere> spheres = new List<Sphere> { sphere };

            // Keep going until every last entity has been randomised.
            foreach (var entity in queue)
            {
                _log.Debug($"> Picked entity {entity}");
                if (entity.Sphere >= 0)
                {
                    _log.Debug($"Entity {entity} was already randomised, skipping.");
                    queue.RemoveCurrent();
                    continue;
                }
                
                if (!entity.Dependencies.TrueForAll(e => e.Sphere >= 0))
                {
                    // This isn't ready yet. Delay it before trying again.
                    queue.DelayCurrent();
                    _log.Debug("Dependencies were:");
                    foreach (var dep in entity.Dependencies)
                    {
                        _log.Debug($"- {dep}");
                    }
                    continue;
                }

                // Hand the entity off to one of the modules for randomising.
                if (_entityRandomisers.TryGetValue(entity.GetType(), out BaseLogicModule module))
                    module.RandomiseEntity(_rng, saveData, entity);
                else
                    _log.Debug($"Entity {entity} does not have a handler, skipping.");
                
                sphere.AddEntity(entity);
                _log.Debug($"{entity} randomised into sphere {sphere.Tier}");
                queue.RemoveCurrent();
                _monitor.TriggerEntityRandomised(entity);
                // TODO: Check whether all entities of this techtype have been done.
                // If not, check for any that have no handler.
                // Instantly complete any without a handler which have their dependencies fulfilled.
                // This helps e.g. RecipeModule for spawnable-->inventoryitem availability.
                
                // TODO: Assemble dynamic list of things that *can* cause progress, only update when that is rando'd.
                context.TravelManager.UpdateDepths(context.EntityManager);
                // After every fill, check whether a transition lock can be opened.
                if (sphere.TryUnlockEdges(out var newRegions))
                {
                    sphere = new Sphere(sphere, newRegions);
                    spheres.Add(sphere);
                    _log.Debug($"--- Entity {entity} unlocked new sphere tier {sphere.Tier} ---");
                    _monitor.TriggerSphereCreated(sphere);
                }

                yield return null;
            }
            _log.Info($"Finished randomising. Created {spheres.Count} spheres.");
        }
        
        private IEnumerator RandomisePostEntities(SaveData saveData)
        {
            foreach (var module in Bootstrap.Main.Modules)
            {
                module.PostEntityRandomisation(_rng, saveData);
                yield return null;
            }
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
                ILogicModule handler = null;
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
        /// <see cref="BaseLogicModule.RandomiseEntity"/> method to be called whenever an entity of that type needs to
        /// be randomised.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown if a handler for the given type of entity already
        /// exists. There can be only one per type.</exception>
        public void RegisterEntityHandler(Type type, BaseLogicModule module)
        {
            if (type != null)
                _entityRandomisers.Add(type, module);
        }
    }
}
