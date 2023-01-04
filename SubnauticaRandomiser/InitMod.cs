using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using BepInEx;
using HarmonyLib;
using SMLHelper.V2.Handlers;
using SubnauticaRandomiser.Logic;
using SubnauticaRandomiser.Logic.Recipes;
using SubnauticaRandomiser.Patches;
using SubnauticaRandomiser.Objects;
using SubnauticaRandomiser.Objects.Enums;
using SubnauticaRandomiser.Objects.Exceptions;

[assembly:InternalsVisibleTo("Tests")]
namespace SubnauticaRandomiser
{
    [BepInPlugin(GUID, "Subnautica Randomiser", VERSION)]
    [BepInDependency("com.ahk1221.smlhelper", "2.15")]
    public class InitMod : BaseUnityPlugin
    {
        public const string GUID = "com.github.tinyhoot.SubnauticaRandomiser";
        public const string VERSION = "0.9.2";
        
        internal static string s_modDirectory;
        internal static RandomiserConfig s_config;
        internal const string _AlternateStartFile = "alternateStarts.csv";
        internal const string _BiomeFile = "biomeSlots.csv";
        internal const string _RecipeFile = "recipeInformation.csv";
        internal const string _WreckageFile = "wreckInformation.csv";
        internal const string _ExpectedRecipeMD5 = "11cc2c8e44db4473c6e0d196b869d582";
        internal const int _ExpectedSaveVersion = 5;
        internal static readonly Dictionary<int, string> s_versionDict = new Dictionary<int, string>
        {
            [1] = "v0.5.1",
            [2] = "v0.6.1",
            [3] = "v0.7.0",
            [4] = "v0.8.2",
            [5] = "v" + VERSION
        };

        // The master list of everything that is modified by the mod.
        internal static EntitySerializer s_masterDict;
        internal static LogHandler _log;
        private const bool _Debug_forceRandomise = false;
        
        private void Awake()
        {
            _log = new LogHandler();
            _log.Info($"Subnautica Randomiser v{VERSION} starting up!");

            // Register options menu.
            s_modDirectory = GetSubnauticaRandomiserDirectory();
            s_config = OptionsPanelHandler.Main.RegisterModOptions<RandomiserConfig>();
            _log.Debug("Registered options menu.");

            // Ensure the user did not update into a save incompatibility, and abort if they did to preserve a prior
            // version's state.
            if (!CheckSaveCompatibility())
                return;
            
            // Register console commands.
            var cmd = gameObject.AddComponent<CommandHandler>();
            cmd.RegisterCommands();

            // Try and restore a game state from disk.
            try
            {
                s_masterDict = RestoreGameStateFromDisk();
            }
            catch (Exception ex)
            {
                _log.Warn("Could not load game state from disk.");
                _log.Warn(ex.Message);
            }

            // Triple checking things here in case the save got corrupted somehow.
            if (!_Debug_forceRandomise && s_masterDict != null)
            {
                ApplyAllChanges();
                _log.Info("Successfully loaded game state from disk.");
            }
            else
            {
                if (_Debug_forceRandomise)
                    _log.Warn("Set to forcibly re-randomise recipes.");
                else
                    _log.Warn("Failed to load game state from disk: dictionary empty.");

                Randomise();
            }

            _log.Info("Finished loading.");
        }

        /// <summary>
        /// Randomise the game, discarding any earlier randomisation data.
        /// </summary>
        internal static void Randomise()
        {
            s_masterDict = null;
            s_config.SanitiseConfigValues();
            s_config.iSaveVersion = _ExpectedSaveVersion;

            // Parse all the necessary input files.
            var (alternateStarts, biomes, databoxes, materials) = ParseInputFiles();

            // Create a new seed if the current one is just a default
            RandomHandler random;
            if (s_config.iSeed == 0)
            {
                random = new RandomHandler();
                s_config.iSeed = random.Next();
            }
            random = new RandomHandler(s_config.iSeed);

            // Randomise!
            CoreLogic logic = new CoreLogic(random, s_config, _log, materials, alternateStarts, biomes, databoxes);
            try
            {
                s_masterDict = logic.Randomise();
            }
            catch (Exception ex)
            {
                _log.InGameMessage("ERROR: Something went wrong. Please report this error with the config.json"
                                           + " from your mod folder on NexusMods.", true);
                _log.Fatal($"{ex.GetType()}: {ex.Message}");
                
                // Ensure that the randomiser crashes completely if things go wrong this badly.
                throw;
            }
            
            ApplyAllChanges();
            _log.Info("Randomisation successful!");

            SaveGameStateToDisk();
        }

