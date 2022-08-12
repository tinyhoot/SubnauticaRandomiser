using System;

namespace SubnauticaRandomiser.RandomiserObjects
{
    /// <summary>
    /// The exception that is thrown when an input file cannot be parsed properly into the expected objects.
    /// </summary>
    public class ParsingException : Exception
    {
        public ParsingException() {}

        public ParsingException(string message) : base(message) {}
    }
}