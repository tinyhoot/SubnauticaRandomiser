using System;
using System.Collections.Generic;
using System.Linq;
using HootLib;
using HootLib.Interfaces;
using MonoMod.Utils;
using SubnauticaRandomiser.Handlers;
using UWE;

namespace SubnauticaRandomiser.Serialization.Modules.EntitySlots
{
    internal class EntitySlotsTrackerSaveData : BaseModuleSaveData
    {
        public Dictionary<BiomeType, SlotCounts> SlotsData = new Dictionary<BiomeType, SlotCounts>();
        public Dictionary<BiomeType, EntityCounts> EntityData = new Dictionary<BiomeType, EntityCounts>();
        public Dictionary<TechType, Spawnable> Spawnables = new Dictionary<TechType, Spawnable>();
        public Dictionary<string, TechType> ClassIds = new Dictionary<string, TechType>();

        [NonSerialized]
        private ILogHandler _log = PrefixLogHandler.Get("[TrackerSaveData]");

        public void AddClassIds(Dictionary<string, TechType> classIds)
        {
            ClassIds.AddRange(classIds);
        }

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
                // Only look at those prefabs we actually want to modify.
                if (!ClassIds.TryGetValue(classId, out var techType))
                    continue;
                
                if (!Spawnables.TryGetValue(techType, out var spawnable))
                {
                    spawnable = new Spawnable();
                    spawnable.TechType = techType;
                    Spawnables.Add(techType, spawnable);
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

            // Only count classIds we were actually expecting. There will still be tons of unrelated stuff spawning.
            if (!ClassIds.TryGetValue(filler.classId, out var techType))
                return;
            
            counts.CountSpawn(techType, filler.count);
        }
    }
}