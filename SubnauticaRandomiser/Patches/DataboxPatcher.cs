using System.Collections.Generic;
using HarmonyLib;
using SubnauticaRandomiser.Logic;
using SubnauticaRandomiser.Objects;
using UnityEngine;

namespace SubnauticaRandomiser.Patches
{
    [HarmonyPatch]
    internal class DataboxPatcher
    {
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
                Initialiser._Log.Debug($"[D] Replacing databox [{__instance.spawnTechType} "
                                       + $"{__instance.transform.position}] with {replacement}");
                __instance.spawnTechType = replacement;
            }
        }

        private static TechType GetTechTypeForPosition(Vector3 position)
        {
            Dictionary<RandomiserVector, TechType> boxDict = CoreLogic._Serializer.Databoxes;

            foreach (RandomiserVector vector in boxDict.Keys)
            {
                if (vector.EqualsUnityVector(position))
                {
                    return boxDict[vector];
                }
            }

            Initialiser._Log.Warn($"[D] Failed to find databox replacement for position {position}!");
            return TechType.None;
        }
    }
}
