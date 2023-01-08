using HarmonyLib;

namespace SubnauticaRandomiser.Patches
{
    [HarmonyPatch]
    internal class AuroraPatcher
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(KeypadDoorConsole), nameof(KeypadDoorConsole.Start))]
        public static void ChangeDoorCodes(ref KeypadDoorConsole __instance)
        {
            PrefabIdentifier id = __instance.gameObject.GetComponent<PrefabIdentifier>();
            // The Lab and Cargo room have multiple keypads organised under the same parent gameObject. Account for
            // that structure here.
            id ??= __instance.transform.parent.GetComponent<PrefabIdentifier>();
            Initialiser._Log.Debug($"Found door with code {__instance.accessCode} and identifier {id}");
            Initialiser._Log.Debug($"Code: {__instance.accessCode} key: {id.prefabKey}, id: {id.id}, classId: {id.classId}");
            if (!Initialiser._Serializer.DoorKeyCodes.ContainsKey(id.classId))
            {
                Initialiser._Log.Warn($"Found keypad for door which is not in logic: {id}");
                return;
            }

            __instance.accessCode = Initialiser._Serializer.DoorKeyCodes[id.classId];
        }
    }
}