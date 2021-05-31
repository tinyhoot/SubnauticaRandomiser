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
        public ETechTypeCategory Category;
        public int Depth;
        public List<TechType> Prerequisites;
        public int CraftAmount;
        public int Value;
        public Blueprint Blueprint;

        public int craftAmount { get { return CraftAmount; } }
        public int ingredientCount { get { return Ingredients.Count; } }
        public int linkedItemCount { get { return LinkedIngredients.Count; } }

        public Recipe(TechType type, ETechTypeCategory category, int depth = 0, List<TechType> prereqs = null, int value = 0, Blueprint blueprint = null)
        {
            CraftAmount = 1;
            Value = value;
            Depth = depth;

            TechType = type;
            Ingredients = new List<RandomiserIngredient>();
            LinkedIngredients = new List<TechType>();

            // This part copies over information on linked items from the base
            // recipe already loaded by the game. Raw materials do not have any
            // recipes to load this data from and *will* cause a NullReference.
            TechData techdata = CraftDataHandler.GetTechData(type);
            if (!category.Equals(ETechTypeCategory.RawMaterials) && !category.Equals(ETechTypeCategory.Fish) && !category.Equals(ETechTypeCategory.Seeds) && !category.Equals(ETechTypeCategory.Eggs))
            {
                if (techdata.ingredientCount > 0)
                {
                    foreach (Ingredient i in techdata.Ingredients)
                    {
                        Ingredients.Add(new RandomiserIngredient(i.techType, i.amount));
                    }
                }

                if (techdata.linkedItemCount > 0)
                {
                    LinkedIngredients = techdata.LinkedItems;
                }
            }

            Category = category;
            Prerequisites = prereqs;
            Blueprint = blueprint;
        }

        public IIngredient GetIngredient(int index)
        {
            return Ingredients[index];
        }

        public TechType GetLinkedItem(int index)
        {
            return LinkedIngredients[index];
        }

        public override string ToString()
        {
            string result;
            string separator = ",";

            result = TechType.AsString() + separator;

            if (ingredientCount != 0)
            {
                foreach (RandomiserIngredient i in Ingredients)
                {
                    result += i.techType + ":" + i.amount + ";";
                }
                result += separator;
            }

            result += Category.ToString() + separator;

            result += Depth + separator;

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
