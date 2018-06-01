using Harmony;

namespace SubnauticaRandomizer.HarmonyPatches
{
    [HarmonyPatch(typeof(PDAScanner))]
    [HarmonyPatch("GetEntryData")]
    public class PDAScanner_GetEntryData_Patch
    {
        public static void Postfix(ref PDAScanner.EntryData __result, TechType key)
        {
            if (__result != null && Settings.Instance.ScannerData.Required.ContainsKey(key))
            {
                __result.totalFragments = Settings.Instance.ScannerData.Required[key];
            }
        }
    }
}
