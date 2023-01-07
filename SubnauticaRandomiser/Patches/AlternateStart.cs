using System.Collections.Generic;
using HarmonyLib;
using SubnauticaRandomiser.Objects;
using UnityEngine;

namespace SubnauticaRandomiser.Patches
{
    // [HarmonyPatch]
    internal static class AlternateStart
    {
        /// <summary>
        /// Override the spawn location of the lifepod at the start of the game.
        /// </summary>
        /// <param name="__result">The spawnpoint chosen by the game.</param>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(RandomStart), nameof(RandomStart.GetRandomStartPoint))]
        internal static void OverrideStart(ref Vector3 __result)
        {
            if (__result.y > 50f)
                // User is likely using Lifepod Unleashed, skip randomising in that case.
                return;
            if (InitMod._Serializer?.StartPoint is null)
                // Has not been randomised, don't do anything.
                return;

            InitMod._Log.Debug("[AS] Replacing lifepod spawnpoint with " + InitMod._Serializer.StartPoint);
            __result = InitMod._Serializer.StartPoint.ToUnityVector();
        }
    }
}