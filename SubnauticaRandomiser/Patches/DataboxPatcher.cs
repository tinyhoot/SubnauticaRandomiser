using System.Collections.Generic;
using HarmonyLib;
using SubnauticaRandomiser.RandomiserObjects;
using UnityEngine;

namespace SubnauticaRandomiser.Patches
{
    // [HarmonyPatch]
    internal class DataboxPatcher
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(DataboxSpawner), nameof(DataboxSpawner.Start))]
        internal static bool PatchDataboxOnSpawn(ref DataboxSpawner __instance)
        {
            Dictionary<RandomiserVector, TechType> boxDict = InitMod.s_masterDict.Databoxes;
            BlueprintHandTarget blueprint = __instance.databoxPrefab.GetComponent<BlueprintHandTarget>();
            Vector3 position = __instance.transform.position;

            FileLog.Log("[OnSpawn] Found blueprint " + blueprint.unlockTechType.AsString() + " at " 
                             + position.ToString());

            ReplaceDatabox(boxDict, position, blueprint);

            return true;
        }
        
        /// <summary>
        /// Intercept loading any GameObject from disk, and swap the blueprint if the GameObject happens to be a
        /// databox. Very much suboptimal since this function gets called every single time something spawns in game.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ProtobufSerializer), nameof(ProtobufSerializer.DeserializeIntoGameObject))]
        internal static void PatchDataboxOnLoad(ref ProtobufSerializer __instance, UniqueIdentifier uid)
        {
            BlueprintHandTarget blueprint = uid.gameObject.GetComponent<BlueprintHandTarget>();

            if (blueprint == null)
                return;

            FileLog.Log("[OnLoad] Found blueprint " + blueprint.unlockTechType.AsString());
            ReplaceDatabox(InitMod.s_masterDict.Databoxes, uid.transform.position, blueprint);
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
