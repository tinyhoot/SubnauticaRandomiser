using System;
using System.Collections.Generic;

namespace SubnauticaRandomizer
{
    [Serializable]
    public class Recipe : ITechData
    {
        public int CraftAmount = 1;
        public List<GeneratedRecipeIngredient> Ingredients = new List<GeneratedRecipeIngredient>();
        public List<GeneratedRecipeIngredient> LinkedIngredients = new List<GeneratedRecipeIngredient>();

        public int craftAmount { get { return CraftAmount; } }
        public int ingredientCount {  get { return Ingredients.Count; } }
        public int linkedItemCount {  get { return LinkedIngredients.Count; } }

        public IIngredient GetIngredient(int index)
        {
            return Ingredients[index].ToIIngredient();
        }

        public TechType GetLinkedItem(int index)
        {
            return (TechType)LinkedIngredients[index].TechType;
        }
    }
}
