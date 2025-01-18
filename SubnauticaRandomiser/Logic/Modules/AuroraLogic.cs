using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HarmonyLib;
using HootLib.Objects;
using Nautilus.Handlers;
using SubnauticaRandomiser.Configuration;
using SubnauticaRandomiser.Handlers;
using SubnauticaRandomiser.Interfaces;
using SubnauticaRandomiser.Objects;
using SubnauticaRandomiser.Patches;
using SubnauticaRandomiser.Serialization;
using SubnauticaRandomiser.Serialization.Modules;
using UnityEngine;
using ILogHandler = HootLib.Interfaces.ILogHandler;

namespace SubnauticaRandomiser.Logic.Modules
{
    /// <summary>
    /// Handles randomising everything in and around the Aurora.
    /// </summary>
    [RequireComponent(typeof(CoreLogic))]
    internal class AuroraLogic : MonoBehaviour, ILogicModule
    {
        private CoreLogic _coreLogic;

        private Config _config => _coreLogic._Config;
        private ILogHandler _log;
        private NautilusShell<string, string> _languageCache;

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
            _log = PrefixLogHandler.Get("[Aurora]");
        }
        
        public IEnumerable<Task> LoadFiles()
        {
            return Enumerable.Empty<Task>();
        }

        public BaseModuleSaveData SetupSaveData()
        {
            return null;
        }

        public void ApplySerializedChanges(SaveData saveData)
        {
            if (saveData.TryGetModuleData(out DoorSaveData doorData))
            {
                // This ensures the replacement works no matter what language the user uses.
                // However, it won't persist to a different one if the language is changed mid-game.
                string language = Language.main.currentLanguage;
                _languageCache = new NautilusShell<string, string>(
                    (key, text) => LanguageHandler.SetLanguageLine(key, text, language), Language.main.Get);
                PatchAccessCodeEntries(doorData);
            }
        }
        
        /// <summary>
        /// Edit the description of the PDA logs on door access codes to reflect the codes they were randomised to.
        /// </summary>
        public void PatchAccessCodeEntries(DoorSaveData saveData)
        {
            foreach (var kv in saveData.DoorKeyCodes)
            {
                string descId = KeypadPrefabClassIds[kv.Key];
                string originalDesc = Language.main.Get(descId);
                // Search for four numbers in a row and replace them with the randomised ones.
                string newDesc = Regex.Replace(originalDesc, "[0-9]{4}", kv.Value, RegexOptions.CultureInvariant);
                _languageCache.SendChanges(descId, newDesc);
            }
        }

        public void UndoSerializedChanges(SaveData saveData)
        {
            if (saveData.Contains<DoorSaveData>())
                _languageCache?.UndoChanges();
        }

        public void RandomiseOutOfLoop(IRandomHandler rng, SaveData saveData)
        {
            if (_config.RandomiseDoorCodes.Value)
                saveData.AddModuleData(RandomiseDoorCodes(rng));
            if (_config.RandomiseSupplyBoxes.Value)
                saveData.AddModuleData(RandomiseSupplyBoxes());
        }

        public bool RandomiseEntity(IRandomHandler rng, ref LogicEntity entity)
        {
            // This module randomises no entities.
            throw new NotImplementedException();
        }

        public void SetupHarmonyPatches(Harmony harmony, SaveData saveData)
        {
            if (saveData.Contains<DoorSaveData>())
            {
                harmony.PatchAll(typeof(AuroraPatcher_KeyCodes));
            }
            if (saveData.Contains<SupplyBoxSaveData>())
                harmony.PatchAll(typeof(AuroraPatcher_SupplyBoxes));
        }

        /// <summary>
        /// Randomise the access codes for all doors in the Aurora.
        /// </summary>
        private DoorSaveData RandomiseDoorCodes(IRandomHandler rng)
        {
            Dictionary<string, string> keyCodes = new Dictionary<string, string>();
            foreach (string classId in KeypadPrefabClassIds.Keys)
            {
                string code = "0";
                // Keypads only have numbers 1-9, zeroes cannot be entered at all.
                while (code.Contains("0"))
                {
                    code = rng.Next(1111, 9999).ToString();
                }
                _log.Debug($"Assigning accessCode {code} to {classId}");
                keyCodes.Add(classId, code);
            }
            
            return new DoorSaveData { DoorKeyCodes = keyCodes };
        }

        /// <summary>
        /// Prepare a new table of possible contents for supply boxes.
        /// </summary>
        private SupplyBoxSaveData RandomiseSupplyBoxes()
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
            return new SupplyBoxSaveData { LootTable = table };
        }
    }
}