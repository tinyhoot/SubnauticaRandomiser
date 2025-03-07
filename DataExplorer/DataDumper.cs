using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using UWE;

namespace DataExplorer
{
    /// <summary>
    /// Capable of dumping internal data to log files for an easy overview of what's happening.
    /// </summary>
    internal static class DataDumper
    {
        public static void LogBiomes()
        {
            // Grab a copy of all vanilla BiomeData. This loads it fresh from disk
            // and will thus be unaffected by any existing randomisation.
            LootDistributionData loot = LootDistributionData.Load(LootDistributionData.dataPath);

            Initialiser._Log.LogDebug("---Dumping Biomes");
            BiomeType[] biomes = (BiomeType[])Enum.GetValues(typeof(BiomeType));
            foreach (BiomeType biome in biomes)
            {
                if (loot.GetBiomeLoot(biome, out LootDistributionData.DstData distributionData))
                {
                    int valid = 0;
                    int validFragments = 0;
                    float sum = 0f;
                    float sumFragments = 0f;
                    foreach (var prefab in distributionData.prefabs)
                    {
                        if (string.IsNullOrEmpty(prefab.classId) || prefab.classId.Equals("None"))
                            continue;

                        valid++;
                        sum += prefab.probability;

                        if (WorldEntityDatabase.TryGetInfo(prefab.classId, out WorldEntityInfo info)){
                            if (info != null && !info.techType.Equals(TechType.None) && info.techType.AsString().ToLower().Contains("fragment"))
                            {
                                validFragments++;
                                sumFragments += prefab.probability;
                            }
                        }
                    }
                    Initialiser._Log.LogDebug(
                        $"{biome.AsString()}\t{valid} entries\t{validFragments} fragments\t{sum} totalspawnrate\t{sumFragments} totalfragmentrate");
                }
                else
                {
                    Initialiser._Log.LogDebug($"{biome.AsString()}\tNONE\t\t");
                }
            }
        }
        
        public static void LogKnownTech()
        {
            foreach (var tech in KnownTech.compoundTech)
            {
                Initialiser._Log.LogDebug($"Compound: {tech.techType}, {tech.dependencies}");
            }

            foreach (var tech in KnownTech.analysisTech)
            {
                Initialiser._Log.LogDebug($"Scanning {tech.techType} unlocks:");
                foreach (var unlock in tech.unlockTechTypes)
                {
                    Initialiser._Log.LogDebug($"-- {unlock}");
                }
            }
        }

        public static void LogPDAEncyclopedia()
        {
            foreach (var kvpair in PDAEncyclopedia.mapping)
            {
                Initialiser._Log.LogDebug($"Key: {kvpair.Key}, Path: {kvpair.Value.path}");
                Initialiser._Log.LogDebug(Language.main.Get("EncyDesc_" + kvpair.Key));
            }
        }

        public static void LogPrefabs()
        {
            // Cache the ids, otherwise this logs nothing.
            _ = CraftData.GetClassIdForTechType(TechType.Titanium);
            var keys = PrefabDatabase.prefabFiles.Keys;
            foreach (string classId in keys)
            {
                Initialiser._Log.LogDebug($"classId: {classId}, prefab: {PrefabDatabase.prefabFiles[classId]}");
            }
        }

        public static void LogAssets()
        {
            foreach (var bundle in AssetBundle.GetAllLoadedAssetBundles())
            {
                Initialiser._Log.LogDebug($"Bundle: {bundle.name}");
                
                foreach (var path in bundle.GetAllAssetNames())
                {
                    Initialiser._Log.LogDebug($"> {path}");
                }
            }
        }

        public static IEnumerator LogDataboxes()
        {
            ErrorMessage.AddMessage("Logging databoxes is defunct without access to csv data.");
            yield break;
            // var task = CSVReader.ParseDataFileAsync(Initialiser._WreckageFile, CSVReader.ParseWreckageLine);
            // yield return new WaitUntil(() => task.IsCompleted);

            // Temporarily patch into the databox spawner while the chain teleport explores all relevant locations.
            // Harmony harmony = new Harmony(Initialiser.GUID);
            // harmony.Patch(AccessTools.Method(typeof(DataboxSpawner), nameof(DataboxSpawner.Start)),
            //     postfix: new HarmonyMethod(AccessTools.Method(typeof(DataDumper), nameof(DataDumper.PatchLogDatabox))));
            //
            // yield return ChainTeleport(Enumerable.ToList<Vector3>(task.Result.Select(box => box.Coordinates)));
            //
            // harmony.Unpatch(AccessTools.Method(typeof(DataboxSpawner), nameof(DataboxSpawner.Start)),
            //     AccessTools.Method(typeof(DataDumper), nameof(DataDumper.PatchLogDatabox)));
        }

        private static IEnumerator PatchLogDatabox(IEnumerator passthrough, DataboxSpawner __instance)
        {
            Initialiser._Log.LogDebug($"Other components: {__instance.GetComponents(typeof(Component)).Select(x => x.GetType()).Join()}");
            // Let the spawner set up the spawning process for the actual databox.
            passthrough.MoveNext();
            if (passthrough.Current is CoroutineTask<GameObject> task)
            {
                // Let the actual databox load in.
                yield return task;
                // This extra delay seems necessary to guarantee the PrefabIdentifier on the freshly spawned box exists.
                yield return null;
                var spawnerpid = __instance.GetComponent<PrefabIdentifier>();
                var spawnerId = spawnerpid == null ? "NULL" : spawnerpid.id;
                var boxId = task.GetResult().GetComponent<PrefabIdentifier>();
                // Spawners often seem to have no individual id, making identification by id impossible.
                // The boxes do have their own ids, but these are not consistent from save to save.
                Initialiser._Log.LogDebug($"Databox: {__instance.spawnTechType} at {__instance.transform.position}, spawner Id: {spawnerId}, box Id: {boxId?.id}");
            }
            else
            {
                Initialiser._Log.LogDebug($"Spawning passthrough came up null for {__instance.spawnTechType} at {__instance.transform.position}");
            }

            // Let the rest of the vanilla method complete as intended.
            yield return passthrough;
        }

        #region Utility

        /// <summary>
        /// Teleport to multiple locations in a row with a small delay in between.
        /// </summary>
        public static IEnumerator ChainTeleport(List<Vector3> positions)
        {
            Initialiser._Log.LogDebug("Starting chain teleport exploration.");
            int count = 0;
            foreach (Vector3 position in positions)
            {
                yield return TeleportExplore(position);
                // Wait a few extra frames at the fully loaded location.
                yield return null;
                yield return null;
                yield return null;
                
                count++;
                if (count % 5 == 0)
                    ErrorMessage.AddMessage($"Explored {count}/{positions.Count} locations.");
            }
            ErrorMessage.AddMessage("Chain exploration done!");
        }

        /// <summary>
        /// Teleport to a new area and wait until the area has fully loaded in.
        /// </summary>
        private static IEnumerator TeleportExplore(Vector3 position)
        {
            // Instantly go to the desired position.
            yield return GotoConsoleCommand.main.GotoLocation(position, true);
            // Wait until the teleport is done and the game is no longer loading in terrain at the new location.
            yield return new WaitUntil(() => !GotoConsoleCommand.main.movingPlayer && LargeWorldStreamer.main.IsWorldSettled());
        }
        
        #endregion Utility
    }
}