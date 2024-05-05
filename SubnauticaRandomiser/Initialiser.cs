using System;
using System.Runtime.ExceptionServices;
using BepInEx;
using HootLib;
using SubnauticaRandomiser.Configuration;
using SubnauticaRandomiser.Handlers;
using SubnauticaRandomiser.Logic;
using ILogHandler = HootLib.Interfaces.ILogHandler;

namespace SubnauticaRandomiser
{
    [BepInPlugin(GUID, NAME, VERSION)]
    [BepInDependency("com.snmodding.nautilus", "1.0")]
    internal class Initialiser : BaseUnityPlugin
    {
        public const string GUID = "com.github.tinyhoot.SubnauticaRandomiser";
        public const string NAME = "Subnautica Randomiser";
        public const string VERSION = "0.11.1";
        
        // Files and structure.
        internal static Config _Config;
        public const string _AlternateStartFile = "alternateStarts.csv";
        public const string _BiomeFile = "biomeSlots.csv";
        public const string _RecipeFile = "recipeInformation.csv";
        public const string _WreckageFile = "wreckInformation.csv";

        internal static ILogHandler _Log;

        private void Awake()
        {
            _Log = new HootLogger(NAME);
            _Log.Info($"{NAME} v{VERSION} starting up!");

            // Register options menu.
            _Config = new Config(Hootils.GetConfigFileName(NAME), Info.Metadata);
            _Config.Setup();
            _Config.CreateModMenu(NAME, transform);
            _Log.Debug("Registered options menu.");
            
            // Register console commands.
            CommandHandler cmd = gameObject.AddComponent<CommandHandler>();
            cmd.RegisterCommands();

            // Set up the bootstrap to be ready for randomising later on.
            Bootstrap bootstrap = new Bootstrap(_Config);
        }

        /// <summary>
        /// Coroutines make it impossible (or at least very annoying) to catch exceptions with a traditional try-catch
        /// block. Instead, use this as a central callback function for any part of the code that needs it.
        /// </summary>
        internal static void FatalError(Exception exception)
        {
            _Log.InGameMessage($"<color=#FF0000FF>{exception.GetType().Name.ToUpper()}:</color> Something went wrong. "
                               + $"Please report this error with the {Hootils.GetConfigFileName(NAME)} from your "
                               + $"BepInEx config folder on Github or NexusMods.", true);
            _Log.Fatal($"{exception.GetType()}: {exception.Message}");
            _Log.Fatal(exception.StackTrace);
            // Ensure that the randomiser crashes completely if things go wrong this badly.
            // Rethrowing in this way preserves the original stacktrace.
            ExceptionDispatchInfo.Capture(exception).Throw();
        }
        
        /// <summary>
        /// Randomise the game, discarding any earlier randomisation data.
        /// </summary>
        private void Randomise()
        {
            // Create a new seed if the current one is just a default
            if (_Config.Seed.Value == 0)
                _Config.Seed.Entry.Value = new RandomHandler().Next();

            // Randomise!
            try
            {
                Bootstrap bootstrap = new Bootstrap(_Config);
                bootstrap.Initialise();
            }
            catch (Exception ex)
            {
                FatalError(ex);
            }
        }

        internal void RandomiseFromConfig()
        {
            // Delete whatever previous state there was.
            // if (_LogicObject != null)
            //     Destroy(_LogicObject);
            
            // SetupGameObject();
            Randomise();
        }
        
        // /// <summary>
        // /// Try to restore the game state from a previous session. If that fails, start fresh and randomise.
        // /// </summary>
        // private void RestoreSavedState()
        // {
        //     // Try and restore a game state from disk.
        //     if (_Config.DebugForceRandomise.Value || !_coreLogic.TryRestoreSave())
        //     {
        //         if (_Config.DebugForceRandomise.Value)
        //             _Log.Warn("Ignoring previous game state: Config forces fresh randomisation.");
        //         _Log.Warn("Could not load game state from disk.");
        //     }
        //     else
        //     {
        //         _coreLogic.ApplyAllChanges();
        //         _Log.Info("Successfully loaded game state from disk.");
        //     }
        // }
    }
}
