using HarmonyLib;
using SubnauticaRandomiser.Interfaces;
using SubnauticaRandomiser.Objects;
using SubnauticaRandomiser.Objects.Enums;
using SubnauticaRandomiser.Patches;
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

            Bootstrap.Main.RegisterEntityHandler(EntityType.RawMaterial, this);
        }

        public void ApplySerializedChanges(EntitySerializer serializer) { }

        public void RandomiseOutOfLoop(EntitySerializer serializer) { }

        public bool RandomiseEntity(ref LogicEntity entity)
        {
            // Simply add this into the logic if prerequisites and depth check out.
            return entity.CheckReady(_coreLogic, _manager.ReachableDepth)
                   && entity.AccessibleDepth <= _manager.ReachableDepth;
        }

        public void SetupHarmonyPatches(Harmony harmony)
        {
            // This one is an exception and *can* rely on config values because the patch only has to be applied once
            // on game start, and the saved data will carry over from then on.
            if (CoreLogic._Serializer.DiscoverEggs)
                harmony.PatchAll(typeof(EggPatcher));
        }
    }
}