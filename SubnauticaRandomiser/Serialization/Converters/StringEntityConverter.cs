using System;
using HarmonyLib;
using Newtonsoft.Json;
using SubnauticaRandomiser.Logic;
using SubnauticaRandomiser.Logic.LogicObjects;

namespace SubnauticaRandomiser.Serialization.Converters
{
    /// <summary>
    /// Converts a JSON string to the identifier of a <see cref="LogicEntity"/> and vice versa.
    /// Intended for use with objects that reference entities rather than entitites themselves.
    /// </summary>
    internal class StringEntityConverter : JsonConverter
    {
        private const char Separator = ':';
        private readonly Type _logicEntity = typeof(LogicEntity);
        private EntityManager _manager;

        public StringEntityConverter(EntityManager manager)
        {
            _manager = manager;
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == _logicEntity;
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
            JsonSerializer serializer)
        {
            // The entity will have been saved as a reference only. We need to use the available info to uniquely
            // identify the specific existing entity in the manager.
            string json = reader.Value as string;
            if (string.IsNullOrEmpty(json))
                return null;

            // The entity isn't saved as a whole class, but rather as a reference to its type and name.
            var split = json!.Split(Separator);
            if (split.Length != 2)
                throw new JsonSerializationException($"Entity must contain exactly one '{Separator}' separator!");
            
            Type entityType = ConvertToEntityType(split[0]);
            if (entityType is null)
                throw new JsonSerializationException($"Entity type is not a valid LogicEntity or subclass: {split[0]}");
            if (!Enum.TryParse(split[1], true, out TechType techType))
                throw new JsonSerializationException($"Entity name is not a valid TechType: '{split[1]}'");
            
            // Try to look up the type-name combination in the manager.
            var entity = _manager.Find(entityType, techType);
            if (entity is null)
                throw new JsonSerializationException($"No entity of type '{entityType}' and TechType '{techType}' exists!");
            return entity;
        }

        private Type ConvertToEntityType(string typeName)
        {
            var type = AccessTools.TypeByName(typeName);
            // Is a LogicEntity or subclass thereof.
            if (_logicEntity.IsAssignableFrom(type))
                return type;
            return null;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            LogicEntity entity = (LogicEntity)value;
            string typeName = entity.GetType().Name;
            string id = entity.TechType.AsString();

            serializer.Serialize(writer, $"{typeName}{Separator}{id}");
        }
    }
}