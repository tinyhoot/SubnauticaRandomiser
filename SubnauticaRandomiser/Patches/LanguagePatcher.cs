using System.Text.RegularExpressions;
using HarmonyLib;
using SubnauticaRandomiser.Logic;

namespace SubnauticaRandomiser.Patches
{
    [HarmonyPatch]
    internal class LanguagePatcher
    {
        /// <summary>
        /// Edit the description of the PDA logs on door access codes to reflect the codes they were randomised to.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Language), nameof(Language.Start))]
        public static void PatchAccessCodeEntries()
        {
            foreach (var kv in Initialiser._Serializer.DoorKeyCodes)
            {
                string descId = AuroraLogic.KeypadPrefabClassIds[kv.Key];
                string originalDesc = Language.main.Get(descId);
                string newDesc = Regex.Replace(originalDesc, "[0-9]{4}", kv.Value);
                Language.main.strings[descId] = newDesc;
            }
        }
    }
}