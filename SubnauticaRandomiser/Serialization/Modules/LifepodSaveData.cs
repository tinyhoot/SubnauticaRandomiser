using Nautilus.Json.Converters;
using Newtonsoft.Json;
using UnityEngine;

namespace SubnauticaRandomiser.Serialization.Modules
{
    internal class LifepodSaveData : BaseModuleSaveData
    {
        [JsonConverter(typeof(Vector3Converter))]
        public Vector3 StartPoint;
    }
}