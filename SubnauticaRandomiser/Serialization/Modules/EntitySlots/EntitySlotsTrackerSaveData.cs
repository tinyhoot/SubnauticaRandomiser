using System;
using System.Collections.Generic;
using System.Linq;
using HootLib;
using HootLib.Interfaces;
using SubnauticaRandomiser.Handlers;
using UWE;

namespace SubnauticaRandomiser.Serialization.Modules.EntitySlots
{
    internal class EntitySlotsTrackerSaveData : BaseModuleSaveData
    {
        public Dictionary<BiomeType, SlotCounts> SlotsData = new Dictionary<BiomeType, SlotCounts>();
        public Dictionary<BiomeType, EntityCounts> EntityData = new Dictionary<BiomeType, EntityCounts>();
        public Dictionary<TechType, Spawnable> Spawnables = new Dictionary<TechType, Spawnable>();

        [NonSerialized]
        private ILogHandler _log = PrefixLogHandler.Get("[TrackerSaveData]");

        public void SetupSlots(List<SlotCounts> slotsData)
        {
            SlotsData = slotsData.ToDictionary(data => data.Biome, data => data);
        }

        public void SetupEntities(Dictionary<BiomeType, List<(TechType, int)>> minimumSpawns)
        {
            foreach (var (biome, data) in minimumSpawns)
            {
                if (!EntityData.TryGetValue(biome, out EntityCounts counts))
                {
                    counts = new EntityCounts();
                    counts.Biome = biome;
                    EntityData[biome] = counts;
                }

                foreach (var (techType, amt) in data)
                {
                    counts.AddEntity(techType, amt);
                }
            }
        }

        public void SetupSpawnables()
        {
            _log.Debug($"Found {WorldEntityDatabase.main.infos.Count} entries in WEDB");
            foreach (var (classId, info) in WorldEntityDatabase.main.infos)
            {
                // There's a ton of things that can spawn but don't have a TechType associated with them, like random
                // decoratives. Do not consider those.
                if (info.techType == TechType.None)
                    continue;
                
                if (!Spawnables.TryGetValue(info.techType, out var spawnable))
                {
                    spawnable = new Spawnable();
                    spawnable.TechType = info.techType;
                    Spawnables[info.techType] = spawnable;
                }
                spawnable.AddClassId(classId);
                spawnable.SlotType |= SlotCounts.ConvertToPlaceholderType(info.slotType);
            }
        }

        /// <summary>
        /// Count a slot as processed by the game's spawning systems.
        /// </summary>
        public void CountProcessedSlot(BiomeType biome, List<EntitySlot.Type> allowedTypes)
        {
            var type = SlotCounts.ConvertToPlaceholderType(allowedTypes);
            CountProcessedSlot(biome, type);
        }

        /// <inheritdoc cref="CountProcessedSlot(BiomeType,System.Collections.Generic.List{EntitySlot.Type})"/>
        public void CountProcessedSlot(BiomeType biome, EntitySlotData.EntitySlotType allowedTypes)
        {
            if (!SlotsData.TryGetValue(biome, out SlotCounts slotCounts))
            {
                _log.Warn($"Tried to count slot for unknown biome '{biome}'");
                return;
            }

            slotCounts.CountEntity(allowedTypes);
        }

        /// <summary>
        /// Count a desirable entity that was successfully spawned somewhere.
        /// </summary>
        public void CountSpawnedEntity(BiomeType biome, EntitySlot.Filler filler)
        {
            if (!EntityData.TryGetValue(biome, out EntityCounts counts))
            {
                // This causes pretty significant log spam if left on.
                // _log.Warn($"Tried to add spawned entity '{filler.classId}' for biome '{biome}' which has no entry.");
                return;
            }
            
            if (!WorldEntityDatabase.TryGetInfo(filler.classId, out var info))
            {
                throw new KeyNotFoundException($"Filler classId '{filler.classId}' is not in WorldEntityDB!");
            }
            
            counts.CountSpawn(info.techType, filler.count);
        }
    }
}