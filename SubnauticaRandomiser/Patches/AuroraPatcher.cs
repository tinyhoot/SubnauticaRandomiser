using System;
using HarmonyLib;
using SubnauticaRandomiser.Handlers;
using SubnauticaRandomiser.Logic;

namespace SubnauticaRandomiser.Patches
{
    [HarmonyPatch]
    internal class AuroraPatcher_KeyCodes
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
            Initialiser._Log.Debug($"Code: {__instance.accessCode} key: {id.prefabKey}, id: {id.id}, "
                                   + $"classId: {id.classId}");
            if (!CoreLogic._Serializer.DoorKeyCodes.ContainsKey(id.classId))
            {
                Initialiser._Log.Warn($"Found keypad for door which is not in logic: {id}");
                return;
            }

            __instance.accessCode = CoreLogic._Serializer.DoorKeyCodes[id.classId];
        }
    }

    [HarmonyPatch]
    internal class AuroraPatcher_SupplyBoxes
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(HandTarget), nameof(HandTarget.Awake))]
        public static void ChangeSupplyBoxContents(ref HandTarget __instance)
        {
            if (__instance.GetType() != typeof(SupplyCrate))
                return;

            RandomHandler rand = new RandomHandler();
            TechType content = rand.Choice(CoreLogic._Serializer.SupplyBoxContents);
            // It is not enough to change a techtype, the box must load and spawn the correct prefab for its contents.
            PrefabPlaceholdersGroup group = __instance.gameObject.EnsureComponent<PrefabPlaceholdersGroup>();
            group.prefabPlaceholders[0].prefabClassId = CraftData.GetClassIdForTechType(content);
        }
    }
}