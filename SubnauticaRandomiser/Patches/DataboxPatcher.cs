using System.Linq;
using HarmonyLib;
using SubnauticaRandomiser.Handlers;
using SubnauticaRandomiser.Logic;
using SubnauticaRandomiser.Objects;
using SubnauticaRandomiser.Serialization.Modules;
using UnityEngine;
using ILogHandler = HootLib.Interfaces.ILogHandler;

namespace SubnauticaRandomiser.Patches
{
    [HarmonyPatch]
    internal class DataboxPatcher
    {
        private static ILogHandler _log => PrefixLogHandler.Get("[D]");
        // The maximum squared distance a databox's saved coordinates can be from its actual spawned coordinates
        // for it to be considered equal.
        private const float MaxSqrDistance = 3 * 3;
        
        [HarmonyPrefix]
        [HarmonyPatch(typeof(BlueprintHandTarget), nameof(BlueprintHandTarget.Start))]
        public static void PatchDatabox(ref BlueprintHandTarget __instance)
        {
            TechType replacement = GetTechTypeForPosition(__instance.transform.position);
            if (replacement != TechType.None)
                __instance.unlockTechType = replacement;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(DataboxSpawner), nameof(DataboxSpawner.Start))]
        public static void PatchDataboxSpawner(ref DataboxSpawner __instance)
        {
            TechType replacement = GetTechTypeForPosition(__instance.transform.position);
            if (replacement != TechType.None)
            {
                _log.Debug($"Replacing databox [{__instance.spawnTechType} "
                                       + $"{__instance.transform.position}] with {replacement}");
                __instance.spawnTechType = replacement;
            }
        }

        private static TechType GetTechTypeForPosition(Vector3 position)
        {
            if (!Bootstrap.SaveData.TryGetModuleData(out DataboxSaveData saveData))
                return TechType.None;
            // Take the square magnitude for distance to allow for some imperfection in the recorded databox data.
            Databox replacement = saveData.Databoxes.FirstOrDefault(box => (box.Coordinates - position).sqrMagnitude <= MaxSqrDistance);
            if (replacement != null)
                return replacement.TechType;

            _log.Warn($"Failed to find databox replacement for position {position}!");
            return TechType.None;
        }
    }
}
