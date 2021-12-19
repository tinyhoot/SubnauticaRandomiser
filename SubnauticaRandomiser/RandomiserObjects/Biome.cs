using System;
namespace SubnauticaRandomiser.RandomiserObjects
{
    public class Biome
    {
        public readonly int AverageDepth;
        public readonly int CreatureSlots;
        public readonly int MediumSlots;
        public readonly int SmallSlots;
        public readonly string Name;
        public readonly EBiomeType BiomeType;

        public Biome(string name, EBiomeType biomeType, int creatureSlots, int mediumSlots, int smallSlots = -1)
        {
            Name = name;
            BiomeType = biomeType;

            AverageDepth = biomeType.GetAccessibleDepth();
            CreatureSlots = creatureSlots;
            MediumSlots = mediumSlots;
            SmallSlots = smallSlots >= 0 ? smallSlots : mediumSlots;
        }
    }
}
