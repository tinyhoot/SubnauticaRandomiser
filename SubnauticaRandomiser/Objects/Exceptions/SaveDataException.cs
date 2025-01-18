using System;

namespace SubnauticaRandomiser.Objects.Exceptions
{
    /// <summary>
    /// The exception that is thrown when something goes wrong when handling save data.
    /// </summary>
    public class SaveDataException : Exception
    {
        public SaveDataException() { }

        public SaveDataException(string message) : base(message) { }
    }
}