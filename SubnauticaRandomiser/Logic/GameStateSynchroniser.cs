using System.IO;
using HarmonyLib;
using SubnauticaRandomiser.Handlers;
using SubnauticaRandomiser.Interfaces;
using SubnauticaRandomiser.Patches;
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
        public void SyncGameState(EntitySerializer serializer)
        {
            if (serializer is null)
                throw new InvalidDataException("Cannot apply randomisation changes: Serializer is null!");
            
            _log.Info("Applying changes to game.");
            // Load changes stored in the serializer.
            foreach (ILogicModule module in Bootstrap.Main.Modules)
            {
                module.ApplySerializedChanges(serializer);
            }

            // Load any changes that rely on harmony patches.
            EnableHarmony();
        }
        
        /// <summary>
        /// Enables all necessary harmony patches based on the randomisation state in the serialiser.
        /// </summary>
        private void EnableHarmony()
        {
            _harmony = new Harmony(Initialiser.GUID);
            foreach (ILogicModule module in Bootstrap.Main.Modules)
            {
                module.SetupHarmonyPatches(_harmony);
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