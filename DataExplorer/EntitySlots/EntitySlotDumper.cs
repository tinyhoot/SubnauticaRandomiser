using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace DataExplorer.EntitySlots
{
    internal class EntitySlotDumper
    {
        // The number of batches allocated by the worldstreamer in each direction.
        // Of these, the one the player is currently in +1 in each direction is always loaded.
        private Int3 _worldBatches = new Int3(26, 20, 26);
        // The number of cells contained in each batch loaded by the streamer.
        private Int3 _cellsPerBatch = new Int3(5, 5, 5);
        // The size of each cell in blocks. Blocks happen to be 1x1x1 in the unity coordinate system.
        private const int _cellSize = 32;

        public static EntitySlotDumper _main;
        private EntitySlotsDatabase _db;
        private Harmony _harmony;

        public EntitySlotDumper()
        {
            _main = this;
            _db = new EntitySlotsDatabase(Path.Combine(
                new FileInfo(Assembly.GetExecutingAssembly().Location).Directory?.FullName ?? string.Empty,
                "entity_slots.sqlite"));
            
            SetupHarmony();
        }

        public IEnumerator ScrapeSlots()
        {
            yield return DataDumper.ChainTeleport(GetTeleportLocations());
        }

        private void Insert(Int3 batch, Int3 cell, Vector3 worldPos, BiomeType biome, IEnumerable<EntitySlot.Type> slotTypes)
        {
            _db.Insert(batch.ToString(), cell.ToString(), worldPos.ToString(), biome.ToString(), 
                slotTypes.OrderBy(t => t).Join(t => t.ToString()), false);
        }

        private void Insert(Int3 batch, Int3 cell, Vector3 worldPos, BiomeType biome, EntitySlotData.EntitySlotType slotType)
        {
            _db.Insert(batch.ToString(), cell.ToString(), worldPos.ToString(), biome.ToString(), slotType.ToString(), true);
        }
        
        /// <summary>
        /// Get a list of teleport locations that, together, cause every place on the map to be loaded at least once.
        /// </summary>
        private List<Vector3> GetTeleportLocations()
        {
            List<Vector3> locations = new List<Vector3>();
            // 19 is just below sea level. 7 is around 1700m, it's pointless going any lower.
            for (int y = 19; y >= 7; y -= 2)
            {
                for (int z = 1; z <= _worldBatches.z; z += 2)
                {
                    for (int x = 1; x <= _worldBatches.x; x += 2)
                    {
                        locations.Add(LargeWorldStreamer.main.GetBatchCenter(new Int3(x, y, z)));
                    }
                }
            }

            return locations;
        }

        public void Teardown()
        {
            _main = null;
            _db.Teardown();
            _harmony.UnpatchSelf();
        }

        private void SetupHarmony()
        {
            _harmony = new Harmony(Initialiser.GUID + "_SlotDumper");
            _harmony.Patch(AccessTools.Method(typeof(EntitySlot), nameof(EntitySlot.Start)),
                prefix: new HarmonyMethod(typeof(EntitySlotDumper), nameof(RegisterEntitySlot)));
            _harmony.Patch(AccessTools.Method(typeof(EntitySlotsPlaceholder), nameof(EntitySlotsPlaceholder.Start)),
                prefix: new HarmonyMethod(typeof(EntitySlotDumper), nameof(RegisterEntitySlotPlaceholder)));
        }
        
        [HarmonyPrefix]
        [HarmonyPatch(typeof(EntitySlot), nameof(EntitySlot.Start))]
        private static void RegisterEntitySlot(EntitySlot __instance)
        {
            var streamer = LargeWorldStreamer.main;
            var pos = __instance.transform.position;
            _main.Insert(streamer.GetContainingBatch(pos), GetCellId(pos), pos, __instance.biomeType, __instance.allowedTypes);
        }
        
        [HarmonyPrefix]
        [HarmonyPatch(typeof(EntitySlotsPlaceholder), nameof(EntitySlotsPlaceholder.Start))]
        private static void RegisterEntitySlotPlaceholder(EntitySlotsPlaceholder __instance)
        {
            var streamer = LargeWorldStreamer.main;
            _main._db.StartTransaction();
            
            foreach (var slot in __instance.slotsData)
            {
                var pos = __instance.transform.position + slot.localPosition;
                _main.Insert(streamer.GetContainingBatch(pos), GetCellId(pos), pos, slot.biomeType, slot.allowedTypes);
            }
            _main._db.CommitTransaciton();
        }

        private static Int3 GetCellId(Vector3 pos)
        {
            var streamer = LargeWorldStreamer.main;
            Int3 block = streamer.GetBlock(pos);
            Int3 cellId = (block % streamer.blocksPerBatch) / _cellSize;
            return cellId;
        }
    }
}