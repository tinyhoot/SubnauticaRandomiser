using System.Collections.Generic;
using NUnit.Framework;
using SubnauticaRandomiser.Logic.Recipes;
using SubnauticaRandomiser.Objects;
using SubnauticaRandomiser.Objects.Enums;
using Tests.Mocks;

namespace Tests.UnitTests.Logic.Recipes
{
    [TestFixture]
    public class MaterialsTest
    {
        private List<LogicEntity> _allMaterials;
        private EntityHandler _mat;

        [SetUp]
        public void Init()
        {
            _allMaterials = new List<LogicEntity>
            {
                new LogicEntity(TechType.Titanium, ETechTypeCategory.RawMaterials) { AccessibleDepth = 0 },
                new LogicEntity(TechType.Gold, ETechTypeCategory.RawMaterials) { AccessibleDepth = 70 },
                new LogicEntity(TechType.Peeper, ETechTypeCategory.Fish){ AccessibleDepth = 0},
                new LogicEntity(TechType.Battery, ETechTypeCategory.Electronics){ AccessibleDepth = 100 },
                new LogicEntity(TechType.Aquarium, ETechTypeCategory.BaseInternalPieces){ AccessibleDepth = 100 },
                new LogicEntity(TechType.BluePalmSeed, ETechTypeCategory.Seeds, prerequisites: new List<TechType>(){TechType.Knife}),
                new LogicEntity(TechType.SeaCrownSeed, ETechTypeCategory.Seeds, prerequisites: new List<TechType>(){TechType.Knife}){ AccessibleDepth = 100},
                new LogicEntity(TechType.BeaconFragment, ETechTypeCategory.Fragments)
            };
            _mat = new EntityHandler(_allMaterials, new FakeLogger());
        }

        [TestCase(ETechTypeCategory.RawMaterials, 0, ExpectedResult = true)]
        [TestCase(ETechTypeCategory.RawMaterials, 50, ExpectedResult = true)]
        [TestCase(ETechTypeCategory.RawMaterials, 70, ExpectedResult = true)]
        [TestCase(ETechTypeCategory.RawMaterials, -1, ExpectedResult = false)]
        [TestCase(ETechTypeCategory.Fish, 0, ExpectedResult = true)]
        [TestCase(ETechTypeCategory.Eggs, 0, ExpectedResult = false)]
        public bool TestAddReachable_Category(ETechTypeCategory category, int maxDepth)
        {
            return _mat.AddToLogic(category, maxDepth);
        }

        [TestCase(new[]{ ETechTypeCategory.RawMaterials }, 0)]
        [TestCase(new[]{ ETechTypeCategory.RawMaterials }, 100)]
        [TestCase(new[]{ ETechTypeCategory.RawMaterials, ETechTypeCategory.Seeds }, 0)]
        [TestCase(new[]{ ETechTypeCategory.RawMaterials, ETechTypeCategory.Seeds }, 100)]
        [TestCase(new[]{ ETechTypeCategory.Fish, ETechTypeCategory.Equipment }, 100)]
        public void TestAddReachable_Categories(ETechTypeCategory[] categories, int maxDepth)
        {
            Assert.True(_mat.AddToLogic(categories, maxDepth));
        }
        
        [TestCase(new[]{ ETechTypeCategory.RawMaterials }, -1)]
        [TestCase(new[]{ ETechTypeCategory.Electronics }, 0)]
        [TestCase(new[]{ ETechTypeCategory.Electronics }, 0)]
        [TestCase(new[]{ ETechTypeCategory.AdvancedMaterials}, 100)]
        public void TestAddReachable_Categories_Fail(ETechTypeCategory[] categories, int maxDepth)
        {
            Assert.False(_mat.AddToLogic(categories, maxDepth));
        }

        [TestCase(TechType.Titanium, ETechTypeCategory.RawMaterials, ExpectedResult = true)]
        [TestCase(TechType.Titanium, ETechTypeCategory.BaseGenerators, ExpectedResult = true)]
        [TestCase(TechType.JeweledDiskPiece, ETechTypeCategory.RawMaterials, ExpectedResult = true)]
        [TestCase(TechType.JeweledDiskPiece, ETechTypeCategory.Eggs, ExpectedResult = true)]
        public bool TestAddReachable(TechType techType, ETechTypeCategory category)
        {
            LogicEntity entity = new LogicEntity(techType, category);
            return _mat.AddReachable(entity);
        }

