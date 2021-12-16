using System;
using System.Collections.Generic;
using System.IO;
using SMLHelper.V2.Handlers;
using static LootDistributionData;

namespace SubnauticaRandomiser.Logic
{
    internal class FragmentLogic
    {
        private Dictionary<TechType, List<string>> _classIdDatabase;
        private readonly Dictionary<string, TechType> _fragmentDataPaths = new Dictionary<string, TechType>
        {
            { "BaseBioReactor_Fragment", TechType.BaseBioReactorFragment },
            { "BaseNuclearReactor_Fragment", TechType.BaseNuclearReactorFragment },
            { "BatteryCharger_Fragment", TechType.BatteryChargerFragment },
            { "Beacon_Fragment", TechType.BeaconFragment },
            { "Constructor_Fragment", TechType.ConstructorFragment },
            { "Constructor_Fragment_InCrate", TechType.ConstructorFragment },
            { "CyclopsBridge_Fragment", TechType.CyclopsBridgeFragment },
            { "CyclopsEngine_Fragment", TechType.CyclopsEngineFragment },
            { "CyclopsHull_Fragment_Large", TechType.CyclopsHullFragment },
            { "CyclopsHull_Fragment_Medium", TechType.CyclopsHullFragment },
            { "ExosuitDrillArmfragment", TechType.ExosuitDrillArmFragment },
            { "ExosuitGrapplingArmfragment", TechType.ExosuitGrapplingArmFragment },
            { "ExosuitPropulsionArmfragment", TechType.ExosuitPropulsionArmFragment },
            { "ExosuitTorpedoArmfragment", TechType.ExosuitTorpedoArmFragment },
            { "GravSphere_Fragment", TechType.GravSphereFragment },
            { "LaserCutterFragment", TechType.LaserCutterFragment },
            { "LaserCutterFragment_InCrate", TechType.LaserCutterFragment },
            { "ledlightfragment", TechType.LEDLightFragment },
            { "moonpoolfragment", TechType.MoonpoolFragment },
            { "PowerCellCharger_Fragment", TechType.PowerCellChargerFragment },
            { "powertransmitterfragment", TechType.PowerTransmitterFragment },
            { "PropulsionCannonJunkFragment", TechType.PropulsionCannonFragment },
            { "scannerroomfragment", TechType.BaseMapRoomFragment },
            { "SeaglideJunkFragment", TechType.SeaglideFragment },
            { "Seamoth_Fragment", TechType.SeamothFragment },
            { "StasisRifleJunkFragment", TechType.StasisRifleFragment },
            { "ThermalPlant_Fragment", TechType.ThermalPlantFragment },
            { "Workbench_Fragment", TechType.WorkbenchFragment }
        };

        internal FragmentLogic()
        {
        }

        internal void Test()
        {
            LogHandler.Debug("titanium "+GetClassId(TechType.Titanium));
            LogHandler.Debug("peeper "+GetClassId(TechType.Peeper));
            LogHandler.Debug("reaper " +GetClassId(TechType.ReaperLeviathan));
            LogHandler.Debug("acid "+GetClassId(TechType.AcidMushroom));
            LogHandler.Debug("seamothfragment "+GetClassId(TechType.SeamothFragment));
            LogHandler.Debug("cyclopshullfragment "+GetClassId(TechType.CyclopsHullFragment));

            PrepareClassIdDatabase();

            BiomeData b = new BiomeData();
            b.biome = BiomeType.SafeShallows_Grass;
            b.count = 1;
            b.probability = 0.8f;

            BiomeData b2 = new BiomeData();
            b2.biome = BiomeType.GrassyPlateaus_TechSite_Scattered;
            b2.count = 1;
            b2.probability = 0.9f;

            List<BiomeData> list = new List<BiomeData>();
            list.Add(b);
            list.Add(b2);

            EditBiomeData(_classIdDatabase[TechType.BaseNuclearReactorFragment][0], list);
            DumpBiomeDataEntities();
        }

