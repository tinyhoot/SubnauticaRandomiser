using System;
using System.Collections.Generic;
using System.Linq;

namespace SubnauticaRandomiser.Serialization.Modules
{
    internal class EntitySlotsTrackerSaveData : BaseModuleSaveData
    {
        public Dictionary<BiomeType, EntitySlotCounts> SlotsData = new Dictionary<BiomeType, EntitySlotCounts>();

        public void Setup(List<EntitySlotCounts> slotsData)
        {
            SlotsData = slotsData.ToDictionary(data => data.Biome, data => data);
        }

        /// <summary>
        /// Count a slot as processed by the game's spawning systems.
        /// </summary>
        public void AddProcessedSlot(BiomeType biome, List<EntitySlot.Type> allowedTypes)
        {
            // Bitwise OR of this flags enum is the same as just summing up all the underlying int values.
            var type = (EntitySlotData.EntitySlotType)allowedTypes
                .Select(EntitySlotCounts.ConvertToPlaceholderType)
                .Sum(t => (int)t);
            AddProcessedSlot(biome, type);
        }

        /// <inheritdoc cref="AddProcessedSlot(BiomeType,System.Collections.Generic.List{EntitySlot.Type})"/>
        public void AddProcessedSlot(BiomeType biome, EntitySlotData.EntitySlotType allowedTypes)
        {
            if (!SlotsData.TryGetValue(biome, out EntitySlotCounts slotCounts))
                throw new ArgumentOutOfRangeException(nameof(biome), biome, "Invalid biome!");
            slotCounts.CountEntity(allowedTypes);
        }
    }
}