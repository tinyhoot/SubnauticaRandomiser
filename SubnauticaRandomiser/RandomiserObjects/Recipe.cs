using System;
using System.Collections.Generic;
using SMLHelper.V2.Crafting;
using SMLHelper.V2.Handlers;

namespace SubnauticaRandomiser
{
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
            TechData techdata = CraftDataHandler.GetTechData(type);
            if (techdata != null)
            {
                if (techdata.Ingredients != null && techdata.ingredientCount > 0)
                {
                    foreach (Ingredient i in techdata.Ingredients)
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
