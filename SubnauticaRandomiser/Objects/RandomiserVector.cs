using System;
using UnityEngine;

namespace SubnauticaRandomiser.Objects
{
    /// <summary>
    /// A wrapper for Unity's Vector3 class to make it serializable.
    /// </summary>
    [Serializable]
    public class RandomiserVector
    {
        public int x;
        public int y;
        public int z;

        public RandomiserVector(int x = 0, int y = 0, int z = 0)
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

        public override string ToString()
        {
            return x + ", " + y + ", " + z;
        }

        public Vector3 ToUnityVector()
        {
            return new Vector3(x, y, z);
        }
    }
}
