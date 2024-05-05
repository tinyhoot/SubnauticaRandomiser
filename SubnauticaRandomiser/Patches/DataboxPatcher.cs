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
            if (saveData.Databoxes.TryGetValue(position.ToRandomiserVector(), out TechType replacement))
                return replacement;

            _log.Warn($"Failed to find databox replacement for position {position}!");
            return TechType.None;
        }
    }
}
