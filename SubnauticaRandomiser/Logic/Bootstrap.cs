using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using BepInEx.Bootstrap;
using HootLib;
using Nautilus.Handlers;
using Nautilus.Json;
using SubnauticaRandomiser.Configuration;
using SubnauticaRandomiser.Handlers;
using SubnauticaRandomiser.Interfaces;
using SubnauticaRandomiser.Logic.Modules;
using SubnauticaRandomiser.Logic.Modules.Recipes;
using SubnauticaRandomiser.Serialization;
using SubnauticaRandomiser.Serialization.Modules;
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
        internal static SaveData SaveData;
        
        private Config _config;
        private ILogHandler _log = PrefixLogHandler.Get("[Bootstrap]");

        private GameObject _logicObject;
        private CoreLogic _coreLogic;
        private GameStateSynchroniser _sync;
        private readonly List<Task> _fileTasks = new List<Task>();
        private readonly List<ILogicModule> _modules = new List<ILogicModule>();

        public ReadOnlyCollection<ILogicModule> Modules => _modules.AsReadOnly();

        public Bootstrap(Config config)
        {
            Main = this;
            _config = config;

            // Register the save data file. Whether empty or populated, it will kick things off once loaded.
            SaveData = SaveDataHandler.RegisterSaveDataCache<SaveData>();
            SaveData.OnFinishedLoading += OnSaveDataLoaded;
        }

        /// <summary>
        /// Once the save data file has loaded, everything else can kick into gear.
        /// </summary>
        private void OnSaveDataLoaded(object sender, JsonFileEventArgs args)
        {
            Initialise();
        }

        /// <summary>
        /// Prepare everything needed for a randomised game, be it randomising fresh or loading a saved game.
        /// </summary>
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
            InitSaveData();
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
        /// Give every registered module a chance to set up its own save data.
        /// </summary>
        private void InitSaveData()
        {
            foreach (ILogicModule module in Modules)
            {
                BaseModuleSaveData moduleData = module.SetupSaveData();
                if (moduleData != null)
                    SaveData.AddModuleData(moduleData);
            }
        }

        /// <summary>
        /// Wait until all data files have finished loading and then hand off to the core logic.
        /// </summary>
        private IEnumerator WaitUntilFilesLoaded()
        {
            yield return new WaitUntil(() => _fileTasks.TrueForAll(task => task.IsCompleted));
            _coreLogic.Initialise(SaveData);
        }

        public void SyncGameState()
        {
            _sync ??= new GameStateSynchroniser(Initialiser.GUID);
            _sync.SyncGameState(SaveData);
        }

        /// <summary>
        /// Get all active modules as a list of their types.
        /// </summary>
        public List<Type> GetActiveModuleTypes()
        {
            return _modules.Select(module => module.GetType()).ToList();
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
        /// Register a component module for use with the randomiser.
        /// </summary>
        public TLogicModule RegisterModule<TLogicModule>() where TLogicModule : MonoBehaviour, ILogicModule
        {
            TLogicModule component = _logicObject.EnsureComponent<TLogicModule>();
            _modules.Add(component);
            return component;
        }
    }
}