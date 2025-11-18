using System;
using System.Collections.Generic;
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

        public StringEntityConverter(){}
        
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
            // This could be either a straight-up LogicEntity or a list of references specified in a dependencies
            // section. Find out which.
            if (reader.TokenType == JsonToken.StartArray)
            {
                List<LogicEntity> entities = new List<LogicEntity>();
                // Read the next entry but stop if we reached the end of the list.
                while (reader.Read() && reader.TokenType != JsonToken.EndArray)
                {
                    if (reader.TokenType == JsonToken.String)
                        entities.Add(Deserialise(reader.Value as string));
                }

                // Position the reader for the rest of the JSON after this list.
                reader.Skip();
                return entities;
            }

            // Parse just a single entity.
            return Deserialise(reader.Value as string);
        }

        private LogicEntity Deserialise(string json)
        {
            if (string.IsNullOrEmpty(json))
                return null;
            
            // The entity isn't saved as a whole class, but rather as a reference to its type and name.
            var split = json!.Split(LogicEntity.TypeNameSeparator);
            if (split.Length != 2)
                throw new JsonSerializationException("Entity must contain exactly one " +
                                                     $"'{LogicEntity.TypeNameSeparator}' separator!");
            
            Type entityType = ConvertToEntityType(split[0]);
            if (entityType is null)
                throw new JsonSerializationException($"Entity type is not a valid LogicEntity or subclass: {split[0]}");
            if (!Enum.TryParse(split[1], true, out TechType techType))
                throw new JsonSerializationException($"Entity name is not a valid TechType: '{split[1]}'");
            
            // Try to look up the type-name combination in the manager.
            var entity = _manager?.Find(entityType, techType);
            if (entity is null)
            {
                // throw new JsonSerializationException($"No entity of type '{entityType}' and TechType '{techType}' exists!");
                // Create a temporary reference to be resolved later.
                return new LogicEntityReference(entityType, techType);
            }

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