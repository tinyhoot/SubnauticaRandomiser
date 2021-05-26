using System;
using SMLHelper.V2.Json;
using SMLHelper.V2.Options.Attributes;
namespace SubnauticaRandomiser
{
    [Menu("Randomiser")]
    public class RandomiserConfig : ConfigFile
    {
        private DateTime _timeButtonPressed = new DateTime();
        private readonly int _confirmInterval = 5;

        // Every public variable listed here will end up in the config file
        // Additionally, adding the relevant Attributes will also make them
        // show up in the in-game options menu

        // This option would allow for a "randomiser lite", so to speak.
        // With this checked, the randomiser would not go ham on
        // everything, but rather substitute materials 1:1 for something else
        //[Toggle("Shuffle ingredients only?")]
        //public bool bShuffleRecipes = false;

        [Toggle("Use fish in logic?")]
        public bool bUseFish = false;

        [Toggle("Use seeds in logic?")]
        public bool bUseSeeds = false;

        [Button("Randomise Again")]
        public void NewRandomisation()
        {
            // Re-randomising everything is a serious request, and it should not
            // happen accidentally. This here ensures the button is pressed twice
            // within a certain timeframe before actually randomising.
            if (DateTime.UtcNow.Subtract(_timeButtonPressed).TotalSeconds > _confirmInterval)
            {
                LogHandler.MainMenuMessage("Are you sure you wish to re-randomise all recipes?");
                LogHandler.MainMenuMessage("Press the button again to proceed.");
            }
            else
            {
                LogHandler.MainMenuMessage("Randomising...");
                InitMod.Randomise();
                LogHandler.MainMenuMessage("Finished randomising!");
            }

            _timeButtonPressed = DateTime.UtcNow;
        }

        // Way down here since it tends to take up some space and scrolling is annoying.
        public string sBase64Seed = "";
    }
}
