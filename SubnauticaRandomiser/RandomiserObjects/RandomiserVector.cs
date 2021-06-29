using System;
using UnityEngine;

namespace SubnauticaRandomiser
{
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
    }
}
