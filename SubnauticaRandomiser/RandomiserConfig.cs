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

        [Choice("Mode", "Default", "True Random")]
        public int iRandomiserMode = 0;

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
                _timeButtonPressed = DateTime.MinValue;
                return;
            }

            _timeButtonPressed = DateTime.UtcNow;
        }

        public double dFuzziness = 0.2;
        public double dIngredientRatio = 0.5;
        // Way down here since it tends to take up some space and scrolling is annoying.
        public string sBase64Seed = "";

        public void SanitiseConfigValues()
        {
            if (iRandomiserMode > 1 || iRandomiserMode < 0)
                iRandomiserMode = 0;
            if (dFuzziness > 1 || dFuzziness < 0)
                dFuzziness = 0.2;
            if (dIngredientRatio > 1 || dIngredientRatio < 0)
                dIngredientRatio = 0.5;
        }
    }
}
