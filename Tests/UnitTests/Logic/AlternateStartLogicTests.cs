using System;
using System.Collections.Generic;
using NUnit.Framework;
using SubnauticaRandomiser.Handlers;
using SubnauticaRandomiser.Logic.Modules;
using SubnauticaRandomiser.Objects;
using Tests.Mocks;

namespace Tests.UnitTests.Logic
{
    [TestFixture]
    public class AlternateStartLogicTests
    {
        private RandomiserConfig _config;
        private FakeLogger _log;
        private AlternateStartLogic _logic;
        private Dictionary<SubnauticaRandomiser.Objects.Enums.BiomeRegion, List<float[]>> _starts;
        
        [SetUp]
        public void Init()
        {
            _starts = new Dictionary<SubnauticaRandomiser.Objects.Enums.BiomeRegion, List<float[]>>
            {
                { SubnauticaRandomiser.Objects.Enums.BiomeRegion.GrassyPlateaus, new List<float[]> { new float[] { 0, 200, 200, 0 } } },
                { SubnauticaRandomiser.Objects.Enums.BiomeRegion.Kelp, new List<float[]> { new float[] { -200, 0, 0, -200 } } },
                { SubnauticaRandomiser.Objects.Enums.BiomeRegion.SafeShallows, new List<float[]> { new float[] { -100, 100, 100, -100 } } }
            };
            _log = new FakeLogger();
            _config = new RandomiserConfig(_log);
            _logic = new AlternateStartLogic(_starts, _config, _log, new RandomHandler());
        }

        [Test]
        public void TestGetRandomStart()
        {
            RandomiserVector result = _logic.GetRandomStart("Kelp");
            Assert.GreaterOrEqual(result.x, _starts[SubnauticaRandomiser.Objects.Enums.BiomeRegion.Kelp][0][0]);
            Assert.GreaterOrEqual(result.z, _starts[SubnauticaRandomiser.Objects.Enums.BiomeRegion.Kelp][0][3]);
            Assert.LessOrEqual(result.x, _starts[SubnauticaRandomiser.Objects.Enums.BiomeRegion.Kelp][0][2]);
            Assert.LessOrEqual(result.z, _starts[SubnauticaRandomiser.Objects.Enums.BiomeRegion.Kelp][0][1]);
            Assert.Zero(result.y);
        }

        [Test]
        public void TestGetRandomStart_Vanilla()
        {
            Assert.Null(_logic.GetRandomStart("Vanilla"));
        }

        [Test]
        public void TestGetRandomStart_Invalid()
        {
            Assert.Throws<ArgumentException>(() => _logic.GetRandomStart(""));
            Assert.Throws<ArgumentException>(() => _logic.GetRandomStart("BulbZone"));
        }
    }
}