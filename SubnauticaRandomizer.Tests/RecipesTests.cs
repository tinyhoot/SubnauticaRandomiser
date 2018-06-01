using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace SubnauticaRandomizer.Tests
{
    [TestClass]
    public class RecipesTests
    {
        [TestMethod]
        public void SerializationDoesntMutate()
        {
            var recipes = new Recipes();
            var recipe = new Recipe();
            recipe.Ingredients = new List<GeneratedRecipeIngredient>() {
                new GeneratedRecipeIngredient
                {
                    Amount = 1,
                    TechType = (int)TechType.Quartz
                }
            };
            recipes.RecipesByType[(int)TechType.Bleach] = recipe;

            var recipeSeed = recipes.ToBase64String();
            var otherRecipes = Recipes.FromBase64String(recipeSeed);

            Assert.AreEqual(recipes.RecipesByType[(int)TechType.Bleach].ingredientCount, otherRecipes.RecipesByType[(int)TechType.Bleach].ingredientCount);
        }
    }
}
