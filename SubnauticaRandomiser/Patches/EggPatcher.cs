using HarmonyLib;
using SubnauticaRandomiser.Logic;
using SubnauticaRandomiser.Objects.Enums;

namespace SubnauticaRandomiser.Patches
{
    [HarmonyPatch]
    internal class EggPatcher
    {
        /// <summary>
        /// Mark all eggs as known at the beginning of the game, skipping the need for identifying in ACU.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Player), nameof(Player.Start))]
        public static void UnlockEggs()
        {
            foreach (var egg in CoreLogic.Main.EntityHandler.GetByCategory(TechTypeCategory.Eggs))
            {
                KnownTech.Add(egg.TechType);
            }
        }
    }
}