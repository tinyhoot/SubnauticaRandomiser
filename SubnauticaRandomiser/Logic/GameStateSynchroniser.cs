using System.IO;
using System.Linq;
using HarmonyLib;
using HootLib;
using SubnauticaRandomiser.Handlers;
using SubnauticaRandomiser.Interfaces;
using SubnauticaRandomiser.Objects.Exceptions;
using SubnauticaRandomiser.Patches;
using SubnauticaRandomiser.Serialization;
using ILogHandler = HootLib.Interfaces.ILogHandler;

namespace SubnauticaRandomiser.Logic
{
    /// <summary>
    /// Synchronises the game with a previously randomised and saved state as determined by the core logic.
    /// </summary>
    internal class GameStateSynchroniser
    {
        private Harmony _harmony;
        private ILogHandler _log = PrefixLogHandler.Get("[Sync]");

        public GameStateSynchroniser(string guid)
        {
            _harmony = new Harmony(guid);
        }
        
        /// <summary>
        /// Apply a ready-made randomised state to the game.
        /// </summary>
        /// <exception cref="InvalidDataException">Raised if the serializer is null.</exception>
        public void SyncGameState(SaveData saveData)
        {
            if (saveData is null)
                throw new InvalidDataException("Cannot apply randomisation changes: Save Data is null!");

            if (!CheckAllModulesEnabled(saveData))
                throw new RandomisationException("Active modules do not match modules expected by save data!");
            
            _log.Info("Applying changes to game.");
            // Load changes stored in the save data.
            foreach (ILogicModule module in Bootstrap.Main.Modules)
            {
                module.ApplySerializedChanges(saveData);
            }

            // Load any changes that rely on harmony patches.
            EnableHarmony(saveData);
        }

        /// <summary>
        /// Check whether all modules that the save data expects to be enabled were actually enabled by the Bootstrap,
        /// and vice versa.
        /// </summary>
        private bool CheckAllModulesEnabled(SaveData saveData)
        {
            _log.Debug($"Saved modules: {saveData.EnabledModules.ElementsToString()}");
            var activeModules = Bootstrap.Main.GetActiveModuleTypes();
            _log.Debug($"Enabled modules: {activeModules.ElementsToString()}");
            // Compare the elements of the two lists by checking the length of the set difference.
            return !saveData.EnabledModules.Except(activeModules).Any();
        }
        
        /// <summary>
        /// Enables all necessary harmony patches based on the randomisation state in the serialiser.
        /// </summary>
        private void EnableHarmony(SaveData saveData)
        {
            _harmony = new Harmony(Initialiser.GUID);
            foreach (ILogicModule module in Bootstrap.Main.Modules)
            {
                module.SetupHarmonyPatches(_harmony, saveData);
            }
            // Always apply bugfixes.
            _harmony.PatchAll(typeof(VanillaBugfixes));
        }

        /// <summary>
        /// Undo any changes and restore the vanilla game state.
        /// </summary>
        public void Teardown()
        {
            _harmony.UnpatchSelf();
        }
    }
}