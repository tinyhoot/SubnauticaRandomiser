using System;
using System.Collections.Generic;
using System.Linq;
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
        public List<RandomiserIngredient> Ingredients;
        public List<TechType> LinkedIngredients;
        public int CraftAmount;

        /// <summary>
        /// This constructor exists primarily to make it easier for JSON to serialise this class.
        /// </summary>
        [JsonConstructor]
        public Recipe()
        {
            Ingredients = new List<RandomiserIngredient>();
            LinkedIngredients = new List<TechType>();
        }

        public Recipe(TechType type)
        {
            CraftAmount = 1;

            TechType = type;
            Ingredients = new List<RandomiserIngredient>();
            LinkedIngredients = new List<TechType>();
        }

        public Recipe(TechType type, List<RandomiserIngredient> ingredients, List<TechType> linkedIngredients, int craftAmount)
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
                    Ingredients.Add(new RandomiserIngredient(i.techType, i.amount));
                }
            }

            if (techdata.LinkedItems != null && techdata.linkedItemCount > 0)
                LinkedIngredients = techdata.LinkedItems;

            CraftAmount = techdata.craftAmount;
        }

        public RecipeData ToRecipeData()
        {
            var recipe = new RecipeData(Ingredients.Select(i => i.ToGameIngredient()).ToList())
            {
                LinkedItems = LinkedIngredients.ShallowCopy(),
                craftAmount = CraftAmount
            };
            return recipe;
        }
    }
}
