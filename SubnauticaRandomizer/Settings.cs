using Oculus.Newtonsoft.Json;

namespace SubnauticaRandomizer
{
    [JsonObject(MemberSerialization.OptOut)]
    public class Settings
    {
        internal Recipes Recipes = new Recipes();

        public string RecipeSeed;

        public static Settings Instance = new Settings();
    }
}
