using QModManager.API;
using Logger = QModManager.Utility.Logger;
namespace SubnauticaRandomiser
{
    /// A class for handling all the logging that the main program might ever want to do.
    /// Also includes main menu messages for relaying information to the user directly.
    public static class LogHandler
    {
        internal static void Info(string message)
        {
            Logger.Log(Logger.Level.Info, message);
        }

        internal static void Warn(string message)
        {
            Logger.Log(Logger.Level.Warn, message);
        }

        internal static void Error(string message)
        {
            Logger.Log(Logger.Level.Error, message);
        }

        internal static void Fatal(string message)
        {
            Logger.Log(Logger.Level.Fatal, message);
        }

        internal static void Debug(string message)
        {
            Logger.Log(Logger.Level.Debug, message);
        }

        /// Send a message through QModManager's main menu system.
        internal static void MainMenuMessage(string message)
        {
            QModServices.Main.AddCriticalMessage(message);
        }
    }
}
