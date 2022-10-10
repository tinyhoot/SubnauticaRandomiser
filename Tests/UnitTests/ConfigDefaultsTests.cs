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
            Assert.False(ConfigDefaults.Contains("yeet"));
        }
    }
}