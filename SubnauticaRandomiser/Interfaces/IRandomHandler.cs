using System.Collections.Generic;

namespace SubnauticaRandomiser.Interfaces
{
    internal interface IRandomHandler
    {
        /// <summary>
        /// Get a nonnegative random integer.
        /// </summary>
        public int Next();
        
        /// <summary>
        /// Get a nonnegative random integer up to maxValue, exclusive.
        /// </summary>
        /// <param name="maxValue">The exclusive upper bound.</param>
        public int Next(int maxValue);
        
        /// <summary>
        /// Get a random integer between the lower and upper bound.
        /// </summary>
        /// <param name="minValue">The lower bound, inclusive.</param>
        /// <param name="maxValue">The upper bound, exclusive.</param>
        public int Next(int minValue, int maxValue);

        /// <summary>
        /// Returns a random floating-point number between 0.0 and 1.0
        /// </summary>
        public double NextDouble();

        /// <summary>
        /// Get a random element from a collection of items.
        /// </summary>
        /// <param name="collection">The collection to get an element from.</param>
        /// <returns>A random element, or null if the collection has no elements.</returns>
        public T Choice<T>(ICollection<T> collection);
    }
}