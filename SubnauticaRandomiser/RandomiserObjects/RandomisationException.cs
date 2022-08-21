using System;

namespace SubnauticaRandomiser.RandomiserObjects
{
    /// <summary>
    /// Represents grave errors that occur during randomisation.
    /// </summary>
    public class RandomisationException : Exception
    {
        public RandomisationException(string message) : base(message)
        {
            
        }
    }
}