using HarmonyLib;
using SubnauticaRandomiser.Interfaces;
using SubnauticaRandomiser.Objects;
using SubnauticaRandomiser.Objects.Enums;
using SubnauticaRandomiser.Patches;
using SubnauticaRandomiser.Serialization;
using SubnauticaRandomiser.Serialization.Modules;
using UnityEngine;

namespace SubnauticaRandomiser.Logic.Modules
{
    /// <summary>
    /// Rudimentary module to handle raw materials like eggs in the logic.
    /// </summary>
    [RequireComponent(typeof(CoreLogic))]
    internal class RawMaterialLogic : MonoBehaviour, ILogicModule
    {
        private CoreLogic _coreLogic;
        private ProgressionManager _manager;

        private void Awake()
        {
            _coreLogic = GetComponent<CoreLogic>();
            _manager = GetComponent<ProgressionManager>();

            _coreLogic.RegisterEntityHandler(EntityType.RawMaterial, this);
        }

        public BaseModuleSaveData SetupSaveData()
        {
            return null;
        }

        public void ApplySerializedChanges(SaveData saveData) { }

        public void RandomiseOutOfLoop(SaveData saveData) { }

        public bool RandomiseEntity(ref LogicEntity entity)
        {
            // Simply add this into the logic if prerequisites and depth check out.
            return entity.CheckReady(_coreLogic, _manager.ReachableDepth)
                   && entity.AccessibleDepth <= _manager.ReachableDepth;
        }

        public void SetupHarmonyPatches(Harmony harmony, SaveData saveData)
        {
            if (saveData.GetModuleData<RecipeSaveData>().DiscoverEggs)
                harmony.PatchAll(typeof(EggPatcher));
        }
    }
}