        [Test]
        public void TestAddReachable_Twice()
        {
            LogicEntity entity = new LogicEntity(TechType.Titanium, ETechTypeCategory.RawMaterials);
            Assert.True(_mat.AddReachable(entity));
            Assert.False(_mat.AddReachable(entity));
        }
        
        [TestCase(ETechTypeCategory.RawMaterials, 100, TechType.Knife, ExpectedResult = false)]
        [TestCase(ETechTypeCategory.Seeds, 100, TechType.Knife, ExpectedResult = true)]
        [TestCase(ETechTypeCategory.Seeds, -1, TechType.Knife, ExpectedResult = false)]
        public bool TestAddReachableWithPrereqs(ETechTypeCategory category, int maxDepth, TechType prereq)
        {
            return _mat.AddToLogic(category, maxDepth, prereq);
        }

        [TestCase(ETechTypeCategory.RawMaterials, 100, TechType.Knife, ExpectedResult = true)]
        [TestCase(ETechTypeCategory.RawMaterials, -1, TechType.Knife, ExpectedResult = false)]
        [TestCase(ETechTypeCategory.Seeds, 100, TechType.Knife, ExpectedResult = false)]
        [TestCase(ETechTypeCategory.Seeds, -1, TechType.Knife, ExpectedResult = false)]
        public bool TestAddReachableWithPrereqs_Invert(ETechTypeCategory category, int maxDepth, TechType prereq)
        {
            return _mat.AddToLogic(category, maxDepth, prereq, invert: true);
        }
        
        [TestCase(new[]{ ETechTypeCategory.RawMaterials, ETechTypeCategory.Electronics }, 100, TechType.Knife, ExpectedResult = false)]
        [TestCase(new[]{ ETechTypeCategory.RawMaterials, ETechTypeCategory.Seeds }, 100, TechType.Knife, ExpectedResult = true)]
        [TestCase(new[]{ ETechTypeCategory.RawMaterials, ETechTypeCategory.Seeds }, -1, TechType.Knife, ExpectedResult = false)]
        public bool TestAddReachableWithPrereqs_Multiple(ETechTypeCategory[] categories, int maxDepth, TechType prereq)
        {
            return _mat.AddToLogic(categories, maxDepth, prereq);
        }
        
        [TestCase(new[]{ ETechTypeCategory.RawMaterials, ETechTypeCategory.Electronics }, 100, TechType.Knife, ExpectedResult = true)]
        [TestCase(new[]{ ETechTypeCategory.RawMaterials, ETechTypeCategory.Electronics }, -1, TechType.Knife, ExpectedResult = false)]
        [TestCase(new[]{ ETechTypeCategory.Equipment, ETechTypeCategory.Seeds }, 100, TechType.Knife, ExpectedResult = false)]
        [TestCase(new[]{ ETechTypeCategory.RawMaterials, ETechTypeCategory.Seeds }, -1, TechType.Knife, ExpectedResult = false)]
        public bool TestAddReachableWithPrereqs_Multiple_Invert(ETechTypeCategory[] categories, int maxDepth, TechType prereq)
        {
            return _mat.AddToLogic(categories, maxDepth, prereq, invert: true);
        }

        [Test]
        public void TestFind()
        {
            Assert.AreEqual(TechType.Titanium, _mat.GetEntity(TechType.Titanium)?.TechType);
        }

        [Test]
        public void TestFind_Null()
        {
            Assert.Null(_mat.GetEntity(TechType.DiveSuit));
        }

        [Test]
        public void TestGetAllCraftables()
        {
            List<LogicEntity> craftables = _mat.GetAllCraftables();
            Assert.NotNull(craftables);
            Assert.AreEqual(2, craftables.Count);
        }

        [Test]
        public void TestGetAllFragments()
        {
            List<LogicEntity> fragments = _mat.GetAllFragments();
            Assert.NotNull(fragments);
            Assert.AreEqual(1, fragments.Count);
        }

        [TestCase(0, ExpectedResult = 1)]
        [TestCase(100, ExpectedResult = 2)]
        [TestCase(-1, ExpectedResult = 0)]
        public int TestGetAllRawMaterials(int maxDepth)
        {
            List<LogicEntity> raws = _mat.GetAllRawMaterials(maxDepth);
            Assert.NotNull(raws);
            return raws.Count;
        }
    }
}