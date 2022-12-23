using System.Collections.Generic;
using HarmonyLib;
using SubnauticaRandomiser.Objects;
using UnityEngine;

namespace SubnauticaRandomiser.Patches
{
    // [HarmonyPatch]
    internal class DataboxPatcher
    {
        // [HarmonyPrefix]
        // [HarmonyPatch(typeof(DataboxSpawner), nameof(DataboxSpawner.Start))]
        // internal static bool PatchDataboxOnSpawn(ref DataboxSpawner __instance)
        // {
        //     Dictionary<RandomiserVector, TechType> boxDict = InitMod.s_masterDict.Databoxes;
        //     BlueprintHandTarget blueprint = __instance.databoxPrefab.GetComponent<BlueprintHandTarget>();
        //     Vector3 position = __instance.transform.position;
        //
        //     FileLog.Log("[OnSpawn] Found blueprint " + blueprint.unlockTechType.AsString() + " at " 
        //                 + position.ToString());
        //
        //     ReplaceDatabox(boxDict, position, blueprint);
        //
        //     return true;
        // }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(BlueprintHandTarget), nameof(BlueprintHandTarget.Start))]
        internal static bool PatchDatabox(ref BlueprintHandTarget __instance)
        {
            FileLog.Log($"Found DB {__instance.unlockTechType}, {__instance.inspectObject.transform.position}");

            return true;
        }

        internal static void ReplaceDatabox(Dictionary<RandomiserVector, TechType> boxDict, Vector3 position, BlueprintHandTarget blueprint)
        {
            // Unfortunately it has to be done like this. Building an equal vector from CSV has proven elusive, and
            // they're not serialisable anyway.
            foreach (RandomiserVector vector in boxDict.Keys)
            {
                if (vector.EqualsUnityVector(position))
                {
                    FileLog.Log("[D] Replacing databox " + position.ToString() + " with "
                                     + boxDict[vector].AsString());
                    blueprint.unlockTechType = boxDict[vector];
                }
            }
        }
    }
}
