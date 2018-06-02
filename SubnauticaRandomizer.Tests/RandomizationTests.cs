using Microsoft.VisualStudio.TestTools.UnitTesting;
using SubnauticaRandomizer.Randomizer;
using System;

namespace SubnauticaRandomizer.Tests
{
    [TestClass]
    public class RandomizationTests
    {
        [TestMethod]
        public void RandomizeTest()
        {
            var recipes = RecipeRandomizer.Randomize(Environment.CurrentDirectory);

            Assert.IsNotNull(recipes);
            //Not sure what else to validate here...
        }
    }
}
