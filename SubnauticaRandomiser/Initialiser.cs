using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using BepInEx;
using SMLHelper.V2.Handlers;
using SubnauticaRandomiser.Handlers;
using SubnauticaRandomiser.Logic;
using UnityEngine;
using ILogHandler = SubnauticaRandomiser.Interfaces.ILogHandler;
using Random = UnityEngine.Random;

[assembly:InternalsVisibleTo("Tests")]
namespace SubnauticaRandomiser
{
    [BepInPlugin(GUID, NAME, VERSION)]
    [BepInDependency("com.ahk1221.smlhelper", "2.15")]
    internal class Initialiser : BaseUnityPlugin
    {
        public const string GUID = "com.github.tinyhoot.SubnauticaRandomiser";
        public const string NAME = "Subnautica Randomiser";
        public const string VERSION = "0.9.2";
        
        // Files and structure.
        internal static string _ModDirectory;
        internal static RandomiserConfig _Config;
        public const string _AlternateStartFile = "alternateStarts.csv";
        public const string _BiomeFile = "biomeSlots.csv";
        public const string _RecipeFile = "recipeInformation.csv";
        public const string _WreckageFile = "wreckInformation.csv";
        internal const string _ExpectedRecipeMD5 = "11cc2c8e44db4473c6e0d196b869d582";
        internal const int _ExpectedSaveVersion = 5;
        private static readonly Dictionary<int, string> s_versionDict = new Dictionary<int, string>
        {
            [1] = "v0.5.1",
            [2] = "v0.6.1",
            [3] = "v0.7.0",
            [4] = "v0.8.2",
            [_ExpectedSaveVersion] = "v" + VERSION
        };

        // Everything the mod ever modifies is stored in here.
        internal static EntitySerializer _Serializer;
        internal static ILogHandler _Log;
        internal static GameObject _LogicObject;
        private static CoreLogic _coreLogic;

        private void Awake()
        {
            _Log = new LogHandler();
            _Log.Info($"{NAME} v{VERSION} starting up!");

            // Register options menu.
            _ModDirectory = GetModDirectory();
            _Config = OptionsPanelHandler.Main.RegisterModOptions<RandomiserConfig>();
            _Log.Debug("Registered options menu.");

            // Ensure the user did not update into a save incompatibility, and abort if they did to preserve a prior
            // version's state.
            if (!CheckSaveCompatibility())
                return;
            
            // Register console commands.
            CommandHandler cmd = gameObject.AddComponent<CommandHandler>();
            cmd.RegisterCommands();

            // Setup the logic and restore from disk if possible.
            SetupGameState();

            _Log.Info("Finished loading.");
        }

        private void Start()
        {
            // Randomise, but only if no existing state was loaded from disk.
            if (_Serializer is null)
                Randomise();
        }

        /// <summary>
        /// Randomise the game, discarding any earlier randomisation data.
        /// </summary>
        public static void Randomise()
        {
            _Serializer = null;
            _Config.SanitiseConfigValues();
            _Config.iSaveVersion = _ExpectedSaveVersion;

            // Create a new seed if the current one is just a default
            if (_Config.iSeed == 0)
                _Config.iSeed = Random.Range(1, int.MaxValue);

            // Randomise!
            try
            {
                _coreLogic.Run();
            }
            catch (Exception ex)
            {
                _Log.InGameMessage("ERROR: Something went wrong. Please report this error with the config.json"
                                           + " from your mod folder on NexusMods.", true);
                _Log.Fatal($"{ex.GetType()}: {ex.Message}");
                
                // Ensure that the randomiser crashes completely if things go wrong this badly.
                throw;
            }
            
            // ApplyAllChanges();
            // _Log.Info("Randomisation successful!");
            //
            // SaveGameStateToDisk();
        }

        /// <summary>
        /// Ensure the user did not update into a save incompatibility.
        /// </summary>
        private static bool CheckSaveCompatibility()
        {
            if (_Config.iSaveVersion == _ExpectedSaveVersion)
                return true;
            
            s_versionDict.TryGetValue(_Config.iSaveVersion, out string version);
            if (string.IsNullOrEmpty(version))
                version = "unknown or corrupted.";

            _Log.InGameMessage("It seems you updated Subnautica Randomiser. This version is incompatible with your previous savegame.", true);
            _Log.InGameMessage("The last supported version for your savegame is " + version, true);
            _Log.InGameMessage("To protect your previous savegame, no changes to the game have been made.", true);
            _Log.InGameMessage("If you wish to continue anyway, randomise again in the options menu or delete your config.json", true);
            return false;
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
        /// Try to restore the game state from a previous session. If that fails, start fresh and randomise.
        /// </summary>
        private void SetupGameState()
        {
            SetupGameObject();
            // Try and restore a game state from disk.
            if (_Config.debug_forceRandomise || !_coreLogic.TryRestoreSave())
            {
                if (_Config.debug_forceRandomise)
                    _Log.Warn("Ignoring previous game state: Config forces fresh randomisation.");
                _Log.Warn("Could not load game state from disk.");
            }
            else
            {
                _Serializer = _coreLogic.Serializer;
                _coreLogic.ApplyAllChanges();
                _Log.Info("Successfully loaded game state from disk.");
            }
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
