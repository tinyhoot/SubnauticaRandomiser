using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using BepInEx;
using BepInEx.Configuration;
using Nautilus.Handlers;
using SubnauticaRandomiser.Configuration;
using SubnauticaRandomiser.Handlers;
using SubnauticaRandomiser.Logic;
using UnityEngine;
using ILogHandler = SubnauticaRandomiser.Interfaces.ILogHandler;

[assembly:InternalsVisibleTo("Tests")]
namespace SubnauticaRandomiser
{
    [BepInPlugin(GUID, NAME, VERSION)]
    [BepInDependency("com.snmodding.nautilus", "1.0")]
    internal class Initialiser : BaseUnityPlugin
    {
        public const string GUID = "com.github.tinyhoot.SubnauticaRandomiser";
        public const string NAME = "Subnautica Randomiser";
        public const string VERSION = "0.10.1";
        
        // Files and structure.
        internal static string _ModDirectory;
        internal static Config _Config;
        public const string _AlternateStartFile = "alternateStarts.csv";
        public const string _BiomeFile = "biomeSlots.csv";
        public const string _RecipeFile = "recipeInformation.csv";
        public const string _WreckageFile = "wreckInformation.csv";
        internal const string _ExpectedRecipeMD5 = "11cc2c8e44db4473c6e0d196b869d582";
        internal const int _ExpectedSaveVersion = 6;
        private static readonly Dictionary<int, string> s_versionDict = new Dictionary<int, string>
        {
            [1] = "v0.5.1",
            [2] = "v0.6.1",
            [3] = "v0.7.0",
            [4] = "v0.8.2",
            [5] = "v0.9.2",
            [_ExpectedSaveVersion] = "v" + VERSION
        };

        internal static ILogHandler _Log;
        internal GameObject _LogicObject;
        private CoreLogic _coreLogic;
        internal static Initialiser _Main;

        private void Awake()
        {
            _Main = this;
            _Log = new LogHandler();
            _Log.Info($"{NAME} v{VERSION} starting up!");

            // Register options menu.
            _ModDirectory = GetModDirectory();
            var file = new ConfigFile(Path.Combine(Paths.ConfigPath, $"{NAME.Replace(" ", string.Empty)}.cfg"), true, Info.Metadata);
            _Config = new Config(file);
            _Config.RegisterOptions();
            var modOptions = new ConfigModOptions(NAME, _Config);
            OptionsPanelHandler.RegisterModOptions(modOptions);
            _Log.Debug("Registered options menu.");

            // Ensure the user did not update into a save incompatibility, and abort if they did to preserve a prior
            // version's state.
            if (!CheckSaveCompatibility())
                return;
            
            // Register console commands.
            CommandHandler cmd = gameObject.AddComponent<CommandHandler>();
            cmd.RegisterCommands();

            // Setup the logic and restore from disk if possible.
            SetupGameObject();
            RestoreSavedState();

            _Log.Info("Finished loading.");
        }

        private void Start()
        {
            // Randomise, but only if no existing state was loaded from disk.
            if (_coreLogic != null && CoreLogic._Serializer is null)
                Randomise();
        }

        /// <summary>
        /// Ensure the user did not update into a save incompatibility.
        /// </summary>
        private bool CheckSaveCompatibility()
        {
            if (_Config.SaveVersion.Value == _ExpectedSaveVersion)
                return true;
            
            s_versionDict.TryGetValue(_Config.SaveVersion.Value, out string version);
            if (string.IsNullOrEmpty(version))
                version = "unknown or corrupted.";

            _Log.InGameMessage("It seems you updated Subnautica Randomiser. This version is incompatible with your previous savegame.", true);
            _Log.InGameMessage("The last supported version for your savegame is " + version, true);
            _Log.InGameMessage("To protect your previous savegame, no changes to the game have been made.", true);
            _Log.InGameMessage("If you wish to continue anyway, randomise again in the options menu or delete your config.json", true);
            return false;
        }

        /// <summary>
        /// Coroutines make it impossible (or at least very annoying) to catch exceptions with a traditional try-catch
        /// block. Instead, use this as a central callback function for any part of the code that needs it.
        /// </summary>
        /// <param name="exception"></param>
        /// <exception cref="Exception"></exception>
        internal static void FatalError(Exception exception)
        {
            _Log.InGameMessage($"{exception.GetType().Name.ToUpper()}: Something went wrong. Please report this error "
                               + "with the config.json from your mod folder on Github or NexusMods.", true);
            _Log.Fatal($"{exception.GetType()}: {exception.Message}");
            _Log.Fatal(exception.StackTrace);
            // Ensure that the randomiser crashes completely if things go wrong this badly.
            throw exception;
        }
        
        /// <summary>
        /// Randomise the game, discarding any earlier randomisation data.
        /// </summary>
        private void Randomise()
        {
            // _Config.SanitiseConfigValues();
            _Config.SaveVersion.Value = _ExpectedSaveVersion;

            // Create a new seed if the current one is just a default
            if (_Config.Seed.Value == 0)
                _Config.Seed.Entry.Value = new RandomHandler().Next();

            // Randomise!
            try
            {
                _coreLogic.Run();
            }
            catch (Exception ex)
            {
                FatalError(ex);
            }
        }

        internal void RandomiseFromConfig()
        {
            // Delete whatever previous state there was.
            if (_LogicObject != null)
                Destroy(_LogicObject);
            
            SetupGameObject();
            Randomise();
        }
        
        /// <summary>
        /// Try to restore the game state from a previous session. If that fails, start fresh and randomise.
        /// </summary>
        private void RestoreSavedState()
        {
            // Try and restore a game state from disk.
            if (_Config.DebugForceRandomise.Value || !_coreLogic.TryRestoreSave())
            {
                if (_Config.DebugForceRandomise.Value)
                    _Log.Warn("Ignoring previous game state: Config forces fresh randomisation.");
                _Log.Warn("Could not load game state from disk.");
            }
            else
            {
                _coreLogic.ApplyAllChanges();
                _Log.Info("Successfully loaded game state from disk.");
            }
        }

        /// <summary>
        /// Initialise the GameObject that holds the randomisation logic components.
        /// </summary>
        private void SetupGameObject()
        {
            // Instantiating this automatically starts running the Awake() methods of all components.
            _LogicObject = new GameObject("Randomiser Logic", typeof(CoreLogic), typeof(ProgressionManager));
            // Set this plugin (or BepInEx) as the parent of the logic GameObject.
            _LogicObject.transform.parent = transform;
            _coreLogic = _LogicObject.GetComponent<CoreLogic>();
        }

        /// <summary>
        /// Get the installation directory of the mod.
        /// </summary>
        internal static string GetModDirectory()
        {
            return new FileInfo(Assembly.GetExecutingAssembly().Location).Directory?.FullName;
        }
    }
}
