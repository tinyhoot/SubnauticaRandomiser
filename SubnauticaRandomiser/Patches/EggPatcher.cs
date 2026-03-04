using System.Linq;
using HarmonyLib;
using SubnauticaRandomiser.Logic;
using SubnauticaRandomiser.Serialization.Modules;

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
            if (Bootstrap.SaveData.GetModuleData<RecipeSaveData>() is { } save)
            {
                foreach (var egg in save.EggsToAutoDiscover ?? Enumerable.Empty<TechType>())
                {
                    KnownTech.Add(egg);
                }
            }
        }
    }
}