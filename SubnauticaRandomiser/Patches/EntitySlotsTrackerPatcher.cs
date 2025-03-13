using HarmonyLib;
using SubnauticaRandomiser.Serialization.Modules;

namespace SubnauticaRandomiser.Patches
{
    /// <summary>
    /// Responsible for hooking into the game methods needed for the slots tracker to do its work.
    /// </summary>
    [HarmonyPatch]
    internal static class EntitySlotsTrackerPatcher
    {
        private static EntitySlotsTrackerSaveData _saveData;
        
        public static void Setup(EntitySlotsTrackerSaveData saveData)
        {
            _saveData = saveData;
            Hooking.OnQuitToMainMenu += Teardown;
        }

        private static void Teardown()
        {
            _saveData = null;
            Hooking.OnQuitToMainMenu -= Teardown;
        }
        
        [HarmonyPrefix]
        [HarmonyPatch(typeof(EntitySlot), nameof(EntitySlot.Start))]
        private static void RegisterEntitySlot(EntitySlot __instance)
        {
            _saveData.AddProcessedSlot(__instance.biomeType, __instance.allowedTypes);
            
            // TODO: Check if spawned entity has minimum number defined in savedata, ensure good spawn curve if yes
            // Override even if successful spawn would occur, via postfix.
        }
        
        [HarmonyPrefix]
        [HarmonyPatch(typeof(EntitySlotsPlaceholder), nameof(EntitySlotsPlaceholder.Start))]
        private static void RegisterEntitySlotPlaceholder(EntitySlotsPlaceholder __instance)
        {
            // Placeholders hold all slots of an entire batch cell, often around 50.
            foreach (var slotsData in __instance.slotsData)
            {
                _saveData.AddProcessedSlot(slotsData.biomeType, slotsData.allowedTypes);
            }
        }
    }
}