using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HootLib;
using Newtonsoft.Json;

namespace SubnauticaRandomiser.Serialization
{
    internal static class SerdeUtils
    {
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

            var serialiser = JsonSerializer.Create(new JsonSerializerSettings { Converters = converters });
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