using System;

namespace SubnauticaRandomiser.Objects.Exceptions
{
    /// <summary>
    /// The exception that is thrown when trying to get an invalid config entry by id.
    /// </summary>
    public class ConfigFieldException : Exception
    {
        public ConfigFieldException() { }

        public ConfigFieldException(string message) : base(message) { }
    }
}