using System.Text.RegularExpressions;
using HarmonyLib;
using SubnauticaRandomiser.Logic;
using SubnauticaRandomiser.Logic.Modules;
using SubnauticaRandomiser.Serialization.Modules;

namespace SubnauticaRandomiser.Patches
{
    [HarmonyPatch]
    internal class LanguagePatcher
    {
        /// <summary>
        /// Edit the description of the PDA logs on door access codes to reflect the codes they were randomised to.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Language), nameof(Language.SetCurrentLanguage))]
        public static void PatchAccessCodeEntries()
        {
            foreach (var kv in Bootstrap.SaveData.GetModuleData<DoorSaveData>().DoorKeyCodes)
            {
                string descId = AuroraLogic.KeypadPrefabClassIds[kv.Key];
                string originalDesc = Language.main.Get(descId);
                string newDesc = Regex.Replace(originalDesc, "[0-9]{4}", kv.Value, RegexOptions.CultureInvariant);
                Language.main.strings[descId] = newDesc;
            }
        }
        
        /// <summary>
        /// For people with stupidly fast computers. STOP LOADING EVERYTHING SO FAST
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Player), nameof(Player.Start))]
        public static void PatchAccessCodeEntriesWithAHammer()
        {
            PatchAccessCodeEntries();
        }
    }
}