using System.Collections.Generic;
using NUnit.Framework;
using SubnauticaRandomiser.Logic;
using SubnauticaRandomiser.Logic.Recipes;
using SubnauticaRandomiser.Objects;
using SubnauticaRandomiser.Objects.Enums;
using Tests.Mocks;

namespace Tests.UnitTests.Logic
{
    [TestFixture]
    public class ProgressionTreeTest
    {
        private ProgressionTree _tree;
        
        [SetUp]
        public void Init()
        {
            _tree = new ProgressionTree();
        }

        [Test]
        public void TestSetupVanillaTree()
        {
            _tree.SetupVanillaTree();
            Assert.Greater(_tree._depthDifficulties.Count, 0);
            Assert.Greater(_tree.DepthProgressionItems.Count, 0);
        }

        [Test]
        public void TestApplyUpgradeChainToPrerequisites()
        {
            List<LogicEntity> materials = new List<LogicEntity>
            {
                new LogicEntity(TechType.Titanium, ETechTypeCategory.RawMaterials),
                new LogicEntity(TechType.Gold, ETechTypeCategory.RawMaterials)
            };
            LogicEntity battery = new LogicEntity(TechType.Battery, ETechTypeCategory.Electronics);
            materials.Add(battery);
            _tree._upgradeChains.Add(TechType.Battery, TechType.Titanium);
            
            _tree.ApplyUpgradeChainToPrerequisites(materials);
            Assert.NotNull(battery.Prerequisites);
            Assert.AreEqual(TechType.Titanium, battery.Prerequisites[0]);
        }
        
        [Test]
        public void TestApplyUpgradeChainToPrerequisites_Empty()
        {
            List<LogicEntity> materials = new List<LogicEntity>
            {
                new LogicEntity(TechType.Titanium, ETechTypeCategory.RawMaterials),
                new LogicEntity(TechType.Gold, ETechTypeCategory.RawMaterials)
            };
            LogicEntity battery = new LogicEntity(TechType.Battery, ETechTypeCategory.Electronics);
            materials.Add(battery);

            _tree.ApplyUpgradeChainToPrerequisites(materials);
            Assert.Null(battery.Prerequisites);
            Assert.Zero(_tree._upgradeChains.Count);
        }

        [Test]
        public void TestGetProgressionPath()
        {
            ProgressionPath path = new ProgressionPath(EProgressionNode.Depth100m);
            _tree._depthDifficulties.Add(EProgressionNode.Depth100m, path);

            Assert.NotNull(_tree.GetProgressionPath(EProgressionNode.Depth100m));
        }

        [Test]
        public void TestGetProgressionPath_Null()
        {
            Assert.Null(_tree.GetProgressionPath(EProgressionNode.Depth100m));
        }

        [Test]
        public void TestSetProgressionPath()
        {
            ProgressionPath path1 = new ProgressionPath(EProgressionNode.Depth100m);
            _tree.SetProgressionPath(EProgressionNode.Depth100m, path1);
            Assert.NotNull(_tree._depthDifficulties[EProgressionNode.Depth100m]);
        }
        
        [Test]
        public void TestSetProgressionPath_Overwrite()
        {
            ProgressionPath path1 = new ProgressionPath(EProgressionNode.Depth100m);
            ProgressionPath path2 = new ProgressionPath(EProgressionNode.Depth100m);
            _tree.SetProgressionPath(EProgressionNode.Depth100m, path1);
            _tree.SetProgressionPath(EProgressionNode.Depth100m, path2);
            Assert.AreSame(path2, _tree._depthDifficulties[EProgressionNode.Depth100m]);
        }

        [Test]
        public void TestAddToProgressionPath()
        {
            TechType testType = TechType.Seamoth;
            _tree.AddToProgressionPath(EProgressionNode.Depth300m, testType);
            var target = _tree._depthDifficulties[EProgressionNode.Depth300m];
            Assert.NotNull(target);
            Assert.Contains(new[] { testType }, target.Pathways);
        }
        
        [Test]
        public void TestAddToProgressionPath_PreExisting()
        {
            TechType testType = TechType.Seamoth;
            _tree.AddToProgressionPath(EProgressionNode.Depth300m, TechType.Cyclops);
            _tree.AddToProgressionPath(EProgressionNode.Depth300m, testType);
            var target = _tree._depthDifficulties[EProgressionNode.Depth300m];
            Assert.NotNull(target);
            Assert.Greater(target.Pathways.Count, 1);
            Assert.Contains(new[] { testType }, target.Pathways);
        }
        
        [Test]
        public void TestAddEssentialItem()
        {
            TechType testType = TechType.Seamoth;
            _tree.AddEssentialItem(EProgressionNode.Depth300m, testType);
            var target = _tree._essentialItems[EProgressionNode.Depth300m];
            Assert.NotNull(target);
            Assert.Contains(testType, target);
        }
        
