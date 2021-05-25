using System;
using System.Collections.Generic;
using SMLHelper.V2.Crafting;
using SMLHelper.V2.Handlers;
namespace SubnauticaRandomiser
{
    [Serializable]
    public class Recipe : ITechData
    {
        public TechType ItemType;
        public List<RandomiserIngredient> Ingredients;
        public List<TechType> LinkedIngredients;
        public ETechTypeCategory Category;
        public int DepthDifficulty = 0;
        public List<TechType> Prerequisites;
        public int CraftAmount = 1;

        public int craftAmount { get { return CraftAmount; } }
        public int ingredientCount { get { return Ingredients.Count; } }
        public int linkedItemCount { get { return LinkedIngredients.Count; } }

        public Recipe(TechType type, ETechTypeCategory category, int depthDifficulty = 0, List<TechType> prereqs = null, int craftAmount = 1)
        {
            CraftAmount = craftAmount;
            DepthDifficulty = depthDifficulty;

            ItemType = type;
            Ingredients = new List<RandomiserIngredient>();
            //LinkedIngredients = new List<RandomiserIngredient>();

            // This part copies over information on linked items from the base
            // recipe already loaded by the game. Raw materials do not have any
            // recipes to load this data from.
            TechData techdata = CraftDataHandler.GetTechData(type);
            if (!category.Equals(ETechTypeCategory.RawMaterials) && techdata.linkedItemCount > 0)
            {
                LinkedIngredients = techdata.LinkedItems;
            }

            Category = category;
            Prerequisites = prereqs;
        }

        public IIngredient GetIngredient(int index)
        {
            return Ingredients[index].ToIIngredient();
        }

        public TechType GetLinkedItem(int index)
        {
            return (TechType)LinkedIngredients[index];
        }

        public override string ToString()
        {
            string result;
            string separator = ",";

            result = ItemType.AsString() + separator;

            if (ingredientCount != 0)
            {
                foreach (RandomiserIngredient i in Ingredients)
                {
                    result += i.TechType + ":" + i.Amount + ";";
                }
                result += separator;
            }

            result += Category.ToString() + separator;

            result += DepthDifficulty + separator;

            if (Prerequisites.Count != 0)
            {
                foreach (TechType pre in Prerequisites)
                {
                    result += pre.AsString() + ";";
                }
                result += separator;
            }

            result += CraftAmount;

            return result;
        }
    }
}
