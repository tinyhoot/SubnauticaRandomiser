using System;
using System.Collections.Generic;
using NUnit.Framework;
using SubnauticaRandomiser;
using SubnauticaRandomiser.Objects;

namespace Tests.UnitTests
{
    [TestFixture]
    public class RandomHandlerTest
    {
        [Test]
        public void TestChoice()
        {
            List<TechType> list = new List<TechType>()
            {
                TechType.Titanium,
                TechType.HydrochloricAcid,
                TechType.Battery,
                TechType.Seaglide,
                TechType.Diamond
            };
            RandomHandler rh = new RandomHandler();
            TechType choice = rh.Choice(list);
            Assert.AreNotEqual(choice, default(TechType));
            Assert.Contains(choice, list);
        }

        [TestCase(typeof(TechType))]
        [TestCase(typeof(LogicEntity))]
        public void TestChoice_Empty<T>(T type)
        {
            List<T> list = new List<T>();
            RandomHandler rh = new RandomHandler();
            Assert.AreEqual(default(T), rh.Choice(list));
        }
        
        [TestCase(typeof(TechType))]
        [TestCase(typeof(LogicEntity))]
        public void TestChoice_Null<T>(T type)
        {
            List<T> list = null;
            RandomHandler rh = new RandomHandler();
            Assert.AreEqual(default(T), rh.Choice(list));
        }
    }
}