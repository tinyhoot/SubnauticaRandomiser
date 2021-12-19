using System;
using System.Collections.Generic;
using HarmonyLib;
using SubnauticaRandomiser.RandomiserObjects;
using UnityEngine;

namespace SubnauticaRandomiser
{
    [HarmonyPatch(typeof(DataboxSpawner), nameof(DataboxSpawner.Start))]
    internal class DataboxPatcher
    {

        [HarmonyPrefix]
        internal static bool PatchDataboxOnSpawn(ref DataboxSpawner __instance)
        {
            Dictionary<RandomiserVector, TechType> boxDict = InitMod.s_masterDict.Databoxes;
            BlueprintHandTarget blueprint = __instance.databoxPrefab.GetComponent<BlueprintHandTarget>();
            Vector3 position = __instance.transform.position;

            LogHandler.Debug("[OnSpawn] Found blueprint " + blueprint.unlockTechType.AsString() + " at " + position.ToString());

            ReplaceDatabox(boxDict, position, blueprint);

            return true;
        }

        internal static void ReplaceDatabox(Dictionary<RandomiserVector, TechType> boxDict, Vector3 position, BlueprintHandTarget blueprint)
        {
            // Unfortunately it has to be done like this. Building an equal vector
            // from CSV has proven elusive, and they're not serialisable anyway.
            foreach (RandomiserVector vector in boxDict.Keys)
            {
                if (vector.EqualsUnityVector(position))
                {
                    LogHandler.Debug("[!] Replacing databox " + position.ToString() + " with " + boxDict[vector].AsString());
                    blueprint.unlockTechType = boxDict[vector];
                }
            }
        }
    }


    [HarmonyPatch(typeof(ProtobufSerializer), nameof(ProtobufSerializer.DeserializeIntoGameObject))]
    internal class DataboxSavePatcher
    {
        // This intercepts loading any GameObject from disk, and swaps the blueprint
        // of any databoxes it finds. This *needs* to be a fast, lean method or
        // else load times and play quality will likely suffer.
        [HarmonyPostfix]
        internal static void PatchDataboxOnLoad(ref ProtobufSerializer __instance, UniqueIdentifier uid)
        {
            BlueprintHandTarget blueprint = uid.gameObject.GetComponent<BlueprintHandTarget>();

            if (blueprint == null)
            {
                return;
            }

            LogHandler.Debug("[OnLoad] Found blueprint " + blueprint.unlockTechType.AsString());
            DataboxPatcher.ReplaceDatabox(InitMod.s_masterDict.Databoxes, uid.transform.position, blueprint);

            return;
        }
    }
}
