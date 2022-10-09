using System;
using SubnauticaRandomiser.Interfaces;

namespace Tests.Mocks
{
    public class FakeLogger : ILogHandler
    {
        public void Debug(string message)
        {
            Console.WriteLine(message);
        }

        public void Info(string message)
        {
            message = "";
        }

        public void Warn(string message)
        {
            message = "";
        }

        public void Error(string message)
        {
            message = "";
        }

        public void Fatal(string message)
        {
            message = "";
        }

        public void MainMenuMessage(string message)
        {
            message = "";
        }
    }
}