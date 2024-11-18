using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        
        public IEnumerable<Task> LoadFiles()
        {
            return Enumerable.Empty<Task>();
        }

        public BaseModuleSaveData SetupSaveData()
        {
            return null;
        }

        public void ApplySerializedChanges(SaveData saveData) { }

        public void RandomiseOutOfLoop(IRandomHandler rng, SaveData saveData) { }

        public bool RandomiseEntity(IRandomHandler rng, ref LogicEntity entity)
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