        [Test]
        public void TestAddEssentialItem_PreExisting()
        {
            TechType testType = TechType.Seamoth;
            _tree.AddEssentialItem(EProgressionNode.Depth300m, TechType.Beacon);
            _tree.AddEssentialItem(EProgressionNode.Depth300m, testType);
            var target = _tree._essentialItems[EProgressionNode.Depth300m];
            Assert.NotNull(target);
            Assert.Contains(testType, target);
        }
        
        [Test]
        public void TestAddElectiveItems()
        {
            var testTypes = new[]{TechType.Seamoth, TechType.Aerogel};
            _tree.AddElectiveItems(EProgressionNode.Depth300m, testTypes);
            var target = _tree._electiveItems[EProgressionNode.Depth300m];
            Assert.NotNull(target);
            Assert.Contains(testTypes, target);
        }
        
        [Test]
        public void TestAddElectiveItems_PreExisting()
        {
            var testTypes = new[]{TechType.Seamoth, TechType.Aerogel};
            _tree.AddElectiveItems(EProgressionNode.Depth300m, new[] { TechType.Battery });
            _tree.AddElectiveItems(EProgressionNode.Depth300m, testTypes);
            var target = _tree._electiveItems[EProgressionNode.Depth300m];
            Assert.NotNull(target);
            Assert.Contains(testTypes, target);
        }

        [Test]
        public void TestAddUpgradeChain()
        {
            Assert.True(_tree.AddUpgradeChain(TechType.Battery, TechType.Titanium));
        }
        
        [Test]
        public void TestAddUpgradeChain_Fail()
        {
            _tree.AddUpgradeChain(TechType.Battery, TechType.Titanium);
            Assert.True(_tree.AddUpgradeChain(TechType.Seamoth, TechType.Titanium));
            Assert.False(_tree.AddUpgradeChain(TechType.Battery, TechType.Peeper));
        }

        [Test]
        public void TestGetBaseOfUpgrade_Type()
        {
            _tree._upgradeChains.Add(TechType.Battery, TechType.Gold);
            Assert.AreEqual(TechType.Gold, _tree.GetBaseOfUpgrade(TechType.Battery));
        }
        
        [Test]
        public void TestGetBaseOfUpgrade_Type_None()
        {
            _tree._upgradeChains.Add(TechType.Battery, TechType.Gold);
            Assert.AreEqual(TechType.None, _tree.GetBaseOfUpgrade(TechType.Gold));
            Assert.AreEqual(TechType.None, _tree.GetBaseOfUpgrade(TechType.Seamoth));
        }

        [Test]
        public void TestGetBaseOfUpgrade_Entity()
        {
            List<LogicEntity> entities = new List<LogicEntity>
            {
                new LogicEntity(TechType.Titanium, ETechTypeCategory.RawMaterials),
                new LogicEntity(TechType.Gold, ETechTypeCategory.RawMaterials),
                new LogicEntity(TechType.Battery, ETechTypeCategory.Electronics)
            };
            Materials materials = new Materials(entities, new FakeLogger());
            _tree._upgradeChains.Add(TechType.Battery, TechType.Gold);
            Assert.AreEqual(TechType.Gold, _tree.GetBaseOfUpgrade(TechType.Battery, materials)?.TechType);
        }
        
        [Test]
        public void TestGetBaseOfUpgrade_Entity_Null()
        {
            List<LogicEntity> entities = new List<LogicEntity>
            {
                new LogicEntity(TechType.Titanium, ETechTypeCategory.RawMaterials),
                new LogicEntity(TechType.Gold, ETechTypeCategory.RawMaterials),
                new LogicEntity(TechType.Battery, ETechTypeCategory.Electronics)
            };
            Materials materials = new Materials(entities, new FakeLogger());
            _tree._upgradeChains.Add(TechType.Battery, TechType.Gold);
            Assert.Null(_tree.GetBaseOfUpgrade(TechType.Gold, materials));
            Assert.Null(_tree.GetBaseOfUpgrade(TechType.Seamoth, materials));
        }

        [TestCase(TechType.Seaglide, ExpectedResult = true)]
        [TestCase(TechType.Battery, ExpectedResult = false)]
        [TestCase(TechType.Seamoth, ExpectedResult = false)]
        public bool TestIsPriorityEntity(TechType techType)
        {
            _tree._essentialItems.Add(EProgressionNode.Depth100m, new List<TechType> { TechType.Seaglide });
            _tree._electiveItems.Add(EProgressionNode.Depth300m, new List<TechType[]>{ new [] { TechType.Battery }});

            LogicEntity entity = new LogicEntity(techType, ETechTypeCategory.None);
            return _tree.IsPriorityEntity(entity, 128);
        }

        [TestCase(TechType.Benzene, ExpectedResult = true)]
        [TestCase(TechType.Gold, ExpectedResult = false)]
        public bool TestIsProgressionItem(TechType techType)
        {
            LogicEntity entity = new LogicEntity(techType, ETechTypeCategory.None);
            _tree.DepthProgressionItems.Add(TechType.Benzene, true);
            return _tree.IsProgressionItem(entity);
        }
    }
}