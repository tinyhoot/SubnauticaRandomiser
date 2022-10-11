using NUnit.Framework;
using SubnauticaRandomiser;
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
        
        [TestCase("RawMaterials", ExpectedResult = ETechTypeCategory.RawMaterials)]
        [TestCase("SomethingNonexistant", ExpectedResult = ETechTypeCategory.None)]
        public ETechTypeCategory TestParse_ProgressionNode(string value)
        {
            return EnumHandler.Parse<ETechTypeCategory>(value);
        }
    }
}