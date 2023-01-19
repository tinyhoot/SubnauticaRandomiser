using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using BepInEx;
using HarmonyLib;
using SMLHelper.V2.Handlers;
using SubnauticaRandomiser.Handlers;
using SubnauticaRandomiser.Logic;
using SubnauticaRandomiser.Logic.Recipes;
using SubnauticaRandomiser.Patches;
using UnityEngine;
using ILogHandler = SubnauticaRandomiser.Interfaces.ILogHandler;

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
        internal static GameObject _Logic;

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

            // Apply randomisation.
            SetupGameState();

            _Log.Info("Finished loading.");
        }

        /// <summary>
        /// Randomise the game, discarding any earlier randomisation data.
        /// </summary>
        public static void Randomise()
        {
            _Serializer = null;
            _Config.SanitiseConfigValues();
            _Config.iSaveVersion = _ExpectedSaveVersion;

            // Parse all the necessary input files.
            var (alternateStarts, biomes, databoxes, materials) = ParseInputFiles();

            // Create a new seed if the current one is just a default
            RandomHandler random;
            if (_Config.iSeed == 0)
            {
                random = new RandomHandler();
                _Config.iSeed = random.Next();
            }
            random = new RandomHandler(_Config.iSeed);

            // Randomise!
            CoreLogic logic = new CoreLogic(random, _Config, _Log, materials, alternateStarts, biomes, databoxes);
            try
            {
                _Serializer = logic.Randomise();
            }
            catch (Exception ex)
            {
                _Log.InGameMessage("ERROR: Something went wrong. Please report this error with the config.json"
                                           + " from your mod folder on NexusMods.", true);
                _Log.Fatal($"{ex.GetType()}: {ex.Message}");
                
                // Ensure that the randomiser crashes completely if things go wrong this badly.
                throw;
            }
            
            ApplyAllChanges();
            _Log.Info("Randomisation successful!");

            SaveGameStateToDisk();
        }

        /// <summary>
        /// Apply all changes contained within the serialiser.
        /// </summary>
        /// <exception cref="InvalidDataException">If the serialiser is null or invalid.</exception>
        private static void ApplyAllChanges()
        {
            if (_Serializer is null)
                throw new InvalidDataException("Cannot apply randomisation changes: Serializer is null!");
            
            // Load recipe changes.
            if (_Serializer.RecipeDict?.Count > 0)
                RecipeLogic.ApplyMasterDict(_Serializer);
                
            // Load fragment changes.
            if (_Serializer.SpawnDataDict?.Count > 0 || _Serializer.NumFragmentsToUnlock?.Count > 0)
            {
                FragmentLogic.ApplyMasterDict(_Serializer);
                _Log.Info("Loaded fragment state.");
            }

            // Load any changes that rely on harmony patches.
            EnableHarmonyPatching();
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
        /// Enables all necessary harmony patches based on the randomisation state in the serialiser.
        /// </summary>
        private static void EnableHarmonyPatching()
        {
            Harmony harmony = new Harmony(GUID);
            
            // Alternate starting location.
            harmony.PatchAll(typeof(AlternateStart));
            
            // Patching key codes.
            if (_Serializer?.DoorKeyCodes?.Count > 0)
            {
                harmony.PatchAll(typeof(AuroraPatcher));
                harmony.PatchAll(typeof(LanguagePatcher));
            }

            // Swapping databoxes.
            if (_Serializer?.Databoxes?.Count > 0)
                harmony.PatchAll(typeof(DataboxPatcher));

            // Changing duplicate scan rewards.
            if (_Serializer?.FragmentMaterialYield?.Count > 0)
                harmony.PatchAll(typeof(FragmentPatcher));

            // Always apply bugfixes.
            harmony.PatchAll(typeof(VanillaBugfixes));
        }

        /// <summary>
        /// Attempt to deserialise a randomisation state from disk.
        /// </summary>
        /// <returns>The EntitySerializer as previously written to disk.</returns>
        /// <exception cref="InvalidDataException">Raised if the game state is corrupted in some way.</exception>
        private static EntitySerializer RestoreGameStateFromDisk()
        {
            if (string.IsNullOrEmpty(_Config.sBase64Seed))
            {
                throw new InvalidDataException("base64 seed is empty.");
            }

            _Log.Debug("Trying to decode base64 string...");
            EntitySerializer dictionary = EntitySerializer.FromBase64String(_Config.sBase64Seed);

            if (dictionary?.SpawnDataDict is null || dictionary.RecipeDict is null)
            {
                throw new InvalidDataException("base64 seed is invalid; could not deserialize Dictionary.");
            }

            return dictionary;
        }
        
        /// <summary>
        /// Serialise the current randomisation state to disk.
        /// </summary>
        private static void SaveGameStateToDisk()
        {
            if (_Serializer != null)
            {
                string base64 = _Serializer.ToBase64String();
                _Config.sBase64Seed = base64;
                _Config.Save();
                _Log.Debug("Saved game state to disk!");
            }
            else
            {
                _Log.Error("Could not save game state to disk: invalid data.");
            }
        }

        private void SetupGameObject()
        {
            // Instantiating this automatically starts running the Awake() methods of all components.
            _Logic = new GameObject("Randomiser Logic", typeof(CoreLogic));
            // Set this plugin (or BepInEx) as the parent of the logic GameObject.
            _Logic.transform.parent = transform;
        }

        /// <summary>
        /// Try to restore the game state from a previous session. If that fails, start fresh and randomise.
        /// </summary>
        private static void SetupGameState()
        {
            // Try and restore a game state from disk.
            try
            {
                _Serializer = RestoreGameStateFromDisk();
            }
            catch (Exception ex)
            {
                _Log.Warn("Could not load game state from disk.");
                _Log.Warn(ex.Message);
            }
            
            if (_Config.debug_forceRandomise || _Serializer is null)
            {
                if (_Config.debug_forceRandomise)
                    _Log.Warn("Ignoring previous game state: Config forces fresh randomisation.");
                Randomise();
            }
            else
            {
                ApplyAllChanges();
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
