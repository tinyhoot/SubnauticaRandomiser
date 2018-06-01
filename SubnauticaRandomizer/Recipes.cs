using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace SubnauticaRandomizer
{
    [Serializable]
    public class Recipes
    {
        public Dictionary<int, Recipe> RecipesByType = new Dictionary<int, Recipe>();

        public string ToBase64String()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                new BinaryFormatter().Serialize(ms, this);
                return Convert.ToBase64String(ms.ToArray());
            }
        }

        public static Recipes FromBase64String(string base64String)
        {
            byte[] bytes = Convert.FromBase64String(base64String);
            using (MemoryStream ms = new MemoryStream(bytes, 0, bytes.Length))
            {
                ms.Write(bytes, 0, bytes.Length);
                ms.Position = 0;
                return (Recipes)(new BinaryFormatter().Deserialize(ms));
            }
        }
    }
}
