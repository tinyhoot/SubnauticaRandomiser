using System;
using System.Collections.Generic;
using System.Linq;

namespace SubnauticaRandomizer.Randomizer
{
    [Serializable]
    public class SerializedRecipesInformation
    {
        public string ReadMe = "This file was generated via a csv. It is not readable, dont try to edit this directly! See GitHub for more info.";
        public List<SerializedRecipeInformation> Recipes;
    }

    [Serializable]
    public class SerializedRecipeInformation
    {
        public int Type;
        public List<int> RequiredIngredients;
        public int DepthDifficulty;
        public string Category;
        public List<int> RestrictedTools;
        public List<int> RandomizeDifficulty;
        public int? Quantity;

        public RecipeInformation ConvertTo()
        {
            return new RecipeInformation
            {
                Type = (TechType)Type,
                RequiredIngredients = RequiredIngredients.Select(ri => (TechType)ri).ToList(),
                DepthDifficulty = DepthDifficulty,
                Category = Category,
                RestrictedTools = RestrictedTools.Select(rt => (TechType)rt).ToList(),
                RandomizeDifficulty = RandomizeDifficulty,
                Quantity = Quantity
            };
        }

        public static SerializedRecipeInformation ConvertFrom(RecipeInformation recipeInformation)
        {
            return new SerializedRecipeInformation
            {
                Type = (int)recipeInformation.Type,
                RequiredIngredients = recipeInformation.RequiredIngredients.Select(ri => (int)ri).ToList(),
                DepthDifficulty = recipeInformation.DepthDifficulty,
                Category = recipeInformation.Category,
                RestrictedTools = recipeInformation.RestrictedTools.Select(rt => (int)rt).ToList(),
                RandomizeDifficulty = recipeInformation.RandomizeDifficulty,
                Quantity = recipeInformation.Quantity
            };
        }
    }

}
