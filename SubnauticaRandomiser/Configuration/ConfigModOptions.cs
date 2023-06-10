using System.Collections.Generic;
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
    internal class ConfigModOptions : ModOptions
    {
        public static ConfigModOptions Instance;
        
        private Config _config;
        private Transform _modOptionsPane;
        private GameObject _separator;
        private List<string> _addSeparatorBefore = new List<string>
        {
            Initialiser._Config.RandomiseDataboxes.GetId(),
            Initialiser._Config.EnableFragmentModule.GetId(),
            Initialiser._Config.EnableRecipeModule.GetId(),
        };
        
        public ConfigModOptions(string name, Config config) : base(name)
        {
            // Needed so that individual options can access the list of all other ones.
            Instance = this;
            _config = config;
            
            AddItem(ModButtonOption.Create("button_randomise", "Randomise!", RandomiseNewSeed,
                tooltip: "Apply your config changes and randomise. Restart your game afterwards!"));
            AddItem(ModButtonOption.Create("button_randomiseFromConfig", "Apply config from disk", RandomiseFromConfig, 
                tooltip: "If someone else gave you their config file, click here to load it. Restart your game afterwards!"));

            AddItem(_config.EnableAlternateStartModule.ToModToggleOption());
            AddItem(_config.SpawnPoint.ToModChoiceOption());
            AddItem(_config.AllowRadiatedStarts.ToModToggleOption());

            AddItem(_config.RandomiseDataboxes.ToModToggleOption());
            AddItem(_config.RandomiseDoorCodes.ToModToggleOption());
            AddItem(_config.RandomiseSupplyBoxes.ToModToggleOption());

            AddItem(_config.EnableFragmentModule.ToModToggleOption());
            AddItem(_config.RandomiseFragments.ToModToggleOption());
            AddItem(_config.RandomiseNumFragments.ToModToggleOption());
            AddItem(_config.MaxFragmentsToUnlock.ToModSliderOption(1, 20));
            AddItem(_config.MaxBiomesPerFragment.ToModSliderOption(3, 10));
            AddItem(_config.RandomiseDuplicateScans.ToModToggleOption());

            AddItem(_config.EnableRecipeModule.ToModToggleOption());
            AddItem(_config.RandomiseRecipes.ToModToggleOption());
            AddItem(_config.RecipeMode.ToModChoiceOption());
            AddItem(_config.UseFish.ToModToggleOption());
            AddItem(_config.UseSeeds.ToModToggleOption());
            AddItem(_config.UseEggs.ToModToggleOption());
            AddItem(_config.DiscoverEggs.ToModToggleOption());
            AddItem(_config.EquipmentAsIngredients.ToModChoiceOption());
            AddItem(_config.ToolsAsIngredients.ToModChoiceOption());
            AddItem(_config.UpgradesAsIngredients.ToModChoiceOption());
            AddItem(_config.VanillaUpgradeChains.ToModToggleOption());
            AddItem(_config.BaseTheming.ToModToggleOption());
            AddItem(_config.MaxNumberPerIngredient.ToModSliderOption(1, 10));
            AddItem(_config.MaxIngredientsPerRecipe.ToModSliderOption(1, 10));
        }

        /// <summary>
        /// Constructs the in-game menu.
        /// </summary>
        public override void BuildModOptions(uGUI_TabbedControlsPanel panel, int modsTabIndex, IReadOnlyCollection<OptionItem> options)
        {
            // Reset the options pane reference to avoid linking to a gameobject that was destroyed.
            FindModOptionsPane(panel, modsTabIndex);
            _separator = null;
            
            panel.AddHeading(modsTabIndex, Initialiser.NAME);
            
            // If this is not the main menu, replace the entire menu with a warning and exit immediately.
            // The menu should never be accessible from in-game.
            if (!IsMainMenu(panel))
            {
                AddText("The settings for this mod can only be accessed from the main menu.");
                return;
            }
            
            AddText("After changing any options here you must <color=#FF0000FF>press the button below</color> or "
                    + "your changes will not do anything!");
            foreach (var option in options)
            {
                if (_addSeparatorBefore.Contains(option.Id))
                    AddSeparator(panel);
                option.AddToPanel(panel, modsTabIndex);
            }
            
            _config.EnableAlternateStartModule.UpdateControlledOptions(options);
            _config.EnableFragmentModule.UpdateControlledOptions(options);
            _config.EnableRecipeModule.UpdateControlledOptions(options);
        }

        private void AddSeparator(uGUI_TabbedControlsPanel panel)
        {
            _separator ??= CreateSeparator(panel);
            GameObject.Instantiate(_separator, _modOptionsPane, false);
        }
        
        /// <summary>
        /// Add a pure text label without attachment to any ModOptions to the menu.
        /// </summary>
        private void AddText(string text, float fontSize = 30f)
        {
            var textObject = new GameObject("Text Label");
            textObject.transform.SetParent(_modOptionsPane, false);
            var textMesh = textObject.AddComponent<TextMeshProUGUI>();
            textMesh.autoSizeTextContainer = true;
            textMesh.fontSize = fontSize;
            textMesh.enableWordWrapping = true;
            textMesh.overflowMode = TextOverflowModes.Overflow;
            textMesh.text = text;
        }
        
        /// <summary>
        /// Creates a GameObject which can act as a visual separator in the options menu.
        /// </summary>
        private GameObject CreateSeparator(uGUI_TabbedControlsPanel panel)
        {
            GameObject separator = new GameObject("OptionSeparator");
            separator.layer = 5;
            Transform panesHolder = panel.transform.Find("Middle/PanesHolder");

            LayoutElement layout = separator.EnsureComponent<LayoutElement>();
            layout.minHeight = 40;
            layout.preferredHeight = 40;
            layout.minWidth = -1;

            // Putting the image into its own child object prevents weird layout issues.
            GameObject background = new GameObject("Background");
            background.transform.SetParent(separator.transform, false);
            // Get the image from one of the sliders in the "General" tab.
            Image image = background.EnsureComponent<Image>();
            // SliderOption sprite - very nice, but very yellow
            Sprite sprite = panesHolder.GetChild(0).Find("Viewport/Content/uGUI_SliderOption(Clone)/Slider/Slider/Background").GetComponent<Image>().sprite;
            //image.sprite = panesHolder.GetChild(0).Find("Scrollbar Vertical/Sliding Area/Handle").GetComponent<Image>().sprite;
            // The size of the image gameobject will auto-adjust to the image. This fixes it.
            float targetWidth = panel.transform.GetComponent<RectTransform>().rect.width * 0.67f;
            background.transform.localScale = new Vector3(targetWidth / background.GetComponent<RectTransform>().rect.width, 0.4f, 1f);
            
            // Change the colour of the nabbed sprite.
            image.sprite = Utils.RecolourSprite(sprite, new Color(0.4f, 0.7f, 0.9f));

            return separator;
        }

        private void FindModOptionsPane(uGUI_TabbedControlsPanel panel, int modsTabIndex)
        {
            _modOptionsPane = panel.transform.Find("Middle/PanesHolder").GetChild(modsTabIndex).Find("Viewport/Content");
        }

        /// <summary>
        /// Check whether this panel is part of the main menu.
        /// </summary>
        private bool IsMainMenu(uGUI_TabbedControlsPanel panel)
        {
            return panel.GetComponentInParent<uGUI_MainMenu>() != null;
        }

        private void RandomiseNewSeed(ButtonClickedEventArgs args)
        {
            Random random = new Random();
            int seed = random.Next();
            _config.Seed.Entry.Value = seed;
            Initialiser._Log.InGameMessage("Changed seed to " + seed);
            Initialiser._Log.InGameMessage("Randomising...");
            Initialiser._Main.RandomiseFromConfig();
        }

        private void RandomiseFromConfig(ButtonClickedEventArgs args)
        {
            Initialiser._Log.InGameMessage("Randomising...");
            // Ensure all manual changes to the config file are loaded.
            _config.Reload();
            Initialiser._Main.RandomiseFromConfig();
        }
    }
}