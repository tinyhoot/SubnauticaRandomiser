using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using HarmonyLib;
using HootLib;
using Nautilus.Handlers;
using SubnauticaRandomiser.Configuration;
using SubnauticaRandomiser.Handlers;
using SubnauticaRandomiser.Logic.Modules;
using SubnauticaRandomiser.Logic.Modules.Recipes;
using SubnauticaRandomiser.Patches;
using SubnauticaRandomiser.Serialization;
using SubnauticaRandomiser.Serialization.Modules;
using UnityEngine;
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
        
        private CoreLogic _coreLogic;
        private LogicMonitor _monitor;
        private EntityManager _entityManager;
        private RegionManager _regionManager;
        private TravelDistanceManager _travelDistanceManager;
        private GameStateSynchroniser _sync;
        private readonly List<BaseLogicModule> _modules = new List<BaseLogicModule>();

        public ReadOnlyCollection<BaseLogicModule> Modules => _modules.AsReadOnly();

        public Bootstrap(Config config)
        {
            Main = this;
            _config = config;
        }

        public void RegisterHooks()
        {
            // Register the save data file. Doing this before the WaitScreen task guarantees it will be ready when
            // we need it.
            SaveData = SaveDataHandler.RegisterSaveDataCache<SaveData>();
            // Kick off the randomiser for real early during the loading screen.
            WaitScreenHandler.RegisterEarlyAsyncLoadTask(Initialiser.NAME, Initialise, "Setting up.");
            // Undo all changes to the game when the user quits back to the main menu.
            Hooking.OnQuitToMainMenu += Teardown;
        }

        /// <summary>
        /// Prepare everything needed for a randomised game, be it randomising fresh or loading a saved game.
        /// </summary>
        private IEnumerator Initialise(WaitScreenHandler.WaitScreenTask task)
        {
            var startTime = Time.realtimeSinceStartup;
            _monitor = new LogicMonitor();
            _coreLogic = new CoreLogic(_monitor);
            
            // If the save version is negative it is on the default value and has never been set, meaning the file
            // has never been saved and this is a fresh start.
            if (SaveData.SaveVersion < 0)
            {
                _log.Info("Starting new game, randomising...");
                _entityManager = new EntityManager();
                _regionManager = new RegionManager();
                _travelDistanceManager = new TravelDistanceManager(_config);
                yield return EnableModules(task);
                _log.Debug($"Modules - {Time.realtimeSinceStartup - startTime}");
                yield return InitSaveData(task);
                _log.Debug($"SaveData - {Time.realtimeSinceStartup - startTime}");
                yield return LoadRandomisationInfoFiles(task);
                _log.Debug($"InfoFiles - {Time.realtimeSinceStartup - startTime}");
                yield return BuildEntityRegionModel(task);
                _log.Debug($"EntityModel - {Time.realtimeSinceStartup - startTime}");
                yield return LetModulesSetup(task);
                _log.Debug($"ModuleSetup - {Time.realtimeSinceStartup - startTime}");
                yield return ValidateSetupStage(task);
                _log.Debug($"ValidateSetup - {Time.realtimeSinceStartup - startTime}");
                // Randomise the game and save the final state to the SaveData.
                yield return _coreLogic.Randomise(task, SaveData, _entityManager, _regionManager, _travelDistanceManager);
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
        /// Enable all modules for a fresh start as deemed necessary by the config.
        /// </summary>
        private IEnumerator EnableModules(WaitScreenHandler.WaitScreenTask task)
        {
            _log.Debug("Enabling modules for fresh start.");
            task.Status = "Randomising - Registering modules";
            yield return null;
            
            // TODO: Re-enable these modules once they have been ported to the new system.
            //
            if (_config.EnableLifepodModule.Value && !_config.SpawnPoint.Value.Equals("Vanilla"))
                RegisterModule<LifepodModule>();
            // if (_config.RandomiseDoorCodes.Value || _config.RandomiseSupplyBoxes.Value)
            //     RegisterModule<AuroraLogic>();
            // if (_config.RandomiseDataboxes.Value)
            //     RegisterModule<DataboxLogic>();
            // if (_config.EnableFragmentModule.Value &&
            //     (_config.RandomiseFragments.Value || _config.RandomiseNumFragments.Value
            //                                       || _config.RandomiseDuplicateScans.Value))
            // {
            //     RegisterModule<FragmentLogic>();
            //     RegisterModule<EntitySlotsTracker>();
            // }
            //
            if (_config.EnableRecipeModule.Value && _config.RandomiseRecipes.Value)
            {
                // RegisterModule<RawMaterialLogic>();
                RegisterModule<RecipeModule>();
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
            foreach (Type moduleType in SaveData.EnabledModules)
            {
                RegisterModule(moduleType);
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
            
            var fileTasks = new List<Task>();
            // Start loading these now so they can complete in the background.
            foreach (BaseLogicModule module in Modules)
            {
                fileTasks.AddRange(module.LoadFilesAsync());
            }
            
            // These files are always required, as they form the backbone of the entity-region model.
            yield return new RushedCoroutine(_entityManager.ParseEntitiesFromDiskAsync(), 1f / 30f).Advance();
            yield return new RushedCoroutine(_regionManager.ParseFromDiskAsync(), 1f / 30f).Advance();
            yield return _travelDistanceManager.LoadTravelDataFromDiskAsync(_entityManager);
            yield return new WaitUntil(() => fileTasks.TrueForAll(fTask => fTask.IsCompleted));
            // Ensure we don't continue and the user is notified if some data fails to load.
            foreach (var t in fileTasks.Where(t => t.IsFaulted))
            {
                _log.Error("Extra data required by a module failed to load.");
                throw t.Exception!;
            }
        }

        /// <summary>
        /// Link entities and regions together to build the full game model.
        /// </summary>
        private IEnumerator BuildEntityRegionModel(WaitScreenHandler.WaitScreenTask task)
        {
            task.Status = "Randomising - Linking entities";
            yield return null;
            
            yield return _entityManager.LinkEntities(_config);
            _regionManager.ReplaceReferences(_entityManager);
        }

        /// <summary>
        /// Give modules an opportunity to prepare and/or modify entity relationships before randomisation begins.
        /// </summary>
        private IEnumerator LetModulesSetup(WaitScreenHandler.WaitScreenTask task)
        {
            task.Status = "Randomising - Letting modules do individual setup";
            yield return null;
            
            _modules.ForEach(m => m.PrepareRandomisation(_entityManager));
        }

        /// <summary>
        /// Ensure that all the data gathered and all the models built for randomising are valid.
        /// </summary>
        private IEnumerator ValidateSetupStage(WaitScreenHandler.WaitScreenTask task)
        {
            task.Status = "Randomising - Validating setup data";
            yield return null;
            
            Validator.ValidateEntityReferenceLinking(_entityManager.GetAllEntities());
            Validator.ValidateRegionLinking(_regionManager);
        }

        /// <summary>
        /// Give every registered module a chance to set up its own save data.
        /// </summary>
        private IEnumerator InitSaveData(WaitScreenHandler.WaitScreenTask task)
        {
            task.Status = "Randomising - Initialising SaveData";
            yield return null;
            
            SaveData.SaveVersion = Initialiser.SaveVersion;
            foreach (var module in Modules)
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
            _coreLogic = null;
            _monitor = null;
            _entityManager = null;
            _regionManager = null;
            _travelDistanceManager = null;
        }

        /// <summary>
        /// Get all active modules as a list of their types.
        /// </summary>
        public List<Type> GetActiveModuleTypes()
        {
            return _modules.Select(module => module.GetType()).ToList();
        }

        /// <summary>
        /// Register a module for use with the randomiser. Must be a subclass of <see cref="BaseLogicModule"/> with a
        /// parameterless constructor.
        /// </summary>
        public void RegisterModule<T>() where T : BaseLogicModule, new()
        {
            var module = Activator.CreateInstance(typeof(T)) as BaseLogicModule;
            InternalRegisterModule(module);
        }

        /// <inheritdoc cref="RegisterModule{T}"/>
        public void RegisterModule(Type type)
        {
            if (!typeof(BaseLogicModule).IsAssignableFrom(type))
                throw new ArgumentException($"Provided module type must inherit from {nameof(BaseLogicModule)}!");
            if (AccessTools.Constructor(type) is null)
                throw new ArgumentException("Provided module type must have a public parameterless constructor!");
            
            var module = Activator.CreateInstance(type) as BaseLogicModule;
            InternalRegisterModule(module);
        }

        private void InternalRegisterModule(BaseLogicModule module)
        {
            _coreLogic.RegisterEntityHandler(module.HandledEntityType, module);
            module.OnRegisterModule(_config, PrefixLogHandler.Get(module.LogPrefix), _monitor);
            _modules.Add(module);
        }
    }
}