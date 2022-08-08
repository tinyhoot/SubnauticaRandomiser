using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using SubnauticaRandomiser.RandomiserObjects;

namespace SubnauticaRandomiser
{
    
    /// <summary>
    /// This class does three things.
    /// <list type="bullet">
    ///     <item><description>
    ///         First, it provides an easy way to store a large amount of recipes or spawnables by
    ///         putting them in a dictionary.
    ///     </description></item>
    ///     <item><description>
    ///         Second, it provides a way to save itself to and restore from disk.
    ///         Because this dictionary eventually contains all randomised entities,
    ///         this makes restoring to a previous state trivial.
    ///     </description></item>
    ///     <item><description>
    ///         Third, the base64 string representing this class also doubles as a seed.
    ///     </description></item>
    /// </list>
    /// </summary>
    [Serializable]
    public class EntitySerializer
    {
        public Dictionary<TechType, Recipe> RecipeDict = new Dictionary<TechType, Recipe>();
        public Dictionary<TechType, List<SpawnData>> SpawnDataDict = new Dictionary<TechType, List<SpawnData>>();
        public Dictionary<RandomiserVector, TechType> Databoxes = new Dictionary<RandomiserVector, TechType>();

        public bool isDataboxRandomised = false;
        public static readonly int s_SaveVersion = InitMod.s_expectedSaveVersion;

        /// <summary>
        /// Convert this class to a string for saving.
        /// </summary>
        public string ToBase64String()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                new BinaryFormatter().Serialize(ms, this);
                return Convert.ToBase64String(ms.ToArray());
            }
        }
        
        /// <summary>
        /// Convert a previously saved string back into an instance of this class.
        /// </summary>
        /// <param name="base64String"></param>
        /// <returns>A typecast EntitySerializer, which may or may not be valid.</returns>
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
        
        /// <summary>
        /// Try to add an entry to the Recipe dictionary.
        /// </summary>
        /// <param name="type">The TechType to use as key.</param>
        /// <param name="r">The Recipe to use as value.</param>
        /// <returns>True if successful, false if the key already exists in the dictionary.</returns>
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

        /// <summary>
        /// Try to add an entry to the SpawnData dictionary.
        /// </summary>
        /// <param name="type">The TechType to use as key.</param>
        /// <param name="data">The SpawnData to use as value.</param>
        /// <returns>True if successful, false if the key already exists in the dictionary.</returns>
        public bool AddSpawnData(TechType type, List<SpawnData> data)
        {
            if (SpawnDataDict.ContainsKey(type))
            {
                LogHandler.Warn("Tried to add duplicate key " + type.AsString() + "to SpawnData master dictionary!");
                return false;
            }
            SpawnDataDict.Add(type, data);
            return true;
        }
        
        /// <summary>
        /// Check whether the recipe dictionary contains any kind of knife. Used for progression checks.
        /// </summary>
        public bool ContainsKnife()
        {
            return RecipeDict.ContainsKey(TechType.Knife) || RecipeDict.ContainsKey(TechType.HeatBlade);
        }
    }
}
