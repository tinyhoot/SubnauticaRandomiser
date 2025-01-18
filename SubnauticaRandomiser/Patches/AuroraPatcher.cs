using HarmonyLib;
using HootLib.Interfaces;
using SubnauticaRandomiser.Handlers;
using SubnauticaRandomiser.Logic;
using SubnauticaRandomiser.Serialization.Modules;

namespace SubnauticaRandomiser.Patches
{
    [HarmonyPatch]
    internal class AuroraPatcher_KeyCodes
    {
        private static ILogHandler _log => PrefixLogHandler.Get("[Aurora]");
        
        [HarmonyPostfix]
        [HarmonyPatch(typeof(KeypadDoorConsole), nameof(KeypadDoorConsole.Start))]
        public static void ChangeDoorCodes(ref KeypadDoorConsole __instance)
        {
            PrefabIdentifier id = __instance.gameObject.GetComponent<PrefabIdentifier>();
            // The Lab and Cargo room have multiple keypads organised under the same parent gameObject. Account for
            // that structure here.
            id ??= __instance.transform.parent.GetComponent<PrefabIdentifier>();
            _log.Debug($"Found door with code {__instance.accessCode} and identifier {id}");
            // _log.Debug($"Code: {__instance.accessCode} key: {id.prefabKey}, id: {id.id}, "
            //                        + $"classId: {id.classId}");
            DoorSaveData saveData = Bootstrap.SaveData.GetModuleData<DoorSaveData>();
            if (!saveData.DoorKeyCodes.ContainsKey(id.classId))
            {
                _log.Warn($"Found keypad for door which is not in logic: {id}");
                return;
            }

            __instance.accessCode = saveData.DoorKeyCodes[id.classId];
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
            TechType content = Bootstrap.SaveData.GetModuleData<SupplyBoxSaveData>().LootTable.Drop(rand);
            // It is not enough to change a techtype, the box must load and spawn the correct prefab for its contents.
            PrefabPlaceholdersGroup group = __instance.gameObject.EnsureComponent<PrefabPlaceholdersGroup>();
            group.prefabPlaceholders[0].prefabClassId = CraftData.GetClassIdForTechType(content);
        }
    }
}