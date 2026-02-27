using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HootLib;
using Nautilus.Json.Converters;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace SubnauticaRandomiser.Serialization
{
    internal static class SerdeUtils
    {
        private static JsonSerializerSettings _defaultSettings = new JsonSerializerSettings
        {
            Converters =
            {
                new FloatConverter(),
                new StringEnumConverter(),
                new CustomEnumConverter(),
                new Vector2Converter(),
                new Vector2IntConverter(),
                new Vector3Converter(),
                new Vector3IntConverter()
            }
        };

        /// <summary>
        /// Creating new <see cref="JsonSerializerSettings"/> is expensive. This method returns cached general-purpose
        /// settings with the most commonly needed converters.
        /// </summary>
        public static JsonSerializerSettings GetSettings(params JsonConverter[] converters)
        {
            if (converters.Length == 0)
                return _defaultSettings;

            return new JsonSerializerSettings
            {
                Converters = _defaultSettings.Converters.Concat(converters).ToList()
            };
        }

        /// <summary>
        /// Deserialise something all at once using the general-purpose <see cref="JsonSerializerSettings"/> settings.
        /// </summary>
        public static T DeserializeObject<T>(string json, params JsonConverter[] converters)
        {
            var settings = GetSettings(converters);
            return JsonConvert.DeserializeObject<T>(json, settings);
        }
        
        /// <summary>
        /// Deserialise a collection of objects one at a time. Coroutine friendly.
        /// </summary>
        public static IEnumerator DeserializeObjectsAsync<T>(string json, IOut<List<T>> result,
            params JsonConverter[] converters)
        {
            var logicObjects = new List<T>();
            using JsonReader reader = new JsonTextReader(new StringReader(json));
            // Skip the initial StartArray token so we can deserialise object for object.
            reader.Read();

            var serialiser = JsonSerializer.Create(GetSettings(converters));
            while (reader.Read() && reader.TokenType != JsonToken.EndArray)
            {
                // Doing this is only going to consume as much text as necessary to create another object.
                var entity = serialiser.Deserialize<T>(reader);
                logicObjects.Add(entity);
                yield return null;
            }

            result.Set(logicObjects);
        }
        
        /// <summary>
        /// Read a file's contents from inside the assets folder.
        /// </summary>
        /// <param name="path">The filepath relative to this mod's Assets directory.</param>
        /// <returns>The complete file contents.</returns>
        public static async Task<string> ReadFileContents(params string[] path)
        {
            var fullPath = Path.Combine(new[] { Hootils.GetModDirectory(), "Assets" }.Concat(path).ToArray());
            using StreamReader reader = new StreamReader(File.OpenRead(fullPath));
            return await reader.ReadToEndAsync();
        }
    }
}