        internal void EditBiomeData(string classId, List<LootDistributionData.BiomeData> distribution)
        {
            LootDistributionHandler.EditLootDistributionData(classId, distribution);
        }

        internal string GetClassId(TechType type)
        {
            string classId = null;

            classId = CraftData.GetClassIdForTechType(type);

            return classId;
        }

        // Assemble a dictionary of all relevant prefabs with their unique classId
        // identifier.
        private void PrepareClassIdDatabase()
        {
            _classIdDatabase = new Dictionary<TechType, List<string>>();

            // Get the unique identifier of every single prefab currently loaded
            // by the game.
            var keys = UWE.PrefabDatabase.prefabFiles.Keys;

            foreach (string classId in keys)
            {
                string dataPath = UWE.PrefabDatabase.prefabFiles[classId];

                // If the prefab is not part of the predefined dictionary of fragments,
                // discard it and continue. Acts as a filter for only those fragments
                // which have actual BiomeData used by the game.
                if (!_fragmentDataPaths.TryGetValue(Path.GetFileName(dataPath), out TechType type))
                    continue;

                if (!_classIdDatabase.ContainsKey(type))
                    _classIdDatabase.Add(type, new List<string> { classId });
                else
                    _classIdDatabase[type].Add(classId);

                LogHandler.Debug("KEY: " + classId + ", VALUE: " + UWE.PrefabDatabase.prefabFiles[classId] + ", TECHTYPE: " + type.AsString());
            }
        }

        // This is really just for testing purposes.
        internal void DumpBiomeDataEntities()
        {
            LogHandler.Debug("---Dumping BiomeData---");

            // Grab a copy of all vanilla BiomeData. This loads it fresh from disk
            // and will thus be unaffected by any existing randomisation.
            LootDistributionData loot = LootDistributionData.Load(LootDistributionData.dataPath);
            var keys = UWE.PrefabDatabase.prefabFiles.Keys;

            LogHandler.Debug("---Dumping valid prefabs");
            foreach (string classId in keys)
            {
                if (!loot.GetPrefabData(classId, out SrcData data))
                    continue;
                
                // Any prefab with BiomeData will end up in the log files. This is
                // the case even if that BiomeData specifies 0.0 spawn chance across
                // the board and is thus "empty".
                LogHandler.Debug("KEY: " + classId + ", VALUE: " + UWE.PrefabDatabase.prefabFiles[classId]);
            }

            LogHandler.Debug("---Dumping Biomes");
            BiomeType[] biomes = (BiomeType[])Enum.GetValues(typeof(BiomeType));
            foreach (BiomeType biome in biomes)
            {
                if (loot.GetBiomeLoot(biome, out DstData distributionData))
                {
                    int valid = 0;
                    int validFragments = 0;
                    float sum = 0f;
                    float sumFragments = 0f;
                    foreach (var prefab in distributionData.prefabs)
                    {
                        if (string.IsNullOrEmpty(prefab.classId) || prefab.classId.Equals("None"))
                            continue;

                        valid++;
                        sum += prefab.probability;

                        if (UWE.WorldEntityDatabase.TryGetInfo(prefab.classId, out UWE.WorldEntityInfo info)){
                            if (info != null && !info.techType.Equals(TechType.None) && info.techType.AsString().ToLower().Contains("fragment"))
                            {
                                validFragments++;
                                sumFragments += prefab.probability;
                            }
                        }
                    }
                    //LogHandler.Debug("BIOME: " + biome.AsString() + ", VALID ENTRIES: " + valid + ", SUM: " + sum + ", OF WHICH FRAGMENTS: " + sumFragments);
                    LogHandler.Debug(string.Format("{0}\t{1} entries\t{2} fragments\t{3} totalspawnrate\t{4} totalfragmentrate", biome.AsString(), valid, validFragments, sum, sumFragments));
                }
                else
                {
                    //LogHandler.Debug("No DstData for biome " + biome.AsString());
                    LogHandler.Debug(string.Format("{0}\tNONE\t\t", biome.AsString()));
                }
            }
        }
    }
}
