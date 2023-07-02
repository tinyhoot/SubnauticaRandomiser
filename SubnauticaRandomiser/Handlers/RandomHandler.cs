using System;
using System.Collections.Generic;
using SubnauticaRandomiser.Interfaces;
using SubnauticaRandomiser.Objects.Enums;
using Random = System.Random;

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

        public int Next(RandomDistribution dist)
        {
            return Next(0, int.MaxValue, dist);
        }

        public int Next(int maxValue)
        {
            return _random.Next(maxValue);
        }

        public int Next(int maxValue, RandomDistribution dist)
        {
            return Next(0, maxValue, dist);
        }

        public int Next(int minValue, int maxValue)
        {
            return _random.Next(minValue, maxValue);
        }

        public int Next(int minValue, int maxValue, RandomDistribution dist)
        {
            double weight = NextDouble(dist);
            int x = (int)Math.Floor((maxValue - minValue) * weight);
            return minValue + x;
        }

        public double NextDouble()
        {
            return _random.NextDouble();
        }

        public double NextDouble(RandomDistribution dist)
        {
            return dist.ApplyFunction(NextDouble());
        }
    }
}