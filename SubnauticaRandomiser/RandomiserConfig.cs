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
        public int iSeed = 0;

        [Choice("Mode", "Balanced", "True Random")]
        public int iRandomiserMode = 0;

        [Toggle("Use fish in logic?")]
        public bool bUseFish = true;

        [Toggle("Use eggs in logic?")]
        public bool bUseEggs = false;

        [Toggle("Use seeds in logic?")]
        public bool bUseSeeds = true;

        [Toggle("Randomise blueprints in databoxes?")]
        public bool bRandomiseDataboxes = true;

        [Choice("Include equipment as ingredients?", "Never", "Top-level recipes only", "Unrestricted")]
        public int iEquipmentAsIngredients = 1;

        [Choice("Include tools as ingredients?", "Never", "Top-level recipes only", "Unrestricted")]
        public int iToolsAsIngredients = 1;

        [Choice("Include upgrades as ingredients?", "Never", "Top-level recipes only", "Unrestricted")]
        public int iUpgradesAsIngredients = 1;

        [Button("Randomise with new seed")]
        public void NewRandomNewSeed()
        {
            // Re-randomising everything is a serious request, and it should not
            // happen accidentally. This here ensures the button is pressed twice
            // within a certain timeframe before actually randomising.
            if (EnsureButtonTime())
            {
                Random ran = new Random();
                iSeed = ran.Next();
                LogHandler.MainMenuMessage("Changed seed to " + iSeed);
                LogHandler.MainMenuMessage("Randomising...");
                InitMod.Randomise();
                LogHandler.MainMenuMessage("Finished randomising!");
            }
            else
            {
                LogHandler.MainMenuMessage("Are you sure you wish to re-randomise all recipes?");
                LogHandler.MainMenuMessage("Press the button again to proceed.");
            }
        }

        [Button("Randomise with same seed")]
        public void NewRandomOldSeed()
        {
            LogHandler.MainMenuMessage("Randomising...");
            Load();
            InitMod.Randomise();
            LogHandler.MainMenuMessage("Finished randomising!");
        }

        public string ADVANCED_SETTINGS_BELOW_THIS_POINT = "ADVANCED_SETTINGS_BELOW_THIS_POINT";
        public int iDepthSearchTime = 15;
        public int iMaxAmountPerIngredient = 5;
        public int iMaxEggsAsSingleIngredient = 1;
        public int iMaxInventorySizePerRecipe = 24;
        public double dFuzziness = 0.2;
        public double dIngredientRatio = 0.5;

        // Way down here since it tends to take up some space and scrolling is annoying.
        public string sBase64Seed = "";
        public int iSaveVersion = InitMod.s_expectedSaveVersion;

        public void SanitiseConfigValues()
        {
            if (iRandomiserMode > 1 || iRandomiserMode < 0)
                iRandomiserMode = 0;
            if (iToolsAsIngredients > 2 || iToolsAsIngredients < 0)
                iToolsAsIngredients = 1;
            if (iUpgradesAsIngredients > 2 || iUpgradesAsIngredients < 0)
                iUpgradesAsIngredients = 1;
            if (iDepthSearchTime > 45 || iDepthSearchTime < 0)
                iDepthSearchTime = 15;
            if (iMaxAmountPerIngredient > 20 || iMaxAmountPerIngredient < 1)
                iMaxAmountPerIngredient = 5;
            if (iMaxEggsAsSingleIngredient > 10 || iMaxEggsAsSingleIngredient < 1)
                iMaxEggsAsSingleIngredient = 1;
            if (iMaxInventorySizePerRecipe > 100 || iMaxInventorySizePerRecipe < 4)
                iMaxInventorySizePerRecipe = 24;
            if (dFuzziness > 1 || dFuzziness < 0)
                dFuzziness = 0.2;
            if (dIngredientRatio > 1 || dIngredientRatio < 0)
                dIngredientRatio = 0.5;
        }

        private bool EnsureButtonTime()
        {
            // Re-randomising everything is a serious request, and it should not
            // happen accidentally. This here ensures the button is pressed twice
            // within a certain timeframe before actually randomising.
            if (DateTime.UtcNow.Subtract(_timeButtonPressed).TotalSeconds < _confirmInterval)
            {
                _timeButtonPressed = DateTime.MinValue;
                return true;
            }

            _timeButtonPressed = DateTime.UtcNow;
            return false;
        }
    }
}
