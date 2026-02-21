using System;
using System.Collections.Generic;
using Nautilus.Crafting;
using Nautilus.Handlers;
using Newtonsoft.Json;

namespace SubnauticaRandomiser.Objects
{
    /// <summary>
    /// A wrapper for the game's TechData class to make it serializable.
    /// </summary>
    [Serializable]
    public class Recipe
    {
        public TechType TechType;
        public List<Ingredient> Ingredients = new List<Ingredient>();
        public List<TechType> LinkedIngredients = new List<TechType>();
        public int CraftAmount = 1;

        /// <summary>
        /// This constructor exists primarily to make it easier for JSON to serialise this class.
        /// </summary>
        [JsonConstructor]
        public Recipe()
        {
        }

        public Recipe(TechType type)
        {
            TechType = type;
        }

        public Recipe(TechType type, List<Ingredient> ingredients, List<TechType> linkedIngredients, int craftAmount)
        {
            TechType = type;
            Ingredients = ingredients;
            LinkedIngredients = linkedIngredients;
            CraftAmount = craftAmount;
        }

        /// <summary>
        /// Copy information on linked items from the base recipe already loaded by the game.
        /// </summary>
        public void CopyVanillaData()
        {
            RecipeData techdata = CraftDataHandler.GetRecipeData(TechType);
            if (techdata == null)
                return;
            
            if (techdata.Ingredients != null && techdata.ingredientCount > 0)
            {
                foreach (Ingredient i in techdata.Ingredients)
                {
                    Ingredients.Add(new Ingredient(i.techType, i.amount));
                }
            }

            if (techdata.LinkedItems != null && techdata.linkedItemCount > 0)
                LinkedIngredients = techdata.LinkedItems;

            CraftAmount = techdata.craftAmount;
        }

        public RecipeData ToRecipeData()
        {
            var recipe = new RecipeData(Ingredients)
            {
                LinkedItems = LinkedIngredients.ShallowCopy(),
                craftAmount = CraftAmount
            };
            return recipe;
        }
    }
}
