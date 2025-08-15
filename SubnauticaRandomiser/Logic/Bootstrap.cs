using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using BepInEx.Bootstrap;
using HootLib;
using Nautilus.Handlers;
using SubnauticaRandomiser.Configuration;
using SubnauticaRandomiser.Handlers;
using SubnauticaRandomiser.Interfaces;
using SubnauticaRandomiser.Logic.Modules;
using SubnauticaRandomiser.Logic.Modules.Recipes;
using SubnauticaRandomiser.Patches;
using SubnauticaRandomiser.Serialization;
using SubnauticaRandomiser.Serialization.Modules;
using UnityEngine;
using ILogHandler = HootLib.Interfaces.ILogHandler;
using Object = UnityEngine.Object;
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

            // Register the save data file. Doing this before the WaitScreen task guarantees it will be ready when
            // we need it.
            SaveData = SaveDataHandler.RegisterSaveDataCache<SaveData>();
            // Do setup for the current save game during the loading screen.
            WaitScreenHandler.RegisterEarlyAsyncLoadTask(Initialiser.NAME, Initialise, "Setting up.");
            // Undo all changes to the game when the user quits back to the main menu.
            Hooking.OnQuitToMainMenu += Teardown;
        }

        /// <summary>
        /// Prepare everything needed for a randomised game, be it randomising fresh or loading a saved game.
        /// </summary>
        private IEnumerator Initialise(WaitScreenHandler.WaitScreenTask task)
        {
            _log.Debug("Setting up central GameObject.");
            SetupGameObject();
            
            // If the save version is negative it is on the default value and has never been set, meaning the file
            // has never been saved and this is a fresh start.
            if (SaveData.SaveVersion < 0)
            {
                _log.Info("Starting new game, randomising...");
                yield return EnableModules(task);
                yield return InitSaveData(task);
                yield return LoadRandomisationInfoFiles(task);
                // Randomise the game and save the final state to the SaveData.
                yield return _coreLogic.Randomise(task, SaveData);
            }
            else
            {
                _log.Info("Loading saved game, restoring game state.");
                yield return EnableSavedModules(task);
            }
            // Using either the freshly generated or previously loaded state, apply it to the game.
            yield return SyncGameState(task);
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

        /// <summary>
        /// Enable all modules for a fresh start as deemed necessary by the config.
        /// </summary>
        private IEnumerator EnableModules(WaitScreenHandler.WaitScreenTask task)
        {
            _log.Debug("Enabling modules for fresh start.");
            task.Status = "Randomising - Registering modules";
            yield return null;
            
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
        /// Enable all modules that had been active in a previously saved game.
        /// </summary>
        private IEnumerator EnableSavedModules(WaitScreenHandler.WaitScreenTask task)
        {
            _log.Debug("Re-enabling modules specified in saved game.");
            task.Status = "Loading previously enabled modules";
            yield return null;
            foreach (Type module in SaveData.EnabledModules)
            {
                RegisterModule(module);
            }
        }

        /// <summary>
        /// Get every module's async tasks for loading their important files from disk and delay randomising until they
        /// have all completed.
        /// </summary>
        private IEnumerator LoadRandomisationInfoFiles(WaitScreenHandler.WaitScreenTask task)
        {
            task.Status = "Randomising - Loading info files";
            yield return null;
            
            // The entity handler loads a file with critical information on every entity. It is always required.
            _fileTasks.Add(_coreLogic.EntityHandler.ParseDataFileAsync(Initialiser._RecipeFile));
            foreach (ILogicModule module in Modules)
            {
                _fileTasks.AddRange(module.LoadFiles());
            }
            
            yield return new WaitUntil(() => _fileTasks.TrueForAll(fTask => fTask.IsCompleted));
        }

        /// <summary>
        /// Give every registered module a chance to set up its own save data.
        /// </summary>
        private IEnumerator InitSaveData(WaitScreenHandler.WaitScreenTask task)
        {
            task.Status = "Randomising - Initialising SaveData";
            yield return null;
            
            SaveData.SaveVersion = Initialiser.SaveVersion;
            foreach (ILogicModule module in Modules)
            {
                BaseModuleSaveData moduleData = module.SetupSaveData();
                if (moduleData != null)
                    SaveData.AddModuleData(moduleData);
            }
        }

        private IEnumerator SyncGameState(WaitScreenHandler.WaitScreenTask task)
        {
            task.Status = "Syncing game state with randomisation data";
            yield return null;
            
            _sync ??= new GameStateSynchroniser(Initialiser.GUID);
            _sync.SyncGameState(SaveData);
        }

        /// <summary>
        /// Undo all changes to the game and return to a blank slate, ready for the next seed.
        /// </summary>
        private void Teardown()
        {
            _log.Info("Returning to menu, undoing all modifications.");
            // Let all modules handle undoing their changes first.
            _sync.Teardown(SaveData);
            _modules.Clear();
            // Then destroy the central logic object in preparation for the next fresh save.
            _log.Debug("Destroying logic object.");
            Object.Destroy(_logicObject);
        }

        /// <summary>
        /// Get all active modules as a list of their types.
        /// </summary>
        public List<Type> GetActiveModuleTypes()
        {
            return _modules.Select(module => module.GetType()).ToList();
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

        /// <inheritdoc cref="RegisterModule{TLogicModule}"/>
        /// <exception cref="ArgumentException">Thrown if the provided type does not implement
        /// <see cref="ILogicModule"/>.</exception>
        public ILogicModule RegisterModule(Type moduleType)
        {
            Component component = _logicObject.EnsureComponent(moduleType);
            if (!(component is ILogicModule module))
                throw new ArgumentException("Tried to register type which does not implement "
                                            + $"{nameof(ILogicModule)}: {component.GetType()}");
            _modules.Add(module);
            return module;
        }
    }
}