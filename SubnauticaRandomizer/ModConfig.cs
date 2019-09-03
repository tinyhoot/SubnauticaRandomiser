using Oculus.Newtonsoft.Json;

namespace SubnauticaRandomizer
{
    [JsonObject(MemberSerialization.OptOut)]
    public class ModConfig
    {
        public string Id { get; set; }
        public Settings Config { get; set; }
    }
}
