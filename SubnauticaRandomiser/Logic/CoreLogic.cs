using System;
using System.Collections;
using System.Collections.Generic;
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

namespace SubnauticaRandomiser.Logic
{
    /// <summary>
    /// Acts as the core for handling all randomising logic in the mod while invoking vital events along the way.
    /// </summary>
    internal class CoreLogic
    {
        internal Config _Config { get; private set; }
        public EntityHandler EntityHandler { get; private set; }
        private IRandomHandler _rng { get; set; }
        
        private ILogHandler _log;
        private ProgressionManager _manager;
        private SpoilerLog _spoilerLog;
        
        private LogicMonitor _monitor;
        private Dictionary<Type, BaseLogicModule> _entityRandomisers = new Dictionary<Type, BaseLogicModule>();

        /// <summary>
        /// Invoked during the setup stage. Use this event to add LogicEntities to the main loop.
        /// </summary>
        public event EventHandler<CollectEntitiesEventArgs> EntityCollecting;

        /// <summary>
        /// Invoked once the main loop has successfully completed.
        /// </summary>
        public event EventHandler MainLoopCompleted;

        public CoreLogic(LogicMonitor monitor)
        {
            _monitor = monitor;
            
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
        /// Check whether the given TechType has already been randomised.
        /// </summary>
        /// <exception cref="ArgumentNullException">If the TechType does not have an associated LogicEntity.</exception>
        public bool HasRandomised(TechType techType)
        {
            return EntityHandler.IsInLogic(techType);
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
