namespace SubnauticaRandomiser.Interfaces
{
    public interface ILogHandler
    {
        public void Debug(string message);
        
        public void Info(string message);

        public void Warn(string message);

        public void Error(string message);

        public void Fatal(string message);

        public void MainMenuMessage(string message);
    }
}