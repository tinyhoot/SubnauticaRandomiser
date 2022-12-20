using System;
using NUnit.Framework;
using SubnauticaRandomiser;
using Tests.Mocks;

namespace Tests.UnitTests
{
    [TestFixture]
    public class RandomiserConfigTests
    {
        [TestCase(0, ExpectedResult = 0)]
        [TestCase(1, ExpectedResult = 1)]
        [TestCase(200, ExpectedResult = 0)]
        [TestCase(-1, ExpectedResult = 0)]
        public int TestSanitiseConfigValues(int value)
        {
            RandomiserConfig config = new RandomiserConfig(new FakeLogger());
            config.iRandomiserMode = value;
            config.SanitiseConfigValues();
            return config.iRandomiserMode;
        }

        [Test]
        public void TestIsButtonPressAllowed_Immediate()
        {
            RandomiserConfig config = new RandomiserConfig(new FakeLogger());
            config.IsButtonPressAllowed(DateTime.UtcNow);
            Assert.False(config.IsButtonPressAllowed(DateTime.UtcNow));
        }
        
        [Test]
        public void TestIsButtonPressAllowed_Later()
        {
            RandomiserConfig config = new RandomiserConfig(new FakeLogger());
            DateTime time = DateTime.UtcNow.AddSeconds(3);
            Assert.True(config.IsButtonPressAllowed(time));
        }
    }
}