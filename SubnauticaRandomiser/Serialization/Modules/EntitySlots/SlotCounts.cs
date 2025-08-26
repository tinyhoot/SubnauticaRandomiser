using System;
using System.Collections.Generic;
using System.Linq;

namespace SubnauticaRandomiser.Serialization.Modules.EntitySlots
{
    /// <summary>
    /// Represents the number of <see cref="EntitySlot"/> and <see cref="EntitySlotsPlaceholder"/> that can spawn
    /// something in a specific biome. Distinguishes by <see cref="EntitySlotData.EntitySlotType"/>.
    /// </summary>
    [Serializable]
    internal class SlotCounts
    {
        public BiomeType Biome;
        public int SmallMax;
        public int SmallMediumMax;
        public int MediumMax;
        public int MediumLargeMax;
        public int CreatureMax;
        public double AvgDensity;
        public double TotalDensity;

        public int SmallSpawned;
        public int SmallMediumSpawned;
        public int MediumSpawned;
        public int MediumLargeSpawned;
        public int CreatureSpawned;

        public float SmallProgress;
        public float MediumProgress;
        public float CreatureProgress;
            
        public SlotCounts(BiomeType biome, int small, int smallMedium, int medium, int mediumLarge, int creature,
            double avgDensity, double totalDensity)
        {
            Biome = biome;
            SmallMax = small;
            SmallMediumMax = smallMedium;
            MediumMax = medium;
            MediumLargeMax = mediumLarge;
            CreatureMax = creature;
            AvgDensity = avgDensity;
            TotalDensity = totalDensity;
        }

        public void CountEntity(EntitySlotData.EntitySlotType type)
        {
            switch (type)
            {
                case EntitySlotData.EntitySlotType.Small | EntitySlotData.EntitySlotType.Medium:
                    SmallMediumSpawned += 1;
                    CalculateSmallProgress();
                    CalculateMediumProgress();
                    break;
                case EntitySlotData.EntitySlotType.Medium | EntitySlotData.EntitySlotType.Large:
                    MediumLargeSpawned += 1;
                    CalculateMediumProgress();
                    break;
                case EntitySlotData.EntitySlotType.Small:
                    SmallSpawned += 1;
                    CalculateSmallProgress();
                    break;
                case EntitySlotData.EntitySlotType.Medium:
                    MediumSpawned += 1;
                    CalculateMediumProgress();
                    break;
                case EntitySlotData.EntitySlotType.Creature:
                    CreatureSpawned += 1;
                    CreatureProgress = (float)CreatureSpawned / CreatureMax;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, "Unsupported EntitySlotType.");
            }
        }

        private void CalculateSmallProgress()
        {
            SmallProgress = (float)(SmallSpawned + SmallMediumSpawned) / (SmallMax + SmallMediumMax);
        }

        private void CalculateMediumProgress()
        {
            MediumProgress = (float)(MediumSpawned + SmallMediumSpawned + MediumLargeSpawned) /
                             (MediumMax + SmallMediumMax + MediumLargeMax);
        }

        /// <summary>
        /// Get the percentage of all slots of the given type in this biome that have already been processed.
        /// </summary>
        public float GetProgress(EntitySlotData.EntitySlotType slotType)
        {
            return slotType switch
            {
                EntitySlotData.EntitySlotType.Small | EntitySlotData.EntitySlotType.Medium => (SmallProgress + MediumProgress) / 2f,
                EntitySlotData.EntitySlotType.Medium | EntitySlotData.EntitySlotType.Large => MediumProgress,
                EntitySlotData.EntitySlotType.Small => SmallProgress,
                EntitySlotData.EntitySlotType.Medium => MediumProgress,
                EntitySlotData.EntitySlotType.Creature => CreatureProgress,
                _ => throw new ArgumentOutOfRangeException(nameof(slotType), slotType, "Unsupported EntitySlotType.")
            };
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
        
        /// <inheritdoc cref="ConvertToPlaceholderType(EntitySlot.Type)"/>
        public static EntitySlotData.EntitySlotType ConvertToPlaceholderType(List<EntitySlot.Type> allowedTypes)
        {
            // Bitwise OR of this flags enum is the same as just summing up all the underlying int values.
            return (EntitySlotData.EntitySlotType)allowedTypes
                .Select(ConvertToPlaceholderType)
                .Sum(t => (int)t);
        }
    }
}