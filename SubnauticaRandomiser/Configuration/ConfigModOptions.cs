using System.Collections.Generic;
using System.Linq;
using System.Text;
using HootLib.Configuration;
using Nautilus.Options;
using SubnauticaRandomiser.Logic;
using UnityEngine;

namespace SubnauticaRandomiser.Configuration
{
    /// <summary>
    /// Handles how the mod presents itself in the in-game menu.
    /// </summary>
    internal class ConfigModOptions : HootModOptions
    {
        public ConfigModOptions(string name, Config config, Transform persistentParent) : base(name, config, persistentParent) { }

        public override void BuildModOptions(uGUI_TabbedControlsPanel panel, int modsTabIndex,
            IReadOnlyCollection<OptionItem> options)
        {
            // If this is not the main menu, replace the entire menu with a warning and exit immediately.
            // The menu should never be accessible from in-game.
            if (!IsMainMenu(panel))
            {
                Transform optionsPane = FindModOptionsPane(panel, modsTabIndex);
                
                panel.AddHeading(modsTabIndex, Initialiser.NAME);
                new TextDecorator("The settings for this mod can only be accessed from the main menu.").AddToPanel(optionsPane);
                new TextDecorator($"Loaded modules:\n{CreateLoadedModuleList()}").AddToPanel(optionsPane);
                return;
            }

            base.BuildModOptions(panel, modsTabIndex, options);
        }

        /// <summary>
        /// Create a bullet point list of all currently active modules for display in a running game.
        /// </summary>
        private string CreateLoadedModuleList()
        {
            // Get the names of all loaded modules and sort them alphabetically.
            var modules = Bootstrap.Main.GetActiveModuleTypes()
                .Select(type => type.Name)
                .OrderBy(str => str);
            StringBuilder sb = new StringBuilder();
            foreach (var module in modules)
            {
                // Add a bullet point to the beginning of the line.
                sb.Append(" \u2022  ");
                sb.AppendLine(module);
            }

            return sb.ToString();
        }
    }
}