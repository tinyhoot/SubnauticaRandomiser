using System;
using HarmonyLib;
using SMLHelper.V2.Assets;
using SMLHelper.V2.Crafting;
using SMLHelper.V2.Handlers;
using UnityEngine;
using UWE;
namespace SubnauticaRandomiser
{
    [HarmonyPatch(typeof(DataboxSpawner), nameof(DataboxSpawner.Start))]
    public class Test
    {

        [HarmonyPrefix]
        internal static bool test(ref DataboxSpawner __instance)
        {
            BlueprintHandTarget blueprint = __instance.databoxPrefab.GetComponent<BlueprintHandTarget>();

            TechType type = blueprint.unlockTechType;
            Vector3 position = __instance.transform.position;

            LogHandler.Debug("[Databox] Found type " + type.AsString() + " at " + position.ToString());

            //blueprint.unlockTechType = TechType.Cyclops;

            return true;
        }
    }
}
