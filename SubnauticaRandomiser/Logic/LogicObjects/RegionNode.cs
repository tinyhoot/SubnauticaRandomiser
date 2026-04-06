using System;
using Nautilus.Json.Converters;
using Newtonsoft.Json;
using UnityEngine;

namespace SubnauticaRandomiser.Logic.LogicObjects
{
    /// <summary>
    /// Represents a particular position within a specific <see cref="Region"/>.
    /// </summary>
    [Serializable]
    internal class RegionNode
    {
        public string RegionName;
        
        [JsonConverter(typeof(Vector3Converter))]
        public Vector3 Position;

        public override string ToString()
        {
            return $"<{RegionName}{Position}>";
        }
    }
}