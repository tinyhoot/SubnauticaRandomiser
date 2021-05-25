using System;
using SMLHelper.V2.Json;
using SMLHelper.V2.Options.Attributes;
namespace SubnauticaRandomiser
{
    [Menu("Randomiser")]
    public class RandomiserConfig : ConfigFile
    {
        // Every variable listed here will end up in the config file
        // Additionally, adding the relevant Attributes will also
        // make them show up in the in-game options menu
        public string sBase64Seed = "";

        // This option would allow for a "randomiser lite", so to speak
        // With this checked, the randomiser would not go ham on
        // everything, but rather substitute materials 1:1 for something else
        [Toggle("Shuffle ingredients only?")]
        public bool bShuffleRecipes = false;

        [Toggle("Use fish in logic?")]
        public bool bUseFish = false;

        [Toggle("Use seeds in logic?")]
        public bool bUseSeeds = false;

        [Button("Reroll Seed")]
        public void NewSeed()
        {
            // TODO
        }
    }
}
