using System;
using System.Collections.Generic;
using SubnauticaRandomiser.Interfaces;

namespace SubnauticaRandomiser.Handlers
{
    /// <summary>
    /// Handle anything related to random events and provide some convenience functions not present in System.Random.
    /// </summary>
    internal class RandomHandler : IRandomHandler
    {
        private readonly Random _random;

        public RandomHandler()
        {
            _random = new Random();
        }
        
        public RandomHandler(int seed)
        {
            _random = new Random(seed);
        }

        public T Choice<T>(ICollection<T> collection)
        {
            if (collection is null || collection.Count == 0)
                return default;

            int idx = _random.Next(0, collection.Count);
            int i = 0;
            foreach (T element in collection)
            {
                if (i == idx)
                    return element;
                i++;
            }
            
            // If the code somehow got to down here, something went very wrong.
            throw new IndexOutOfRangeException("Failed to find element for index " + idx);
        }

        public int Next()
        {
            return _random.Next();
        }

        public int Next(int maxValue)
        {
            return _random.Next(maxValue);
        }

        public int Next(int minValue, int maxValue)
        {
            return _random.Next(minValue, maxValue);
        }

        public double NextDouble()
        {
            return _random.NextDouble();
        }
    }
}