using UnityEngine;

namespace SubnauticaRandomiser.Handlers
{
    /// <summary>
    /// Listen to and execute custom console commands.
    /// </summary>
    internal class CommandHandler : MonoBehaviour
    {
        /// <summary>
        /// Register the console commands added by this mod.
        /// </summary>
        public void RegisterCommands()
        {
#if DEBUG
            DevConsole.RegisterConsoleCommand("rando", OnConsoleCommand_rando);
#endif
        }

        private void OnConsoleCommand_rando(NotificationCenter.Notification n)
        {
            DevConsole.SendConsoleCommand("oxygen");
            DevConsole.SendConsoleCommand("nodamage");
            DevConsole.SendConsoleCommand("item seaglide");
            DevConsole.SendConsoleCommand("item scanner");
        }
    }
}