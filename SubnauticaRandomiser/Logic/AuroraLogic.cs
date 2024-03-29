using System;
using System.Collections.Generic;
using HarmonyLib;
using SubnauticaRandomiser.Configuration;
using SubnauticaRandomiser.Interfaces;
using SubnauticaRandomiser.Objects;
using SubnauticaRandomiser.Patches;
using UnityEngine;
using ILogHandler = SubnauticaRandomiser.Interfaces.ILogHandler;

namespace SubnauticaRandomiser.Logic
{
    /// <summary>
    /// Handles randomising everything in and around the Aurora.
    /// </summary>
    [RequireComponent(typeof(CoreLogic))]
    internal class AuroraLogic : MonoBehaviour, ILogicModule
    {
        private CoreLogic _coreLogic;

        private Config _config => _coreLogic._Config;
        private ILogHandler _log => _coreLogic._Log;
        private IRandomHandler _random => _coreLogic.Random;
        private EntitySerializer _serializer => CoreLogic._Serializer;

        public static readonly Dictionary<string, string> KeypadPrefabClassIds = new Dictionary<string, string>
        {
            { "38135f4d-5f31-4438-abce-2c8bbbc5c77c", "EncyDesc_Aurora_RingRoom_Code_PDA" }, // GenericLab
            { "48a5564b-e632-4666-9e7c-f377fbc4fd23", "EncyDesc_Aurora_Office_PDA1" }, // CargoRoom
            { "3265d800-9ae0-478c-973c-ddf5351977c0", "EncyDesc_Aurora_Locker_PDA2" }, // LivingArea_Bedroom2
            { "19feccc5-36a0-431c-ae97-16f87c21d5af", "EncyDesc_CaptainCode" }, // CaptainsQuarters
        };

        private void Awake()
        {
            _coreLogic = GetComponent<CoreLogic>();
        }

        public void ApplySerializedChanges(EntitySerializer serializer) { }

        public void RandomiseOutOfLoop(EntitySerializer serializer)
        {
            if (_config.RandomiseDoorCodes.Value)
                RandomiseDoorCodes();
            if (_config.RandomiseSupplyBoxes.Value)
                RandomiseSupplyBoxes();
        }

        public bool RandomiseEntity(ref LogicEntity entity)
        {
            // This module randomises no entities.
            throw new NotImplementedException();
        }

        public void SetupHarmonyPatches(Harmony harmony)
        {
            if (CoreLogic._Serializer?.DoorKeyCodes?.Count > 0)
            {
                harmony.PatchAll(typeof(AuroraPatcher_KeyCodes)); 
                harmony.PatchAll(typeof(LanguagePatcher));
            }
            if (CoreLogic._Serializer?.SupplyBoxContents?.Count > 0)
                harmony.PatchAll(typeof(AuroraPatcher_SupplyBoxes));
        }

        /// <summary>
        /// Randomise the access codes for all doors in the Aurora.
        /// </summary>
        public void RandomiseDoorCodes()
        {
            Dictionary<string, string> keyCodes = new Dictionary<string, string>();
            foreach (string classId in KeypadPrefabClassIds.Keys)
            {
                string code = "0";
                // Keypads only have numbers 1-9, zeroes cannot be entered at all.
                while (code.Contains("0"))
                {
                    code = _random.Next(1111, 9999).ToString();
                }
                _log.Debug($"[AR] Assigning accessCode {code} to {classId}");
                keyCodes.Add(classId, code);
            }

            _serializer.DoorKeyCodes = keyCodes;
        }

        /// <summary>
        /// Prepare a new table of possible contents for supply boxes.
        /// </summary>
        public void RandomiseSupplyBoxes()
        {
            LootTable<TechType> table = new LootTable<TechType>
            {
                { TechType.Battery, 2 },
                { TechType.PowerCell, 1 },
                { TechType.Bleach, 3 },
                { TechType.Glass, 3 },
                { TechType.Lubricant, 3 },
                { TechType.TitaniumIngot, 1 },
                { TechType.FireExtinguisher, 1 },
                { TechType.FirstAidKit, 2 },
                { TechType.Pipe, 3 },
                { TechType.NutrientBlock, 3 },
                { TechType.DisinfectedWater, 3 },
                { TechType.FilteredWater, 2 },
                { TechType.SeamothSonarModule, 0.1 },
                { TechType.VehicleStorageModule, 0.1 },
                { TechType.CyclopsThermalReactorModule, 0.1 },
            };
            _serializer.SupplyBoxContents = table;
        }
    }
}