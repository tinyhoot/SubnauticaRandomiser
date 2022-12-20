using NUnit.Framework;
using SubnauticaRandomiser;

namespace Tests.UnitTests
{
    [TestFixture]
    public class ConfigDefaultsTests
    {
        [Test]
        public void TestContains()
        {
            Assert.True(ConfigDefaults.Contains("bUseFish"));
        }

        [Test]
        public void TestContains_Invalid()
        {
            Assert.False(ConfigDefaults.Contains("someOptionThatDoesNotExist"));
        }

        [Test]
        public void TestGetDefault()
        {
            Assert.AreEqual(0, ConfigDefaults.GetDefault("iRandomiserMode"));
            Assert.AreEqual(true, ConfigDefaults.GetDefault("bUseFish"));
        }

        [Test]
        public void TestGetDefault_Invalid()
        {
            Assert.Null(ConfigDefaults.GetDefault("someOptionThatDoesNotExist"));
        }
        
        [Test]
        public void TestGetMax()
        {
            Assert.AreEqual(1, ConfigDefaults.GetMax("iRandomiserMode"));
            Assert.AreEqual(true, ConfigDefaults.GetMax("bUseFish"));
        }

        [Test]
        public void TestGetMax_Invalid()
        {
            Assert.Null(ConfigDefaults.GetMax("someOptionThatDoesNotExist"));
        }
        
        [Test]
        public void TestGetMin()
        {
            Assert.AreEqual(0, ConfigDefaults.GetMin("iRandomiserMode"));
            Assert.AreEqual(true, ConfigDefaults.GetMin("bUseFish"));
        }

        [Test]
        public void TestGetMin_Invalid()
        {
            Assert.Null(ConfigDefaults.GetMin("someOptionThatDoesNotExist"));
        }
    }
}