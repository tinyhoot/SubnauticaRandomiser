using System;
using SubnauticaRandomiser.Objects.Enums;

namespace SubnauticaRandomiser.Objects
{
    /// <summary>
    /// A class representing a single Biome as the game handles it, along with detailed info on spawn slots.
    /// These individual biomes can get very detailed, such as BloodKelp_Floor, BloodKelp_CaveWall, etc.
    /// </summary>
    public class Biome
    {
        public readonly int CreatureSlots;
        public readonly int MediumSlots;
        public readonly int SmallSlots;
        public readonly float? FragmentRate;
        public readonly string Name;
        public readonly EBiomeType Region;
        public readonly BiomeType Variant;

        public int AverageDepth => Region.GetAccessibleDepth();
        public int Used = 0;

        public Biome(string name, EBiomeType biomeType, int creatureSlots, int mediumSlots, int smallSlots = -1, float? fragmentRate = null)
        {
            Name = name;
            Region = biomeType;
            Variant = ParseName(name);

            CreatureSlots = creatureSlots;
            MediumSlots = mediumSlots;
            SmallSlots = smallSlots >= 0 ? smallSlots : mediumSlots;
            FragmentRate = fragmentRate;
        }

        public static BiomeType ParseName(string name)
        {
            if (!Enum.TryParse(name, out BiomeType biomeType))
            {
                //LogHandler.Warn("! Failed to parse biome to enum: " + name);
                return BiomeType.Unassigned;
            }

            return biomeType;
        }
    }
}
