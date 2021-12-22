using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using SubnauticaRandomiser.RandomiserObjects;

namespace SubnauticaRandomiser
{
    // This class does three things.
    //
    // First, it provides an easy way to store a large amount of recipes or
    // spawnables by putting them in a dictionary.
    //
    // Second, it provides a way to save itself to and restore from disk.
    // Because this dictionary eventually contains all randomised entities,
    // this makes restoring to a previous state trivial.
    //
    // Third, the base64 string representing this class also doubles as a seed.
    [Serializable]
    public class EntitySerializer
    {
        public Dictionary<TechType, Recipe> RecipeDict = new Dictionary<TechType, Recipe>();
        public Dictionary<TechType, SpawnData> SpawnDataDict = new Dictionary<TechType, SpawnData>();
        public Dictionary<RandomiserVector, TechType> Databoxes = new Dictionary<RandomiserVector, TechType>();

        public bool isDataboxRandomised = false;
        public static readonly int s_SaveVersion = InitMod.s_expectedSaveVersion;

        // Convert this class to a string for saving.
        public string ToBase64String()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                new BinaryFormatter().Serialize(ms, this);
                return Convert.ToBase64String(ms.ToArray());
            }
        }

        // Convert a previously saved string back into an instance of this class.
        public static EntitySerializer FromBase64String(string base64String)
        {
            byte[] bytes = Convert.FromBase64String(base64String);
            using (MemoryStream ms = new MemoryStream(bytes, 0, bytes.Length))
            {
                ms.Write(bytes, 0, bytes.Length);
                ms.Position = 0;
                return (EntitySerializer)(new BinaryFormatter().Deserialize(ms));
            }
        }

        // Try to add an entry to the Recipe dictionary. Returns true if successful.
        public bool AddRecipe(TechType type, Recipe r)
        {
            if (RecipeDict.ContainsKey(type))
            {
                LogHandler.Warn("Tried to add duplicate key " + type.AsString() + " to Recipe master dictionary!");
                return false;
            }
            RecipeDict.Add(type, r);
            return true;
        }

        // Try to add an entry to the SpawnData dictionary. Returns true if successful.
        public bool AddSpawnData(TechType type, SpawnData data)
        {
            if (SpawnDataDict.ContainsKey(type))
            {
                LogHandler.Warn("Tried to add duplicate key " + type.AsString() + "to SpawnData master dictionary!");
                return false;
            }
            SpawnDataDict.Add(type, data);
            return true;
        }

        // Does the recipe dictionary contain any knife? Used for progression.
        public bool ContainsKnife()
        {
            return RecipeDict.ContainsKey(TechType.Knife) || RecipeDict.ContainsKey(TechType.HeatBlade);
        }
    }
}