        /// <summary>
        /// Apply all changes contained within the serialiser.
        /// </summary>
        /// <exception cref="InvalidDataException">If the serialiser is null or invalid.</exception>
        internal static void ApplyAllChanges()
        {
            if (s_masterDict is null)
                throw new InvalidDataException("Cannot apply randomisation changes: MasterDict is null!");
            
            // Load recipe changes.
            if (s_masterDict.RecipeDict?.Count > 0)
                RecipeLogic.ApplyMasterDict(s_masterDict);
                
            // Load fragment changes.
            if (s_masterDict.SpawnDataDict?.Count > 0 || s_masterDict.NumFragmentsToUnlock?.Count > 0)
            {
                FragmentLogic.ApplyMasterDict(s_masterDict);
                _log.Info("Loaded fragment state.");
            }

            // Load any changes that rely on harmony patches.
            EnableHarmonyPatching();
        }

        /// <summary>
        /// Ensure the user did not update into a save incompatibility.
        /// </summary>
        private static bool CheckSaveCompatibility()
        {
            if (s_config.iSaveVersion == _ExpectedSaveVersion)
                return true;
            
            s_versionDict.TryGetValue(s_config.iSaveVersion, out string version);
            if (string.IsNullOrEmpty(version))
                version = "unknown or corrupted.";

            _log.InGameMessage("It seems you updated Subnautica Randomiser. This version is incompatible with your previous savegame.", true);
            _log.InGameMessage("The last supported version for your savegame is " + version, true);
            _log.InGameMessage("To protect your previous savegame, no changes to the game have been made.", true);
            _log.InGameMessage("If you wish to continue anyway, randomise again in the options menu or delete your config.json", true);
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
            var csvReader = new CSVReader(_log);

            // Attempt to read and parse the CSV with all alternate starts.
            var alternateStarts = csvReader.ParseAlternateStartFile(_AlternateStartFile);
            if (alternateStarts is null)
            {
                _log.Error("Failed to extract alternate start information from CSV.");
                throw new ParsingException("Failed to extract alternate start information: null.");
            }
            
            // Attempt to read and parse the CSV with all biome information.
            var biomes = csvReader.ParseBiomeFile(_BiomeFile);
            if (biomes is null)
            {
                _log.Error("Failed to extract biome information from CSV.");
                throw new ParsingException("Failed to extract biome information: null");
            }

            // Attempt to read and parse the CSV with all recipe information.
            var materials = csvReader.ParseRecipeFile(_RecipeFile);
            if (materials is null)
            {
                _log.Error("Failed to extract recipe information from CSV.");
                throw new ParsingException("Failed to extract recipe information: null");
            }

            // Attempt to read and parse the CSV with wreckages and databox info.
            var databoxes = csvReader.ParseWreckageFile(_WreckageFile);
            if (databoxes is null || databoxes.Count == 0)
            {
                _log.Error("Failed to extract databox information from CSV.");
                throw new ParsingException("Failed to extract databox information: null");
            }

            return (alternateStarts, biomes, databoxes, materials);
        }

        /// <summary>
        /// Serialise the current randomisation state to disk.
        /// </summary>
        internal static void SaveGameStateToDisk()
        {
            if (s_masterDict != null)
            {
                string base64 = s_masterDict.ToBase64String();
                s_config.sBase64Seed = base64;
                s_config.Save();
                _log.Debug("Saved game state to disk!");
            }
            else
            {
                _log.Error("Could not save game state to disk: invalid data.");
            }
        }

        /// <summary>
        /// Attempt to deserialise a randomisation state from disk.
        /// </summary>
        /// <returns>The EntitySerializer as previously written to disk.</returns>
        /// <exception cref="InvalidDataException">Raised if the game state is corrupted in some way.</exception>
        internal static EntitySerializer RestoreGameStateFromDisk()
        {
            if (string.IsNullOrEmpty(s_config.sBase64Seed))
            {
                throw new InvalidDataException("base64 seed is empty.");
            }

            _log.Debug("Trying to decode base64 string...");
            EntitySerializer dictionary = EntitySerializer.FromBase64String(s_config.sBase64Seed);

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
            if (s_masterDict?.Databoxes?.Count > 0)
            {
                original = AccessTools.Method(typeof(BlueprintHandTarget), nameof(BlueprintHandTarget.Start));
                var prefix = AccessTools.Method(typeof(DataboxPatcher), nameof(DataboxPatcher.PatchDatabox));
                harmony.Patch(original, prefix: new HarmonyMethod(prefix));
            }

            // Changing duplicate scan rewards.
            if (s_masterDict?.FragmentMaterialYield?.Count > 0)
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
