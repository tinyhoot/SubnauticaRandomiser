using System;
using HarmonyLib;
using SubnauticaRandomiser.Logic;
using SubnauticaRandomiser.Objects;
using UnityEngine;

namespace SubnauticaRandomiser.Patches
{
    [HarmonyPatch]
    internal static class AlternateStart
    {
        /// <summary>
        /// Override the spawn location of the lifepod at the start of the game.
        /// </summary>
        /// <param name="__result">The spawnpoint chosen by the game.</param>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(RandomStart), nameof(RandomStart.GetRandomStartPoint))]
        public static void OverrideStart(ref Vector3 __result)
        {
            if (__result.y > 50f)
                // User is likely using Lifepod Unleashed, skip randomising in that case.
                return;
            if (CoreLogic._Serializer?.StartPoint is null || CoreLogic._Serializer.StartPoint == RandomiserVector.ZERO)
                // Has not been randomised, don't do anything.
                return;

            Initialiser._Log.Debug("[AS] Replacing lifepod spawnpoint with " + CoreLogic._Serializer.StartPoint);
            __result = CoreLogic._Serializer.StartPoint.ToUnityVector();
        }

        /// <summary>
        /// Inform the player if their lifepod starts in a precarious, radiation-threatened position.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(EscapePod), nameof(EscapePod.StopIntroCinematic))]
        public static void CheckRadiationRadius()
        {
            // Get the distance tracker from the Aurora's Radiation gameObject.
            PlayerDistanceTracker tracker = LeakingRadiation.main.gameObject.GetComponent<PlayerDistanceTracker>();
            float maxRadius = LeakingRadiation.main.kMaxRadius;
            float curRadius = LeakingRadiation.main.currentRadius;

            if (tracker.distanceToPlayer <= maxRadius)
            {
                float time = (tracker.distanceToPlayer - curRadius) / LeakingRadiation.main.kGrowRate;
                float days = time / DayNightCycle.kDayLengthSeconds;
                
                Initialiser._Log.Debug($"{LeakingRadiation.main.kMaxRadius}");
                Initialiser._Log.InGameMessage("CAUTION: You are inside the Aurora's radiation radius.");
                Initialiser._Log.InGameMessage($"Radiation will reach the lifepod {days:F1} days after explosion.");
            }
        }
    }
}