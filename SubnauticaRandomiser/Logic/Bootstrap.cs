using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using BepInEx.Bootstrap;
using HootLib;
using SubnauticaRandomiser.Configuration;
using SubnauticaRandomiser.Handlers;
using SubnauticaRandomiser.Interfaces;
using SubnauticaRandomiser.Logic.Modules;
using SubnauticaRandomiser.Logic.Modules.Recipes;
using SubnauticaRandomiser.Objects.Enums;
using UnityEngine;
using UWE;
using ILogHandler = HootLib.Interfaces.ILogHandler;
using Task = System.Threading.Tasks.Task;

namespace SubnauticaRandomiser.Logic
{
    /// <summary>
    /// Responsible for bootstrapping the randomisation process by setting up all required modules and data to the point
    /// where the core logic can begin.
    /// </summary>
    internal class Bootstrap
    {
        public static Bootstrap Main;
        
        private Config _config;
        private ILogHandler _log = PrefixLogHandler.Get("[Bootstrap]");

        private GameObject _logicObject;
        private CoreLogic _coreLogic;
        private GameStateSynchroniser _sync;
        private readonly List<Task> _fileTasks = new List<Task>();
        private readonly List<ILogicModule> _modules = new List<ILogicModule>();
        private readonly Dictionary<EntityType, ILogicModule> _handlingModules = new Dictionary<EntityType, ILogicModule>();

        public ReadOnlyCollection<ILogicModule> Modules => _modules.AsReadOnly();

        public Bootstrap(Config config)
        {
            Main = this;
            _config = config;
        }

        public void Initialise()
        {
            // Order of operations
            // - Setup GO and core
            _log.Debug("Setting up central GameObject.");
            SetupGameObject();
            // - TODO: Decide if randomising or loading save
            // - Enable modules
            _log.Debug("Enabling modules.");
            EnableModules();
            // IF randomising
            //   - Let files load
            // Wait and periodically check whether all file loading has completed. Only continue once that is done.
            CoroutineHost.StartCoroutine(WaitUntilFilesLoaded());
            //   - Hand off to core
            // ENDIF
            // - Hand off to finaliser
            // TODO: INCOMPLETE
            // SyncGameState();
        }

        /// <summary>
        /// Initialise the GameObject that holds the randomisation logic components.
        /// </summary>
        private void SetupGameObject()
        {
            _logicObject = new GameObject("Randomiser Logic");
            // Set the BepInEx manager object as the parent of the logic GameObject.
            _logicObject.transform.SetParent(Chainloader.ManagerObject.transform, false);
            _coreLogic = _logicObject.AddComponent<CoreLogic>();
        }

        private void EnableModules()
        {
            if (_config.EnableAlternateStartModule.Value && !_config.SpawnPoint.Value.Equals("Vanilla"))
                RegisterModule<AlternateStartLogic>();
            if (_config.RandomiseDoorCodes.Value || _config.RandomiseSupplyBoxes.Value)
                RegisterModule<AuroraLogic>();
            if (_config.RandomiseDataboxes.Value)
                RegisterModule<DataboxLogic>();
            if (_config.EnableFragmentModule.Value &&
                (_config.RandomiseFragments.Value || _config.RandomiseNumFragments.Value
                                                  || _config.RandomiseDuplicateScans.Value))
                RegisterModule<FragmentLogic>();
            if (_config.EnableRecipeModule.Value && _config.RandomiseRecipes.Value)
            {
                RegisterModule<RawMaterialLogic>();
                RegisterModule<RecipeLogic>();
            }
            _log.Debug($"Enabled {Modules.Count} modules: {Modules.ElementsToString()}");
        }

        /// <summary>
        /// Wait until all data files have finished loading and then hand off to the core logic.
        /// </summary>
        private IEnumerator WaitUntilFilesLoaded()
        {
            yield return new WaitUntil(() => _fileTasks.TrueForAll(task => task.IsCompleted));
            _coreLogic.Initialise();
        }

        public void SyncGameState(EntitySerializer serializer)
        {
            _sync ??= new GameStateSynchroniser(Initialiser.GUID);
            _sync.SyncGameState(serializer);
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
        public TLogicModule RegisterModule<TLogicModule>() where TLogicModule : MonoBehaviour, ILogicModule
        {
            TLogicModule component = _logicObject.EnsureComponent<TLogicModule>();
            _modules.Add(component);
            return component;
        }

        /// <summary>
        /// Gets a previously registered handler for the given entity type. Can be null if no handler was registered.
        /// </summary>
        /// <returns>The registered handler.</returns>
        public ILogicModule GetEntityHandler(EntityType type)
        {
            return _handlingModules.GetOrDefault(type, null);
        }
    }
}