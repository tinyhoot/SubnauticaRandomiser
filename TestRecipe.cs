using System;
using System.Collections.Generic;
using SMLHelper.V2.Crafting;
using SMLHelper.V2.Handlers;
namespace SubnauticaRandomiser
{
    public class TestRecipe
    {
        int seed;

        public TestRecipe(int seed)
        {
            this.seed = seed;
        }

        public static void EditTitanite()
        {
            // Attempt to set recipe for computer chips to two titanium
            TechData replacement = new TechData();
            replacement.craftAmount = 1;
            replacement.Ingredients.Add(new Ingredient(TechType.Titanium, 2));
            
            CraftDataHandler.SetTechData(TechType.ComputerChip, replacement);
        }

        public static void EditRadiationSuit(RecipeDictionary d, List<Recipe> completeMaterialsList)
        {
            Recipe r = completeMaterialsList.Find(x => x.ItemType.Equals(TechType.RadiationSuit));
            LogHandler.Debug("Linked items of radiation suit: " + r.linkedItemCount);
            List<RandomiserIngredient> i = new List<RandomiserIngredient>();
            i.Add(new RandomiserIngredient((int)TechType.Titanium, 1));
            r.Ingredients = i;

            d.DictionaryInstance.Add((int)TechType.RadiationSuit, r);
            CraftDataHandler.SetTechData(TechType.RadiationSuit, r);
        }
    }
}
