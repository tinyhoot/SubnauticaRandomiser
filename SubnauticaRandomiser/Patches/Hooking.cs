using System;
using HarmonyLib;

namespace SubnauticaRandomiser.Patches
{
    /// <summary>
    /// A class with patches for hooking into specific game events.
    /// </summary>
    [HarmonyPatch]
    internal static class Hooking
    {
        public static event Action OnQuitToMainMenu;
        
        /// <summary>
        /// Ensure we know when the user quits back to the main menu. This method runs after the confirmation step
        /// and after the game has saved (on hardcore). If the game were to quit to desktop instead it would do so
        /// instead of calling this method.
        ///
        /// The scene cleaner runs at the end of this method so we prefix it to ensure any loaded modules still exist.
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(IngameMenu), nameof(IngameMenu.QuitToMainMenuAsync))]
        private static void HookOnGameQuit()
        {
            OnQuitToMainMenu?.Invoke();
        }
    }
}