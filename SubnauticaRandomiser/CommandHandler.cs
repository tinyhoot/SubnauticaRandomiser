using UnityEngine;
using ILogHandler = SubnauticaRandomiser.Interfaces.ILogHandler;

namespace SubnauticaRandomiser
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
            DevConsole.RegisterConsoleCommand(this, "dumpKnownTech", false, false);
            DevConsole.RegisterConsoleCommand(this, "dumpPrefabs", false, false);
#endif
        }
        
        private void OnConsoleCommand_dumpKnownTech(NotificationCenter.Notification n)
        {
            Initialiser._Log.InGameMessage("Dumping known tech");
            DataDumper.LogKnownTech();
        }

        private void OnConsoleCommand_dumpPrefabs(NotificationCenter.Notification n)
        {
            Initialiser._Log.InGameMessage("Dumping prefabs");
            DataDumper.LogPrefabs();
        }
    }
}