using System.Collections;
using HarmonyLib;
using UnityEngine;

namespace SubnauticaRandomiser.Patches
{
    [HarmonyPatch]
    internal class VanillaBugfixes
    {
        /// <summary>
        /// Bugfix patch: Fish that hatch from eggs lack the Pickupable component when they are created from "nothing",
        /// such as when deconstructing base pieces. This causes the fish to either spawn at the player's feet or
        /// nothing to happen at all, which makes base pieces undeconstructable.
        /// Method uses a pass-through postfix.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(CraftData), nameof(CraftData.InstantiateFromPrefabAsync))]
        public static IEnumerator FixWaterParkCreature(IEnumerator values, TaskResult<GameObject> result)
        {
            // Pass through the first return value.
            yield return values;
            // Move to the next value, which exhausts the enumerator and populates 'result' with a GameObject.
            values.MoveNext();

            // Force all WaterParkCreatures to have the Pickupable component.
            GameObject gameObject = result.Get();
            if (gameObject != null && gameObject.GetComponent<WaterParkCreature>() != null)
                gameObject.EnsureComponent<Pickupable>();
        }
    }
}