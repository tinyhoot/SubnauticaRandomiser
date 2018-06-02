using System;
using System.Collections.Generic;
using System.Linq;

namespace SubnauticaRandomizer.Randomizer
{
    public static class RandomizerExtensions
    {
        public static T PickRandom<T>(this IEnumerable<T> source)
        {
            return source.OrderBy(x => Guid.NewGuid()).First();
        }
    }
}
