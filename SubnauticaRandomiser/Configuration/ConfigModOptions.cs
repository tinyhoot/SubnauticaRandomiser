using System.Collections.Generic;
using Nautilus.Options;

namespace SubnauticaRandomiser.Configuration
{
    internal class ConfigModOptions : ModOptions
    {
        public ConfigModOptions(string name) : base(name)
        {
            AddItem(Initialiser._Config.SpawnPoint.ToModChoiceOption());
            AddItem(Initialiser._Config.RandomiseDoorCodes.ToModToggleOption());
            AddItem(Initialiser._Config.RandomiseSupplyBoxes.ToModToggleOption());
            
            AddItem(Initialiser._Config.RandomiseDataboxes.ToModToggleOption());

            AddItem(Initialiser._Config.RandomiseFragments.ToModToggleOption());
            AddItem(Initialiser._Config.RandomiseNumFragments.ToModToggleOption());
            AddItem(Initialiser._Config.MaxFragmentsToUnlock.ToModSliderOption(1, 20));
            AddItem(Initialiser._Config.MaxBiomesPerFragment.ToModSliderOption(3, 10));
            AddItem(Initialiser._Config.RandomiseDuplicateScans.ToModToggleOption());
            
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
            panel.AddHeading(modsTabIndex, Initialiser.NAME);
            foreach (var option in options)
            {
                option.AddToPanel(panel, modsTabIndex);
            }
        }
    }
}