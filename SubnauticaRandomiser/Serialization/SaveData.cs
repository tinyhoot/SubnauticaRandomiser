using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Nautilus.Json;
using Newtonsoft.Json;
using SubnauticaRandomiser.Handlers;
using SubnauticaRandomiser.Objects.Exceptions;
using SubnauticaRandomiser.Serialization.Modules;

namespace SubnauticaRandomiser.Serialization
{
    /// <summary>
    /// Saves all randomised state for easy replication at a later time.
    /// </summary>
    [JsonObject(MemberSerialization.Fields)]
    internal class SaveData : SaveDataCache
    {
        private List<Type> _enabledModules = new List<Type>();
        // Ensure this collection of abstract save data deserialises properly into the correct subclasses.
        [JsonProperty(ItemTypeNameHandling = TypeNameHandling.All)]
        private HashSet<BaseModuleSaveData> _moduleSaveData = new HashSet<BaseModuleSaveData>();
        public int SaveVersion = -1;

        [JsonIgnore] public ReadOnlyCollection<Type> EnabledModules => _enabledModules.AsReadOnly();

        public void AddModuleData(BaseModuleSaveData data)
        {
            // There's no reason why any module should have two of the same sava data so warn if that occurs.
            if (_moduleSaveData.Select(save => save.GetType()).Contains(data.GetType()))
                PrefixLogHandler.Get("[SaveData]").Warn($"Overwriting existing module save data: {data.GetType()}");
            _moduleSaveData.Add(data);
        }

        /// <summary>
        /// Check whether this save contains module-specific save data of the given type.
        /// </summary>
        public bool Contains<T>() where T : BaseModuleSaveData
        {
            return _moduleSaveData.Any(data => data.GetType() == typeof(T));
        }

        /// <summary>
        /// Attempts to get existing module-specific save data.
        /// </summary>
        /// <typeparam name="T">The specific data to load.</typeparam>
        /// <exception cref="SaveDataException">Thrown if no such save data has previously been registered.</exception>
        public T GetModuleData<T>() where T : BaseModuleSaveData
        {
            BaseModuleSaveData data = _moduleSaveData.FirstOrDefault(data => data.GetType() == typeof(T));
            if (data is T typedData)
                return typedData;
            throw new SaveDataException($"No module save data of type '{typeof(T)}' found.");
        }
        
        /// <summary>
        /// Attempts to get existing module-specific save data.
        /// </summary>
        /// <typeparam name="T">The specific data to load.</typeparam>
        /// <exception cref="SaveDataException">Thrown if no such save data has previously been registered.</exception>
        public bool TryGetModuleData<T>(out T moduleData) where T : BaseModuleSaveData
        {
            BaseModuleSaveData baseData = _moduleSaveData.FirstOrDefault(data => data.GetType() == typeof(T));
            moduleData = baseData as T;
            return !(moduleData is null);
        }

        public void SetEnabledModules(IEnumerable<Type> modules)
        {
            _enabledModules = modules.ToList();
        }
        
        /// <summary>
        /// Work around an issue in Nautilus/Newtonsoft(?) where the JSON fails to populate fields that already have
        /// values in them. By resetting these we ensure the save games of successive games are loaded properly.
        /// </summary>
        public void Reset()
        {
            PrefixLogHandler.Get("[SaveData]").Debug("Flushing save data from memory for the next save.");
            _enabledModules.Clear();
            _moduleSaveData.Clear();
            SaveVersion = -1;
        }
    }
}