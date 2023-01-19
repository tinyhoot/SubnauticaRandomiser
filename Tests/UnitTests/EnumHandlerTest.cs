using NUnit.Framework;
using SubnauticaRandomiser;
using SubnauticaRandomiser.Handlers;
using SubnauticaRandomiser.Objects.Enums;

namespace Tests.UnitTests
{
    [TestFixture]
    public class EnumHandlerTest
    {
        [TestCase("BloodKelp_Floor", ExpectedResult = BiomeType.BloodKelp_Floor)]
        [TestCase("SomethingNonexistant", ExpectedResult = BiomeType.Unassigned)]
        public BiomeType TestParse_BiomeType(string value)
        {
            return EnumHandler.Parse<BiomeType>(value);
        }
        
        [TestCase("RawMaterials", ExpectedResult = TechTypeCategory.RawMaterials)]
        [TestCase("SomethingNonexistant", ExpectedResult = TechTypeCategory.None)]
        public TechTypeCategory TestParse_ProgressionNode(string value)
        {
            return EnumHandler.Parse<TechTypeCategory>(value);
        }
    }
}