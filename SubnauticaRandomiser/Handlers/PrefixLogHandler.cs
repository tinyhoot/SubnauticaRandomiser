using System.Collections.Generic;
using HootLib.Interfaces;

namespace SubnauticaRandomiser.Handlers
{
    /// <summary>
    /// A logger that automatically adds a prefix denoting the origin of the log message.
    /// </summary>
    internal class PrefixLogHandler : ILogHandler
    {
        private static readonly Dictionary<string, PrefixLogHandler> _loggers = new Dictionary<string, PrefixLogHandler>();
        private readonly ILogHandler _log;
        private readonly string _prefix;
        
        public PrefixLogHandler(string prefix, ILogHandler log)
        {
            _log = log;
            _prefix = prefix;
            _loggers[prefix] = this;
        }

        public void Debug(string message)
        {
            _log.Debug($"{_prefix} {message}");
        }

        public void Info(string message)
        {
            _log.Info($"{_prefix} {message}");
        }

        public void Warn(string message)
        {
            _log.Warn($"{_prefix} {message}");
        }

        public void Error(string message)
        {
            _log.Error($"{_prefix} {message}");
        }

        public void Fatal(string message)
        {
            _log.Fatal($"{_prefix} {message}");
        }

        public void InGameMessage(string message, bool error = false)
        {
            _log.InGameMessage(message, error);
        }

        public static PrefixLogHandler Get(string prefix)
        {
            if (_loggers.TryGetValue(prefix, out PrefixLogHandler log))
                return log;
            _loggers[prefix] = new PrefixLogHandler(prefix, Initialiser._Log);
            return _loggers[prefix];
        }
    }
}