using System;
using NUnit.Framework;
using SubnauticaRandomiser.Objects;
using SubnauticaRandomiser.Objects.Enums;

namespace Tests.UnitTests.Objects
{
    [TestFixture]
    public class BiomeCollectionTest
    {
        private BiomeCollection _bc;
        
        [SetUp]
        public void Init()
        {
            _bc = new BiomeCollection(SubnauticaRandomiser.Objects.Enums.BiomeType.Kelp);
        }
        
        [Test]
        public void TestAdd()
        {
            Biome biome = new Biome("test", SubnauticaRandomiser.Objects.Enums.BiomeType.Kelp, 0, 0);
            Assert.True(_bc.Add(biome));
        }
        
        [Test]
        public void TestAdd_Duplicate()
        {
            Biome biome = new Biome("test", SubnauticaRandomiser.Objects.Enums.BiomeType.Kelp, 0, 0);
            _bc.Add(biome);
            Assert.False(_bc.Add(biome));
        }
        
        [Test]
        public void TestAdd_Duplicate_Name()
        {
            Biome biome1 = new Biome("test", SubnauticaRandomiser.Objects.Enums.BiomeType.Kelp, 0, 0);
            Biome biome2 = new Biome("test", SubnauticaRandomiser.Objects.Enums.BiomeType.Kelp, 0, 0);
            _bc.Add(biome1);
            Assert.False(_bc.Add(biome2));
        }
        
        [Test]
        public void TestAdd_Null()
        {
            Assert.Throws<ArgumentNullException>(() => _bc.Add(null));
        }
    }
}