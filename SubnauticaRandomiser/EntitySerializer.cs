using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using SubnauticaRandomiser.Interfaces;
using SubnauticaRandomiser.Objects;

namespace SubnauticaRandomiser
{
    /// <summary>
    /// This class does three things.
    /// <list type="bullet">
    ///     <item><description>
    ///         First, it provides an easy way to store any changes the randomiser makes.
    ///     </description></item>
    ///     <item><description>
    ///         Second, it provides a way to save itself to and restore from disk.
    ///         Because this class contains all changes ever made, this makes restoring to a previous state trivial.
    ///     </description></item>
    ///     <item><description>
    ///         Third, the base64 string representing this class also doubles as a seed.
    ///     </description></item>
    /// </list>
    /// </summary>
    [Serializable]
    internal class EntitySerializer
    {
        public List<Type> EnabledModules;
        
        public RandomiserVector StartPoint;
        // All databoxes and their new locations.
        public Dictionary<RandomiserVector, TechType> Databoxes;
        // The prefab classIds and access codes for doors in the Aurora.
        public Dictionary<string, string> DoorKeyCodes;
        // The options to choose from for spawning materials when scanning a fragment which is already unlocked.
        public LootTable<TechType> FragmentMaterialYield;
        // The number of scans required to unlock the fragment item.
        public Dictionary<TechType, int> NumFragmentsToUnlock;
        // All modified recipes.
        public Dictionary<TechType, Recipe> RecipeDict;
        // All modified fragment spawn rates.
        public Dictionary<TechType, List<SpawnData>> SpawnDataDict;
        // All possible supply box contents.
        public LootTable<TechType> SupplyBoxContents;
        public bool DiscoverEggs;
        public TechType ScrapMetalResult;

        public const int SaveVersion = Initialiser._ExpectedSaveVersion;
        [NonSerialized]
        private ILogHandler _log;

        public EntitySerializer(ILogHandler logger)
        {
            NumFragmentsToUnlock = new Dictionary<TechType, int>();
            RecipeDict = new Dictionary<TechType, Recipe>();
            SpawnDataDict = new Dictionary<TechType, List<SpawnData>>();
            _log = logger;
        }
        
        [OnDeserialized]
        private void OnDeserialized()
        {
            _log = Initialiser._Log;
        }

        /// <summary>
        /// Try to add an entry to the FragmentUnlockNumber dictionary.
        /// </summary>
        /// <param name="type">The TechType to use as key.</param>
        /// <param name="number">The number to use as value.</param>
        /// <returns>True if successful, false if the key already exists in the dictionary.</returns>
        public bool AddFragmentUnlockNum(TechType type, int number)
        {
            if (NumFragmentsToUnlock.ContainsKey(type))
            {
                _log.Warn($"[ES] Tried to add duplicate key {type.AsString()} to FragmentNum master dictionary!");
                return false;
            }
            NumFragmentsToUnlock.Add(type, number);
            return true;
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
                _log.Warn($"[ES] Tried to add duplicate key {type.AsString()} to Recipe master dictionary!");
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
                _log.Warn($"[ES] Tried to add duplicate key {type.AsString()} to SpawnData master dictionary!");
                return false;
            }
            SpawnDataDict.Add(type, data);
            return true;
        }

        /// <summary>
        /// Convert a previously saved string back into an instance of this class.
        /// </summary>
        /// <returns>A typecast EntitySerializer, which may or may not be valid.</returns>
        public static EntitySerializer FromBase64String(string base64String)
        {
            byte[] bytes = Convert.FromBase64String(base64String);
            using MemoryStream ms = new MemoryStream(bytes, 0, bytes.Length);
            ms.Write(bytes, 0, bytes.Length);
            ms.Position = 0;
            return (EntitySerializer)(new BinaryFormatter().Deserialize(ms));
        }

        /// <summary>
        /// Serialise the current randomisation state to disk.
        /// </summary>
        public void Serialize(RandomiserConfig config)
        {
            string base64 = ToBase64String();
            config.sBase64Seed = base64;
            config.Save();
            _log.Debug("[ES] Saved game state to disk!");
        }
        
        /// <summary>
        /// Convert this class to a string for saving.
        /// </summary>
        public string ToBase64String()
        {
            using MemoryStream ms = new MemoryStream();
            new BinaryFormatter().Serialize(ms, this);
            return Convert.ToBase64String(ms.ToArray());
        }
    }
}
