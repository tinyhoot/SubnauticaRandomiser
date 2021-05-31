using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace SubnauticaRandomiser
{
    // This class does three things.
    //
    // First, it provides an easy way to store a large amount of recipes by
    // putting them in a dictionary.
    //
    // Second, it provides a way to save itself to and restore from disk.
    // Because this dictionary eventually contains all randomised recipes,
    // this makes restoring to a previous state trivial.
    //
    // Third, the base64 string representing this class also doubles as a seed.
    [Serializable]
    public class RecipeDictionary
    {
        public Dictionary<int, Recipe> DictionaryInstance = new Dictionary<int, Recipe>();
        public Dictionary<RandomiserVector, TechType> Databoxes = new Dictionary<RandomiserVector, TechType>();

        public string ToBase64String()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                new BinaryFormatter().Serialize(ms, this);
                return Convert.ToBase64String(ms.ToArray());
            }
        }

        public static RecipeDictionary FromBase64String(string base64String)
        {
            byte[] bytes = Convert.FromBase64String(base64String);
            using (MemoryStream ms = new MemoryStream(bytes, 0, bytes.Length))
            {
                ms.Write(bytes, 0, bytes.Length);
                ms.Position = 0;
                return (RecipeDictionary)(new BinaryFormatter().Deserialize(ms));
            }
        }

        public bool Add(TechType t, Recipe r)
        {
            if (DictionaryInstance.ContainsKey((int)t))
            {
                LogHandler.Warn("Tried to add duplicate key "+t.AsString()+" to master dictionary!");
                return false;
            }
            DictionaryInstance.Add((int)t, r);
            return true;
        }
    }
}
