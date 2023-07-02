using System.Collections.Generic;
using SubnauticaRandomiser.Objects.Enums;

namespace SubnauticaRandomiser.Interfaces
{
    internal interface IRandomHandler
    {
        /// <summary>
        /// Get a nonnegative random integer.
        /// </summary>
        public int Next();
        
        /// <inheritdoc cref="Next()"/>
        /// <param name="dist">The random distribution weighting to apply to the variable.</param>
        public int Next(RandomDistribution dist);
        
        /// <summary>
        /// Get a nonnegative random integer up to maxValue, exclusive.
        /// </summary>
        /// <param name="maxValue">The exclusive upper bound.</param>
        public int Next(int maxValue);
        
        /// <inheritdoc cref="Next(int)"/>
        /// <param name="dist">The random distribution weighting to apply to the variable.</param>
        public int Next(int maxValue, RandomDistribution dist);

        
        /// <summary>
        /// Get a random integer between the lower and upper bound.
        /// </summary>
        /// <param name="minValue">The lower bound, inclusive.</param>
        /// <param name="maxValue">The upper bound, exclusive.</param>
        public int Next(int minValue, int maxValue);

        /// <inheritdoc cref="Next(int, int)"/>
        /// <param name="dist">The random distribution weighting to apply to the variable.</param>
        public int Next(int minValue, int maxValue, RandomDistribution dist);

        /// <summary>
        /// Returns a random floating-point number between 0.0 and 1.0
        /// </summary>
        public double NextDouble();

        /// <inheritdoc cref="NextDouble()"/>
        /// <param name="dist">The random distribution weighting to apply to the variable.</param>
        public double NextDouble(RandomDistribution dist);

        /// <summary>
        /// Get a random element from a collection of items.
        /// </summary>
        /// <param name="collection">The collection to get an element from.</param>
        /// <returns>A random element, or null if the collection has no elements.</returns>
        public T Choice<T>(ICollection<T> collection);
    }
}