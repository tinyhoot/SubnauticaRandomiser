using System.Collections.Generic;
using Nautilus.Options;
using UnityEngine;
using UnityEngine.UI;

namespace SubnauticaRandomiser.Configuration
{
    internal class ConfigModOptions : ModOptions
    {
        public static ConfigModOptions Instance;
        
        private Transform _modOptionsPane;
        private GameObject _separator;
        private List<string> _addSeparatorBefore = new List<string>
        {
            Initialiser._Config.RandomiseDataboxes.GetId(),
            Initialiser._Config.EnableFragmentModule.GetId(),
            Initialiser._Config.EnableRecipeModule.GetId(),
        };
        
        public ConfigModOptions(string name) : base(name)
        {
            // Needed so that individual options can access the list of all other ones.
            Instance = this;

            AddItem(Initialiser._Config.EnableAlternateStartModule.ToModToggleOption());
            AddItem(Initialiser._Config.SpawnPoint.ToModChoiceOption());

            AddItem(Initialiser._Config.RandomiseDataboxes.ToModToggleOption());
            AddItem(Initialiser._Config.RandomiseDoorCodes.ToModToggleOption());
            AddItem(Initialiser._Config.RandomiseSupplyBoxes.ToModToggleOption());

            AddItem(Initialiser._Config.EnableFragmentModule.ToModToggleOption());
            AddItem(Initialiser._Config.RandomiseFragments.ToModToggleOption());
            AddItem(Initialiser._Config.RandomiseNumFragments.ToModToggleOption());
            AddItem(Initialiser._Config.MaxFragmentsToUnlock.ToModSliderOption(1, 20));
            AddItem(Initialiser._Config.MaxBiomesPerFragment.ToModSliderOption(3, 10));
            AddItem(Initialiser._Config.RandomiseDuplicateScans.ToModToggleOption());

            AddItem(Initialiser._Config.EnableRecipeModule.ToModToggleOption());
            AddItem(Initialiser._Config.RandomiseRecipes.ToModToggleOption());
            AddItem(Initialiser._Config.RecipeMode.ToModChoiceOption());
            AddItem(Initialiser._Config.UseFish.ToModToggleOption());
            AddItem(Initialiser._Config.UseSeeds.ToModToggleOption());
            AddItem(Initialiser._Config.UseEggs.ToModToggleOption());
            AddItem(Initialiser._Config.DiscoverEggs.ToModToggleOption());
            AddItem(Initialiser._Config.EquipmentAsIngredients.ToModChoiceOption());
            AddItem(Initialiser._Config.ToolsAsIngredients.ToModChoiceOption());
            AddItem(Initialiser._Config.UpgradesAsIngredients.ToModChoiceOption());
            AddItem(Initialiser._Config.VanillaUpgradeChains.ToModToggleOption());
            AddItem(Initialiser._Config.BaseTheming.ToModToggleOption());
            AddItem(Initialiser._Config.MaxNumberPerIngredient.ToModSliderOption(1, 10));
            AddItem(Initialiser._Config.MaxIngredientsPerRecipe.ToModSliderOption(1, 10));
        }

        public override void BuildModOptions(uGUI_TabbedControlsPanel panel, int modsTabIndex, IReadOnlyCollection<OptionItem> options)
        {
            // Reset the options pane reference to avoid linking to a gameobject that no longer exists.
            _modOptionsPane = null;
            
            panel.AddHeading(modsTabIndex, Initialiser.NAME);

            foreach (var option in options)
            {
                if (_addSeparatorBefore.Contains(option.Id))
                    AddSeparator(panel, modsTabIndex);
                option.AddToPanel(panel, modsTabIndex);
            }
            
            Initialiser._Config.EnableAlternateStartModule.UpdateControlledOptions(options);
            Initialiser._Config.EnableFragmentModule.UpdateControlledOptions(options);
            Initialiser._Config.EnableRecipeModule.UpdateControlledOptions(options);
        }

        private void AddSeparator(uGUI_TabbedControlsPanel panel, int modsTabIndex)
        {
            _modOptionsPane ??= panel.transform.Find("Middle/PanesHolder").GetChild(modsTabIndex).Find("Viewport/Content");
            _separator ??= CreateSeparator(panel);
            GameObject.Instantiate(_separator, _modOptionsPane, false);
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
    }
}