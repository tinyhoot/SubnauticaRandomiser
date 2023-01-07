using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using BepInEx;
using HarmonyLib;
using SMLHelper.V2.Handlers;
using SubnauticaRandomiser.Interfaces;
using SubnauticaRandomiser.Logic;
using SubnauticaRandomiser.Logic.Recipes;
using SubnauticaRandomiser.Patches;
using SubnauticaRandomiser.Objects;
using SubnauticaRandomiser.Objects.Enums;
using SubnauticaRandomiser.Objects.Exceptions;

[assembly:InternalsVisibleTo("Tests")]
namespace SubnauticaRandomiser
{
    [BepInPlugin(GUID, NAME, VERSION)]
    [BepInDependency("com.ahk1221.smlhelper", "2.15")]
    internal class InitMod : BaseUnityPlugin
    {
        private const string GUID = "com.github.tinyhoot.SubnauticaRandomiser";
        public const string NAME = "Subnautica Randomiser";
        public const string VERSION = "0.9.2";
        
        // Files and structure.
        internal static string _ModDirectory;
        internal static RandomiserConfig _Config;
        private const string _AlternateStartFile = "alternateStarts.csv";
        private const string _BiomeFile = "biomeSlots.csv";
        private const string _RecipeFile = "recipeInformation.csv";
        private const string _WreckageFile = "wreckInformation.csv";
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

        // Everything the mod every modifies is stored in here.
        internal static EntitySerializer _Serializer;
        internal static ILogHandler _Log;
        private const bool _Debug_forceRandomise = false;
        
        private void Awake()
        {
            _Log = new LogHandler();
            _Log.Info($"{NAME} v{VERSION} starting up!");

            // Register options menu.
            _ModDirectory = GetSubnauticaRandomiserDirectory();
            _Config = OptionsPanelHandler.Main.RegisterModOptions<RandomiserConfig>();
            _Log.Debug("Registered options menu.");

            // Ensure the user did not update into a save incompatibility, and abort if they did to preserve a prior
            // version's state.
            if (!CheckSaveCompatibility())
                return;
            
            // Register console commands.
            CommandHandler cmd = gameObject.AddComponent<CommandHandler>();
            cmd.RegisterCommands();

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

            // Triple checking things here in case the save got corrupted somehow.
            if (!_Debug_forceRandomise && _Serializer != null)
            {
                ApplyAllChanges();
                _Log.Info("Successfully loaded game state from disk.");
            }
            else
            {
                if (_Debug_forceRandomise)
                    _Log.Warn("Set to forcibly re-randomise recipes.");
                else
                    _Log.Warn("Failed to load game state from disk: dictionary empty.");

                Randomise();
            }

            _Log.Info("Finished loading.");
        }

        /// <summary>
        /// Randomise the game, discarding any earlier randomisation data.
        /// </summary>
        internal static void Randomise()
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
        internal static void ApplyAllChanges()
        {
            if (_Serializer is null)
                throw new InvalidDataException("Cannot apply randomisation changes: MasterDict is null!");
            
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
        /// Parse all CSV files needed for randomisation.
        /// </summary>
        /// <returns>The parsed objects.</returns>
        /// <exception cref="ParsingException">Raised if a file could not be parsed.</exception>
        private static (Dictionary<EBiomeType, List<float[]>> starts, List<BiomeCollection> biomes, List<Databox>
            databoxes, List<LogicEntity> materials) ParseInputFiles()
        {
            var csvReader = new CSVReader(_Log);

            // Attempt to read and parse the CSV with all alternate starts.
            var alternateStarts = csvReader.ParseAlternateStartFile(_AlternateStartFile);
            if (alternateStarts is null)
            {
                _Log.Error("Failed to extract alternate start information from CSV.");
                throw new ParsingException("Failed to extract alternate start information: null.");
            }
            
            // Attempt to read and parse the CSV with all biome information.
            var biomes = csvReader.ParseBiomeFile(_BiomeFile);
            if (biomes is null)
            {
                _Log.Error("Failed to extract biome information from CSV.");
                throw new ParsingException("Failed to extract biome information: null");
            }

            // Attempt to read and parse the CSV with all recipe information.
            var materials = csvReader.ParseRecipeFile(_RecipeFile);
            if (materials is null)
            {
                _Log.Error("Failed to extract recipe information from CSV.");
                throw new ParsingException("Failed to extract recipe information: null");
            }

            // Attempt to read and parse the CSV with wreckages and databox info.
            var databoxes = csvReader.ParseWreckageFile(_WreckageFile);
            if (databoxes is null || databoxes.Count == 0)
            {
                _Log.Error("Failed to extract databox information from CSV.");
                throw new ParsingException("Failed to extract databox information: null");
            }

            return (alternateStarts, biomes, databoxes, materials);
        }

        /// <summary>
        /// Serialise the current randomisation state to disk.
        /// </summary>
        internal static void SaveGameStateToDisk()
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

        /// <summary>
        /// Attempt to deserialise a randomisation state from disk.
        /// </summary>
        /// <returns>The EntitySerializer as previously written to disk.</returns>
        /// <exception cref="InvalidDataException">Raised if the game state is corrupted in some way.</exception>
        internal static EntitySerializer RestoreGameStateFromDisk()
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
        /// Get the installation directory of the mod.
        /// </summary>
        internal static string GetSubnauticaRandomiserDirectory()
        {
            return new FileInfo(Assembly.GetExecutingAssembly().Location).Directory?.FullName;
        }

        /// <summary>
        /// Enables all necessary harmony patches based on the randomisation state in the serialiser.
        /// Must use manual patching since PatchAll() will not respect any config settings.
        /// </summary>
        private static void EnableHarmonyPatching()
        {
            Harmony harmony = new Harmony(GUID);
            
            // Alternate starting location.
            var original = AccessTools.Method(typeof(RandomStart), nameof(RandomStart.GetRandomStartPoint));
            var postfix = AccessTools.Method(typeof(AlternateStart), nameof(AlternateStart.OverrideStart));
            harmony.Patch(original, postfix: new HarmonyMethod(postfix));

            // Swapping databoxes.
            if (_Serializer?.Databoxes?.Count > 0)
            {
                original = AccessTools.Method(typeof(BlueprintHandTarget), nameof(BlueprintHandTarget.Start));
                var prefix = AccessTools.Method(typeof(DataboxPatcher), nameof(DataboxPatcher.PatchDatabox));
                harmony.Patch(original, prefix: new HarmonyMethod(prefix));
            }

            // Changing duplicate scan rewards.
            if (_Serializer?.FragmentMaterialYield?.Count > 0)
            {
                original = AccessTools.Method(typeof(PDAScanner), nameof(PDAScanner.Scan));
                var transpiler = AccessTools.Method(typeof(FragmentPatcher), nameof(FragmentPatcher.Transpiler));
                harmony.Patch(original, transpiler: new HarmonyMethod(transpiler));
            }
            
            // Always apply bugfixes.
            harmony.PatchAll(typeof(VanillaBugfixes));
        }
    }
}
