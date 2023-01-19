using System;
using System.Collections.Generic;
using NUnit.Framework;
using SubnauticaRandomiser;
using SubnauticaRandomiser.Logic;
using SubnauticaRandomiser.Logic.Recipes;
using SubnauticaRandomiser.Objects;
using SubnauticaRandomiser.Objects.Enums;
using Tests.Mocks;
using UnityEngine;

namespace Tests.UnitTests.Logic
{
    [TestFixture]
    public class DataboxLogicTests
    {
        private RandomiserConfig _config;
        private List<Databox> _databoxes;
        private DataboxLogic _databoxLogic;

        [SetUp]
        public void Init()
        {
            _databoxes = new List<Databox>
            {
                new Databox(TechType.SeamothElectricalDefense, new Vector3(0, 200, 0), 0, true, false),
                new Databox(TechType.SeamothElectricalDefense, new Vector3(0, 0, 0), 0, true, false),
                new Databox(TechType.SeamothElectricalDefense, new Vector3(0, 100, 0), 0, true, false),
                new Databox(TechType.CyclopsDecoyModule, new Vector3(0, 0, 0), 0, true, false),
                new Databox(TechType.CyclopsDecoyModule, new Vector3(0, 50, 0), 0, true, true),
                new Databox(TechType.CyclopsDecoyModule, new Vector3(0, 100, 0), 0, false, false),
                new Databox(TechType.CyclopsHullModule1, new Vector3(0, 500, 0), 0, false, true),
                new Databox(TechType.CyclopsHullModule1, new Vector3(0, 100, 0), 0, false, true),
                new Databox(TechType.CyclopsHullModule1, new Vector3(0, 0, 0), 0, true, false)
            };
            _config = new RandomiserConfig(new FakeLogger())
            {
                bRandomiseDataboxes = false,
                bRandomiseFragments = false,
                bRandomiseNumFragments = false,
                bRandomiseDuplicateScans = false,
                bRandomiseRecipes = false
            };
            CoreLogic core = new CoreLogic(new RandomHandler(), _config, new FakeLogger(), null, null);
            _databoxLogic = new DataboxLogic(core, _databoxes);
        }

        [Test]
        public void TestGetDepthsByType()
        {
            var result = _databoxLogic.GetDepthsByTechType(_databoxes);
            Assert.Contains(TechType.SeamothElectricalDefense, result.Keys);
            Assert.Contains(0, result[TechType.SeamothElectricalDefense]);
            Assert.Contains(100, result[TechType.SeamothElectricalDefense]);
            Assert.Contains(200, result[TechType.SeamothElectricalDefense]);
        }

        [Test]
        public void TestGetDepthsByType_Empty()
        {
            Assert.Throws<ArgumentException>(() => _databoxLogic.GetDepthsByTechType(new List<Databox>()));
        }

        [Test]
        public void TestGetDepthsByType_Null()
        {
            Assert.Throws<ArgumentException>(() => _databoxLogic.GetDepthsByTechType(null));
        }

        [TestCase(TechType.SeamothElectricalDefense)]
        [TestCase(TechType.CyclopsDecoyModule)]
        [TestCase(TechType.CyclopsHullModule1)]
        public void TestGetRequirementsByType(TechType techType)
        {
            var result = _databoxLogic.GetRequirementsByTechType(_databoxes);
            Assert.Contains(techType, result.Keys);
        }

        [TestCase(TechType.SeamothElectricalDefense, ExpectedResult = 3)]
        [TestCase(TechType.CyclopsDecoyModule, ExpectedResult = 2)]
        [TestCase(TechType.CyclopsHullModule1, ExpectedResult = 1)]
        public int TestGetRequirementsByType_LaserCutter(TechType techType)
        {
            var result = _databoxLogic.GetRequirementsByTechType(_databoxes);
            return result[techType][1];
        }

        [TestCase(TechType.SeamothElectricalDefense, ExpectedResult = 0)]
        [TestCase(TechType.CyclopsDecoyModule, ExpectedResult = 1)]
        [TestCase(TechType.CyclopsHullModule1, ExpectedResult = 2)]
        public int TestGetRequirementsByType_PropulsionCannon(TechType techType)
        {
            var result = _databoxLogic.GetRequirementsByTechType(_databoxes);
            return result[techType][2];
        }

        [Test]
        public void TestGetRequirementsByType_Empty()
        {
            Assert.Throws<ArgumentException>(() => _databoxLogic.GetRequirementsByTechType(new List<Databox>()));
        }

        [Test]
        public void TestGetRequirementsByType_Null()
        {
            Assert.Throws<ArgumentException>(() => _databoxLogic.GetRequirementsByTechType(null));
        }

