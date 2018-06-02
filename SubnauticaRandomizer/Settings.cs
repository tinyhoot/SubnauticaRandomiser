using Oculus.Newtonsoft.Json;
using System.Collections.Generic;

namespace SubnauticaRandomizer
{
    [JsonObject(MemberSerialization.OptOut)]
    public class Settings
    {
        internal ScannerData ScannerData;
        internal Recipes Recipes = new Recipes();

        public Dictionary<string, int> Blueprints = new Dictionary<string, int>();
        public string RecipeSeed;
        public bool RandomizeMe = false;

        public static Settings Instance = new Settings();

        public void Initialize()
        {
            ScannerData = new ScannerData(Blueprints);
        }
    }
}
