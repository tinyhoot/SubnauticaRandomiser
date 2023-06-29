using System;
using UnityEngine;

namespace SubnauticaRandomiser.Objects
{
    /// <summary>
    /// A wrapper for Unity's Vector3 class to make it serializable.
    /// </summary>
    [Serializable]
    public struct RandomiserVector
    {
        public static RandomiserVector ZERO => new RandomiserVector(0, 0, 0);
        public readonly int x;
        public readonly int y;
        public readonly int z;

        public RandomiserVector(int x, int y, int z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }
        public RandomiserVector(Vector3 vector)
        {
            x = (int)vector.x;
            y = (int)vector.y;
            z = (int)vector.z;
        }

        /// <summary>
        /// Check whether this vector is the same as an in-game Unity vector with negligible differences.
        /// </summary>
        /// <param name="vector">The Unity vector to compare against.</param>
        /// <returns>True if they're equal, false if not.</returns>
        public bool EqualsUnityVector(Vector3 vector)
        {
            if (Math.Abs(x - vector.x) < 1 && Math.Abs(y - vector.y) < 1 && Math.Abs(z - vector.z) < 1)
                return true;

            return false;
        }

        public override bool Equals(object obj) => obj is RandomiserVector other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = x;
                hashCode = (hashCode * 397) ^ y;
                hashCode = (hashCode * 397) ^ z;
                return hashCode;
            }
        }

        public bool Equals(RandomiserVector other) => x == other.x && y == other.y && z == other.z;

        public static bool operator ==(RandomiserVector vec1, RandomiserVector vec2) => vec1.Equals(vec2);

        public static bool operator !=(RandomiserVector vec1, RandomiserVector vec2) => !(vec1 == vec2);

        public override string ToString()
        {
            return x + ", " + y + ", " + z;
        }

        public Vector3 ToUnityVector() => new Vector3(x, y, z);
    }

    public static class Vector3Extensions
    {
        public static RandomiserVector ToRandomiserVector(this Vector3 vector) => new RandomiserVector(vector);
    }
}
