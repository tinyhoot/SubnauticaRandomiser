using System;
using QModManager.API;
using SubnauticaRandomiser.Interfaces;
using Logger = QModManager.Utility.Logger;
namespace SubnauticaRandomiser
{
    /// A class for handling all the logging that the main program might ever want to do.
    /// Also includes main menu messages for relaying information to the user directly.
    [Serializable]
    public class LogHandler : ILogHandler
    {
        public void Debug(string message)
        {
            Logger.Log(Logger.Level.Debug, message);
        }
        
        public void Info(string message)
        {
            Logger.Log(Logger.Level.Info, message);
        }

        public void Warn(string message)
        {
            Logger.Log(Logger.Level.Warn, message);
        }

        public void Error(string message)
        {
            Logger.Log(Logger.Level.Error, message);
        }

        public void Fatal(string message)
        {
            Logger.Log(Logger.Level.Fatal, message);
        }

        /// Send a message through QModManager's main menu system.
        public void MainMenuMessage(string message)
        {
            QModServices.Main.AddCriticalMessage(message);
        }
    }
}
