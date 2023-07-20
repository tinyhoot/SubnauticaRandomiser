using System;
using UnityEngine;

namespace SubnauticaRandomiser.Objects.Enums
{
    public enum RandomDistribution
    {
        Normal,
        PreferLow,
        PreferHigh,
        PreferExtremes,
    }

    public static class RandomDistributionExtensions
    {
        /// <summary>
        /// Apply a distribution function to the random variable x.
        /// </summary>
        /// <returns>The weighted variable, also between 0 and 1.</returns>
        /// <exception cref="ArgumentException">Thrown if x is not between 0 and 1, inclusive.</exception>
        public static double ApplyFunction(this RandomDistribution dist, double x)
        {
            if (x < 0 || x > 1)
                throw new ArgumentException("x must be a value between 0 and 1!");

            x = dist switch
            {
                RandomDistribution.Normal => x,
                RandomDistribution.PreferLow => Math.Pow(x - 1, 2) * 2,
                RandomDistribution.PreferHigh => Math.Pow(x, 2) * 2,
                RandomDistribution.PreferExtremes => 8 * Math.Pow(x - 0.5, 2),
                _ => throw new ArgumentException($"Unhandled enum: {dist}")
            };

            return Mathf.Clamp((float)x, 0f, 1f);
        }
    }
}