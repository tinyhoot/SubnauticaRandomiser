using System.Collections.Generic;
using NUnit.Framework;
using SubnauticaRandomiser.Handlers;
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
                new LogicEntity(EntityType.Craftable, TechType.Titanium, TechTypeCategory.RawMaterials) { AccessibleDepth = 0 },
                new LogicEntity(EntityType.Craftable, TechType.Gold, TechTypeCategory.RawMaterials) { AccessibleDepth = 70 },
                new LogicEntity(EntityType.Craftable, TechType.Peeper, TechTypeCategory.Fish){ AccessibleDepth = 0},
                new LogicEntity(EntityType.Craftable, TechType.Battery, TechTypeCategory.Electronics){ AccessibleDepth = 100 },
                new LogicEntity(EntityType.Craftable, TechType.Aquarium, TechTypeCategory.BaseInternalPieces){ AccessibleDepth = 100 },
                new LogicEntity(EntityType.Craftable, TechType.BluePalmSeed, TechTypeCategory.Seeds, prerequisites: new List<TechType>(){TechType.Knife}),
                new LogicEntity(EntityType.Craftable, TechType.SeaCrownSeed, TechTypeCategory.Seeds, prerequisites: new List<TechType>(){TechType.Knife}){ AccessibleDepth = 100},
                new LogicEntity(EntityType.Craftable, TechType.BeaconFragment, TechTypeCategory.Fragments)
            };
            _mat = new EntityHandler(new FakeLogger());
        }

        [TestCase(TechTypeCategory.RawMaterials, 0, ExpectedResult = true)]
        [TestCase(TechTypeCategory.RawMaterials, 50, ExpectedResult = true)]
        [TestCase(TechTypeCategory.RawMaterials, 70, ExpectedResult = true)]
        [TestCase(TechTypeCategory.RawMaterials, -1, ExpectedResult = false)]
        [TestCase(TechTypeCategory.Fish, 0, ExpectedResult = true)]
        [TestCase(TechTypeCategory.Eggs, 0, ExpectedResult = false)]
        public bool TestAddReachable_Category(TechTypeCategory category, int maxDepth)
        {
            return _mat.AddToLogic(category, maxDepth);
        }

        [TestCase(new[]{ TechTypeCategory.RawMaterials }, 0)]
        [TestCase(new[]{ TechTypeCategory.RawMaterials }, 100)]
        [TestCase(new[]{ TechTypeCategory.RawMaterials, TechTypeCategory.Seeds }, 0)]
        [TestCase(new[]{ TechTypeCategory.RawMaterials, TechTypeCategory.Seeds }, 100)]
        [TestCase(new[]{ TechTypeCategory.Fish, TechTypeCategory.Equipment }, 100)]
        public void TestAddReachable_Categories(TechTypeCategory[] categories, int maxDepth)
        {
            Assert.True(_mat.AddToLogic(categories, maxDepth));
        }
        
        [TestCase(new[]{ TechTypeCategory.RawMaterials }, -1)]
        [TestCase(new[]{ TechTypeCategory.Electronics }, 0)]
        [TestCase(new[]{ TechTypeCategory.Electronics }, 0)]
        [TestCase(new[]{ TechTypeCategory.AdvancedMaterials}, 100)]
        public void TestAddReachable_Categories_Fail(TechTypeCategory[] categories, int maxDepth)
        {
            Assert.False(_mat.AddToLogic(categories, maxDepth));
        }

        [TestCase(TechType.Titanium, TechTypeCategory.RawMaterials, ExpectedResult = true)]
        [TestCase(TechType.Titanium, TechTypeCategory.BaseGenerators, ExpectedResult = true)]
        [TestCase(TechType.JeweledDiskPiece, TechTypeCategory.RawMaterials, ExpectedResult = true)]
        [TestCase(TechType.JeweledDiskPiece, TechTypeCategory.Eggs, ExpectedResult = true)]
        public bool TestAddReachable(TechType techType, TechTypeCategory category)
        {
            LogicEntity entity = new LogicEntity(EntityType.Craftable, techType, category);
            return _mat.AddToLogic(entity);
        }

        [Test]
        public void TestAddReachable_Twice()
        {
            LogicEntity entity = new LogicEntity(EntityType.Craftable, TechType.Titanium, TechTypeCategory.RawMaterials);
            Assert.True(_mat.AddToLogic(entity));
            Assert.False(_mat.AddToLogic(entity));
        }
        
        [TestCase(TechTypeCategory.RawMaterials, 100, TechType.Knife, ExpectedResult = false)]
        [TestCase(TechTypeCategory.Seeds, 100, TechType.Knife, ExpectedResult = true)]
        [TestCase(TechTypeCategory.Seeds, -1, TechType.Knife, ExpectedResult = false)]
        public bool TestAddReachableWithPrereqs(TechTypeCategory category, int maxDepth, TechType prereq)
        {
            return _mat.AddToLogic(category, maxDepth, prereq);
        }

        [TestCase(TechTypeCategory.RawMaterials, 100, TechType.Knife, ExpectedResult = true)]
        [TestCase(TechTypeCategory.RawMaterials, -1, TechType.Knife, ExpectedResult = false)]
        [TestCase(TechTypeCategory.Seeds, 100, TechType.Knife, ExpectedResult = false)]
        [TestCase(TechTypeCategory.Seeds, -1, TechType.Knife, ExpectedResult = false)]
        public bool TestAddReachableWithPrereqs_Invert(TechTypeCategory category, int maxDepth, TechType prereq)
        {
            return _mat.AddToLogic(category, maxDepth, prereq, invert: true);
        }
        
        [TestCase(new[]{ TechTypeCategory.RawMaterials, TechTypeCategory.Electronics }, 100, TechType.Knife, ExpectedResult = false)]
        [TestCase(new[]{ TechTypeCategory.RawMaterials, TechTypeCategory.Seeds }, 100, TechType.Knife, ExpectedResult = true)]
        [TestCase(new[]{ TechTypeCategory.RawMaterials, TechTypeCategory.Seeds }, -1, TechType.Knife, ExpectedResult = false)]
        public bool TestAddReachableWithPrereqs_Multiple(TechTypeCategory[] categories, int maxDepth, TechType prereq)
        {
            return _mat.AddToLogic(categories, maxDepth, prereq);
        }
        
        [TestCase(new[]{ TechTypeCategory.RawMaterials, TechTypeCategory.Electronics }, 100, TechType.Knife, ExpectedResult = true)]
        [TestCase(new[]{ TechTypeCategory.RawMaterials, TechTypeCategory.Electronics }, -1, TechType.Knife, ExpectedResult = false)]
        [TestCase(new[]{ TechTypeCategory.Equipment, TechTypeCategory.Seeds }, 100, TechType.Knife, ExpectedResult = false)]
        [TestCase(new[]{ TechTypeCategory.RawMaterials, TechTypeCategory.Seeds }, -1, TechType.Knife, ExpectedResult = false)]
        public bool TestAddReachableWithPrereqs_Multiple_Invert(TechTypeCategory[] categories, int maxDepth, TechType prereq)
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