        [Test]
        public void TestLinkCyclopsHullModules()
        {
            LogicEntity mod1 = new LogicEntity(TechType.CyclopsHullModule1, ETechTypeCategory.VehicleUpgrades,
                new Blueprint(TechType.CyclopsHullModule1, new List<TechType> { TechType.LaserCutter }, null, true,
                    248));
            LogicEntity mod2 = new LogicEntity(TechType.CyclopsHullModule2, ETechTypeCategory.VehicleUpgrades,
                new Blueprint(TechType.CyclopsHullModule2, null, null, true, 30));
            LogicEntity mod3 = new LogicEntity(TechType.CyclopsHullModule3, ETechTypeCategory.VehicleUpgrades,
                new Blueprint(TechType.CyclopsHullModule3, null, null, true, 20));
            List<LogicEntity> modules = new List<LogicEntity> { mod1, mod2, mod3 };
            EntityHandler entityHandler = new EntityHandler(modules, new FakeLogger());

            _databoxLogic.LinkCyclopsHullModules(entityHandler);
            Assert.AreEqual(mod1.Blueprint.UnlockDepth, mod2.Blueprint.UnlockDepth);
            Assert.AreEqual(mod1.Blueprint.UnlockConditions, mod2.Blueprint.UnlockConditions);
            Assert.AreEqual(mod1.Blueprint.UnlockDepth, mod3.Blueprint.UnlockDepth);
            Assert.AreEqual(mod1.Blueprint.UnlockConditions, mod3.Blueprint.UnlockConditions);
        }

        [Test]
        public void TestLinkCyclopsHullModules_Incomplete()
        {
            LogicEntity mod1 = new LogicEntity(TechType.CyclopsHullModule1, ETechTypeCategory.VehicleUpgrades,
                new Blueprint(TechType.CyclopsHullModule1, new List<TechType> { TechType.LaserCutter }, null, true,
                    248));
            LogicEntity mod2 = new LogicEntity(TechType.CyclopsHullModule2, ETechTypeCategory.VehicleUpgrades,
                new Blueprint(TechType.CyclopsHullModule2, null, null, true, 30));
            List<LogicEntity> modules = new List<LogicEntity> { mod1, mod2 };
            EntityHandler entityHandler = new EntityHandler(modules, new FakeLogger());

            Assert.Throws<ArgumentException>(() => _databoxLogic.LinkCyclopsHullModules(entityHandler));
        }

        [Test]
        public void TestRandomiseDataboxes()
        {
            var result = _databoxLogic.RandomiseDataboxes();
            Assert.AreNotEqual(_databoxes, result);
            Assert.AreEqual(_databoxes.Count, result.Count);
        }

        [Test]
        public void TestUpdateBlueprints()
        {
            LogicEntity e1 = new LogicEntity(TechType.SeamothElectricalDefense, ETechTypeCategory.VehicleUpgrades);
            LogicEntity e2 = new LogicEntity(TechType.CyclopsDecoyModule, ETechTypeCategory.VehicleUpgrades);
            LogicEntity e3 = new LogicEntity(TechType.CyclopsHullModule1, ETechTypeCategory.VehicleUpgrades,
                new Blueprint(TechType.CyclopsHullModule1));

            List<LogicEntity> entities = new List<LogicEntity>
            {
                e1,
                e2,
                e3
            };

            _databoxLogic.UpdateBlueprints(entities);
            Assert.NotNull(e1.Blueprint);
            Assert.AreEqual(100, e1.Blueprint.UnlockDepth);
            Assert.IsFalse(e3.Blueprint.UnlockConditions.Contains(TechType.LaserCutter));
            Assert.Contains(TechType.PropulsionCannon, e3.Blueprint.UnlockConditions);
        }

        [Test]
        public void TestUpdateBlueprints_BoxesEmpty()
        {
            var core = new CoreLogic(new RandomHandler(), _config, new FakeLogger(), null, null);
            var logic = new DataboxLogic(core, new List<Databox>());
            Assert.Throws<ArgumentException>(() => logic.UpdateBlueprints(null));
        }

        [Test]
        public void TestUpdateBlueprints_BoxesNull()
        {
            var core = new CoreLogic(new RandomHandler(), _config, new FakeLogger(), null, null);
            var logic = new DataboxLogic(core, null);
            Assert.Throws<ArgumentException>(() => logic.UpdateBlueprints(null));
        }

        [Test]
        public void TestUpdateBlueprints_EntityNull()
        {
            LogicEntity e1 = new LogicEntity(TechType.SeamothElectricalDefense, ETechTypeCategory.VehicleUpgrades);
            LogicEntity e2 = new LogicEntity(TechType.CyclopsDecoyModule, ETechTypeCategory.VehicleUpgrades);

            List<LogicEntity> entities = new List<LogicEntity>
            {
                e1,
                e2
            };

            Assert.Throws<ArgumentException>(() => _databoxLogic.UpdateBlueprints(entities));
        }
    }
}