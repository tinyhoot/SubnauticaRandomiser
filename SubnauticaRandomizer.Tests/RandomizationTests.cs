using Microsoft.VisualStudio.TestTools.UnitTesting;
using SubnauticaRandomizer.Randomizer;
using System;
using System.Linq;

namespace SubnauticaRandomizer.Tests
{
    [TestClass]
    public class RandomizationTests
    {
        [TestMethod]
        public void BatteriesContainThreeIngredientsTest()
        {
            var recipes = RecipeRandomizer.Randomize(Environment.CurrentDirectory);

            Assert.IsNotNull(recipes);
            Assert.IsTrue(recipes.RecipesByType.ContainsKey((int)TechType.Battery));
            Assert.IsTrue(recipes.RecipesByType[(int)TechType.Battery].Ingredients.Sum(i => i.Amount) == 3);
        }
    }
}
