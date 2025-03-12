using HootLib.Interfaces;

namespace Tests.Mocks
{
    /// <summary>
    /// Since the normal LogHandler is tightly coupled to the game, this replacement allows for "logging" in an
    /// isolated environment.
    /// </summary>
    public class FakeLogger : ILogHandler
    {
        public void Debug(string message)
        {
        }

        public void Info(string message)
        {
        }

        public void Warn(string message)
        {
        }

        public void Error(string message)
        {
        }

        public void Fatal(string message)
        {
        }

        public void InGameMessage(string message, bool error = false)
        {
        }
    }
}