using System;

namespace SubnauticaRandomiser.Serialization.Modules
{
    /// <summary>
    /// Represents the number of <see cref="EntitySlot"/> and <see cref="EntitySlotsPlaceholder"/> that can spawn
    /// something in a specific biome. Distinguishes by <see cref="EntitySlotData.EntitySlotType"/>.
    /// </summary>
    [Serializable]
    internal class EntitySlotCounts
    {
        public BiomeType Biome;
        public int SmallMax;
        public int SmallMediumMax;
        public int MediumMax;
        public int MediumLargeMax;
        public int CreatureMax;

        public int SmallSpawned;
        public int SmallMediumSpawned;
        public int MediumSpawned;
        public int MediumLargeSpawned;
        public int CreatureSpawned;
            
        public EntitySlotCounts(BiomeType biome, int small, int smallMedium, int medium, int mediumLarge, int creature)
        {
            Biome = biome;
            SmallMax = small;
            SmallMediumMax = smallMedium;
            MediumMax = medium;
            MediumLargeMax = mediumLarge;
            CreatureMax = creature;
        }

        public void CountEntity(EntitySlotData.EntitySlotType type)
        {
            switch (type)
            {
                case EntitySlotData.EntitySlotType.Small | EntitySlotData.EntitySlotType.Medium:
                    SmallMediumSpawned += 1;
                    break;
                case EntitySlotData.EntitySlotType.Medium | EntitySlotData.EntitySlotType.Large:
                    MediumLargeSpawned += 1;
                    break;
                case EntitySlotData.EntitySlotType.Small:
                    SmallSpawned += 1;
                    break;
                case EntitySlotData.EntitySlotType.Medium:
                    MediumSpawned += 1;
                    break;
                case EntitySlotData.EntitySlotType.Creature:
                    CreatureSpawned += 1;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, "Unsupported EntitySlotType.");
            }
        }
        
        /// <summary>
        /// The regular EntitySlot.Type is not a flags enum and not that common in the game. Convert it to the
        /// more standardised EntitySlotType used by <see cref="EntitySlotsPlaceholder"/>.
        /// </summary>
        public static EntitySlotData.EntitySlotType ConvertToPlaceholderType(EntitySlot.Type type)
        {
            return type switch
            {
                EntitySlot.Type.Small => EntitySlotData.EntitySlotType.Small,
                EntitySlot.Type.Medium => EntitySlotData.EntitySlotType.Medium,
                EntitySlot.Type.Large => EntitySlotData.EntitySlotType.Large,
                EntitySlot.Type.Tall => EntitySlotData.EntitySlotType.Tall,
                EntitySlot.Type.Creature => EntitySlotData.EntitySlotType.Creature,
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Invalid EntitySlotType.")
            };
        }
    }
}