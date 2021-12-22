using System;
namespace SubnauticaRandomiser.RandomiserObjects
{
    public class Biome
    {
        public readonly int CreatureSlots;
        public readonly int MediumSlots;
        public readonly int SmallSlots;
        public readonly float? FragmentRate;
        public readonly string Name;
        public readonly EBiomeType BiomeType;

        public int AverageDepth { get { return BiomeType.GetAccessibleDepth(); } }
        public int Used = 0;

        public Biome(string name, EBiomeType biomeType, int creatureSlots, int mediumSlots, int smallSlots = -1, float? fragmentRate = null)
        {
            Name = name;
            BiomeType = biomeType;

            CreatureSlots = creatureSlots;
            MediumSlots = mediumSlots;
            SmallSlots = smallSlots >= 0 ? smallSlots : mediumSlots;
            FragmentRate = fragmentRate;
        }
    }
}
