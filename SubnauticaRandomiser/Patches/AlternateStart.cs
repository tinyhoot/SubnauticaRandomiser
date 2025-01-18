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
    internal static class AlternateStart
    {
        private static ILogHandler _log => PrefixLogHandler.Get("[AS]");
        
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
            if (!Bootstrap.SaveData.TryGetModuleData(out AlternateStartSaveData saveData) 
                || saveData.StartPoint == RandomiserVector.ZERO)
                // Has not been randomised, don't do anything.
                return;

            _log.Debug("Replacing lifepod spawnpoint with " + saveData.StartPoint);
            __result = saveData.StartPoint.ToUnityVector();
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
                
                _log.Debug($"{LeakingRadiation.main.kMaxRadius}");
                _log.InGameMessage("CAUTION: You are inside the Aurora's radiation radius.");
                _log.InGameMessage($"Radiation will reach the lifepod {days:F1} days after explosion.");
            }
        }
    }
}