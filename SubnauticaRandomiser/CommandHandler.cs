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
#endif
        }
        
        private void OnConsoleCommand_dumpKnownTech(NotificationCenter.Notification n)
        {
            InitMod._log.InGameMessage("Dumping known tech");
            DataDumper.LogKnownTech();
        }
    }
}