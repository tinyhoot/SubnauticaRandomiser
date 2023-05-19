using System;
using System.Collections.Generic;
using Nautilus.Crafting;
using Nautilus.Handlers;

namespace SubnauticaRandomiser.Objects
{
    /// <summary>
    /// A wrapper for the game's TechData class to make it serializable.
    /// </summary>
    [Serializable]
    public class Recipe : ITechData
    {
        public TechType TechType;
        public List<RandomiserIngredient> Ingredients;
        public List<TechType> LinkedIngredients;
        public int CraftAmount;

        public int craftAmount { get { return CraftAmount; } }
        public int ingredientCount { get { return Ingredients.Count; } }
        public int linkedItemCount { get { return LinkedIngredients.Count; } }

        public Recipe(TechType type)
        {
            CraftAmount = 1;

            TechType = type;
            Ingredients = new List<RandomiserIngredient>();
            LinkedIngredients = new List<TechType>();

            // This part copies over information on linked items from the base
            // recipe already loaded by the game.
            RecipeData techdata = CraftDataHandler.GetRecipeData(type);
            if (techdata != null)
            {
                if (techdata.Ingredients != null && techdata.ingredientCount > 0)
                {
                    foreach (CraftData.Ingredient i in techdata.Ingredients)
                    {
                        Ingredients.Add(new RandomiserIngredient(i.techType, i.amount));
                    }
                }

                if (techdata.LinkedItems != null && techdata.linkedItemCount > 0)
                {
                    LinkedIngredients = techdata.LinkedItems;
                }

                CraftAmount = techdata.craftAmount;
            }
        }

        public Recipe(TechType type, List<RandomiserIngredient> ingredients, List<TechType> linkedIngredients, int craftAmount)
        {
            TechType = type;
            Ingredients = ingredients;
            LinkedIngredients = linkedIngredients;
            CraftAmount = craftAmount;
        }

        public IIngredient GetIngredient(int index)
        {
            return Ingredients[index];
        }

        public TechType GetLinkedItem(int index)
        {
            return LinkedIngredients[index];
        }
    }
}
