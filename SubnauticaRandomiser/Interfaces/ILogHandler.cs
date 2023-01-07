namespace SubnauticaRandomiser.Interfaces
{
    internal interface ILogHandler
    {
        public void Debug(string message);
        
        public void Info(string message);

        public void Warn(string message);

        public void Error(string message);

        public void Fatal(string message);

        public void InGameMessage(string message, bool error = false);
    }
}