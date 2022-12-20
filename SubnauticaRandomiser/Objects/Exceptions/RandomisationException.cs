using System;

namespace SubnauticaRandomiser.Objects.Exceptions
{
    /// <summary>
    /// Represents grave errors that occur during randomisation.
    /// </summary>
    public class RandomisationException : Exception
    {
        public RandomisationException() {}
        
        public RandomisationException(string message) : base(message) {}
    }
}