using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using SubnauticaRandomiser.Logic.LogicObjects;

namespace SubnauticaRandomiser.Serialization.Converters
{
    /// <summary>
    /// Converts a JSON string to a reference to the identifier of a <see cref="Region"/> and vice versa.
    /// Intended for use with objects that use Regions rather than Regions themselves.
    /// </summary>
    internal class StringRegionConverter : JsonConverter
    {
        private List<Region> _regions;

        public StringRegionConverter(List<Region> regions)
        {
            _regions = regions;
        }
        
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Region);
        }
        
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            string json = reader.Value as string;
            if (string.IsNullOrEmpty(json))
                return null;

            // Try to resolve the unique name from the JSON into a proper Region we loaded previously.
            var region = _regions.Find(r => r.Name.Equals(json, StringComparison.InvariantCultureIgnoreCase));
            if (region is null)
                throw new SerializationException($"Could not find Region with name '{json}'!");
            
            return region;
        }
        
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            Region region = (Region)value;
            serializer.Serialize(writer, region.Name);
        }
    }
}