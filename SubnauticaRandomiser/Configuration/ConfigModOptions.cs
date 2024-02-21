using System.Collections.Generic;
using HootLib;
using HootLib.Configuration;
using Nautilus.Options;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = System.Random;

namespace SubnauticaRandomiser.Configuration
{
    /// <summary>
    /// Handles how the mod presents itself in the in-game menu.
    /// </summary>
    internal class ConfigModOptions : HootModOptions
    {
        public ConfigModOptions(string name, Config config) : base(name, config) { }

        public override void BuildModOptions(uGUI_TabbedControlsPanel panel, int modsTabIndex,
            IReadOnlyCollection<OptionItem> options)
        {
            // If this is not the main menu, replace the entire menu with a warning and exit immediately.
            // The menu should never be accessible from in-game.
            if (!IsMainMenu(panel))
            {
                panel.AddHeading(modsTabIndex, Initialiser.NAME);
                AddText("The settings for this mod can only be accessed from the main menu.");
                return;
            }

            base.BuildModOptions(panel, modsTabIndex, options);
        }
    }
}