using System;
using System.Collections.Generic;
using HarmonyLib;
using SMLHelper.V2.Assets;
using SMLHelper.V2.Crafting;
using SMLHelper.V2.Handlers;
using UnityEngine;
using UWE;
namespace SubnauticaRandomiser
{
    [HarmonyPatch(typeof(DataboxSpawner), nameof(DataboxSpawner.Start))]
    internal class DataboxPatcher
    {

        [HarmonyPrefix]
        internal static bool PatchDataboxOnSpawn(ref DataboxSpawner __instance)
        {
            // If databoxes were not randomised, do not mess with them.
            if (InitMod.s_masterDict == null || InitMod.s_masterDict.Databoxes == null || InitMod.s_masterDict.Databoxes.Count == 0)
            {
                LogHandler.Debug("[Databox] Databoxes not randomised.");
                return true;
            }

            Dictionary<RandomiserVector, TechType> boxDict = InitMod.s_masterDict.Databoxes;
            BlueprintHandTarget blueprint = __instance.databoxPrefab.GetComponent<BlueprintHandTarget>();

            Vector3 position = __instance.transform.position;

            LogHandler.Debug("[Databox] Found type " + blueprint.unlockTechType.AsString() + " at " + position.ToString());

            // Unfortunately it has to be done like this. Building an equal vector
            // from CSV has proven elusive, and they're not serialisable anyway.
            foreach (RandomiserVector vector in boxDict.Keys)
            {
                if (vector.EqualsUnityVector(position))
                {
                    LogHandler.Debug("[!] Replacing databox type with " + boxDict[vector].AsString());
                    blueprint.unlockTechType = boxDict[vector];
                }
            }

            return true;
        }
    }
}
