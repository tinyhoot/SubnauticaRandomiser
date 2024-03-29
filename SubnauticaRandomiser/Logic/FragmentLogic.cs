﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HarmonyLib;
using Nautilus.Handlers;
using SubnauticaRandomiser.Configuration;
using SubnauticaRandomiser.Interfaces;
using SubnauticaRandomiser.Objects;
using SubnauticaRandomiser.Objects.Enums;
using SubnauticaRandomiser.Objects.Events;
using SubnauticaRandomiser.Objects.Exceptions;
using SubnauticaRandomiser.Patches;
using UnityEngine;
using static LootDistributionData;
using ILogHandler = SubnauticaRandomiser.Interfaces.ILogHandler;

namespace SubnauticaRandomiser.Logic
{
    /// <summary>
    /// Handles everything related to randomising fragments.
    /// </summary>
    [RequireComponent(typeof(CoreLogic), typeof(ProgressionManager))]
    internal class FragmentLogic : MonoBehaviour, ILogicModule
    {
        private CoreLogic _coreLogic;
        private ProgressionManager _manager;
        private Config _config;
        private ILogHandler _log;
        private EntitySerializer _serializer => CoreLogic._Serializer;
        private IRandomHandler _random;
        
        private static Dictionary<TechType, List<string>> _classIdDatabase;
        private List<Biome> _allBiomes;
        private List<Biome> _availableBiomes;
        private static readonly Dictionary<string, TechType> _fragmentDataPaths = new Dictionary<string, TechType>
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
            { "exosuit_damaged_01", TechType.ExosuitFragment },
            { "exosuit_damaged_02", TechType.ExosuitFragment },
            { "exosuit_damaged_03", TechType.ExosuitFragment },
            { "exosuit_damaged_06", TechType.ExosuitFragment },
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

        public void Awake()
        {
            _coreLogic = GetComponent<CoreLogic>();
            _manager = GetComponent<ProgressionManager>();
            _config = _coreLogic._Config;
            _log = _coreLogic._Log;
            _random = _coreLogic.Random;

            // Register events.
            _manager.SetupPriority += OnSetupPriorityEntities;
            _manager.SetupProgression += OnSetupProgression;
            _manager.SetupVehicles += OnSetupVehicles;
            
            if (_config.RandomiseFragments.Value)
            {
                _coreLogic.EntityCollecting += OnCollectFragments;
                _manager.HasProgressed += OnProgression;
                // Handle any fragment entities using this component.
                _coreLogic.RegisterEntityHandler(EntityType.Fragment, this);
                // Reset all existing fragment spawns.
                Init();
            }
            
            _coreLogic.RegisterFileLoadTask(ParseDataFileAsync());
        }
        
        /// <summary>
        /// Re-apply spawnList from a saved game. This will fail to catch all existing fragment spawns if called in a
        /// previously randomised game.
        /// </summary>
        public void ApplySerializedChanges(EntitySerializer serializer)
        {
            if (serializer.SpawnDataDict?.Count > 0)
            {
                Init();
                foreach (TechType key in serializer.SpawnDataDict.Keys)
                {
                    foreach (SpawnData spawnData in serializer.SpawnDataDict[key])
                    {
                        LootDistributionHandler.EditLootDistributionData(spawnData.ClassId, spawnData.GetBaseBiomeData());
                    }
                }
            }

            if (serializer.NumFragmentsToUnlock?.Count > 0)
            {
                foreach (TechType key in serializer.NumFragmentsToUnlock.Keys)
                {
                    PDAHandler.EditFragmentsToScan(key, serializer.NumFragmentsToUnlock[key]);
                }
            }
        }

        public void RandomiseOutOfLoop(EntitySerializer serializer)
        {
            if (_config.RandomiseNumFragments.Value)
                RandomiseNumFragments(_coreLogic.EntityHandler.GetAllFragments());
            // Randomise duplicate scan rewards.
            if (_config.RandomiseDuplicateScans.Value)
                CreateDuplicateScanYieldDict();
        }

        public bool RandomiseEntity(ref LogicEntity entity)
        {
            if (!_classIdDatabase.TryGetValue(entity.TechType, out List<string> idList))
                throw new ArgumentException($"Failed to find fragment '{entity}' in classId database!");
            
            // Check whether the fragment fulfills its prerequisites.
            if (!entity.IsPriority && entity.AccessibleDepth > _manager.ReachableDepth)
            {
                _log.Debug($"[F] --- Fragment [{entity}] did not fulfill requirements, skipping.");
                return false;
            }
            
            _log.Debug($"[F] Randomising fragment {entity} for depth {_manager.ReachableDepth}");
            List<SpawnData> spawnList = new List<SpawnData>();

            // Determine how many different biomes the fragment should spawn in.
            int biomeCount = _random.Next(3, _config.MaxBiomesPerFragment.Value + 1);

            for (int i = 0; i < biomeCount; i++)
            {
                // Choose a random biome.
                Biome biome = ChooseBiome(spawnList, _manager.ReachableDepth);
                
                // Calculate spawn rate.
                float spawnRate = CalcFragmentSpawnRate(biome);
                float[] splitRates = SplitFragmentSpawnRate(spawnRate, idList.Count);

                // Split the spawn rate among each variation (prefab) of the fragment.
                for (int j = 0; j < idList.Count; j++)
                {
                    // Add to an existing entry if it already exists from a previous loop.
                    SpawnData spawnData = spawnList.Find(x => x.ClassId.Equals(idList[j]));
                    spawnData ??= new SpawnData(idList[j]);
                    
                    RandomiserBiomeData data = new RandomiserBiomeData
                    {
                        Biome = biome.Variant,
                        Count = 1,
                        Probability = splitRates[j]
                    };
                    spawnData.AddBiomeData(data);
                    spawnList.Add(spawnData);
                }

                _log.Debug($"[F] + Adding fragment to biome: {biome.Variant.AsString()}, {spawnRate}");
            }

            ApplyRandomisedFragment(entity, spawnList);
            return true;
        }

        public void SetupHarmonyPatches(Harmony harmony)
        {
            if (CoreLogic._Serializer?.FragmentMaterialYield?.Count > 0)
                harmony.PatchAll(typeof(FragmentPatcher));
        }

        /// <summary>
        /// Queue up all fragments to be randomised.
        /// </summary>
        private void OnCollectFragments(object sender, CollectEntitiesEventArgs args)
        {
            args.ToBeRandomised.AddRange(_coreLogic.EntityHandler.GetAllFragments());
        }

        /// <summary>
        /// Once lasercutter and propulsion cannon are randomised, add their locked biomes.
        /// </summary>
        private void OnProgression(object sender, EntityEventArgs args)
        {
            TechType techType = args.LogicEntity.TechType;
            if (techType.Equals(TechType.LaserCutter) || techType.Equals(TechType.LaserCutterFragment))
                AddLaserCutterBiomes();
            if (techType.Equals(TechType.RadiationSuit))
                AddAuroraBiomes();
        }

        /// <summary>
        /// Ensure that certain fragments are always randomised by a certain depth.
        /// </summary>
        private void OnSetupPriorityEntities(object sender, SetupPriorityEventArgs args)
        {
            // Ensure this setup is only done when the event is called from the manager itself.
            if (!(sender is ProgressionManager manager))
                return;

            manager.AddEssentialEntities(0, new[] { TechType.SeaglideFragment });
            manager.AddEssentialEntities(100, new[] { TechType.LaserCutterFragment });
            manager.AddEssentialEntities(200, new[] { TechType.BaseBioReactorFragment });
        }

        /// <summary>
        /// Mark items which let you access more fragments as priority items.
        /// </summary>
        private void OnSetupProgression(object sender, SetupProgressionEventArgs args)
        {
            HashSet<TechType> additions = new HashSet<TechType>
            {
                TechType.LaserCutter,
                TechType.PropulsionCannon,
                TechType.RadiationSuit,
                TechType.RepulsionCannon,
                TechType.Welder
            };
            args.ProgressionEntities.AddRange(additions);
            // Limit this to only if fragment entities are part of the logic, else this will make the main loop hang.
            if (_config.RandomiseFragments.Value)
            {
                args.ProgressionEntities.Add(TechType.LaserCutterFragment);
                args.ProgressionEntities.Add(TechType.PropulsionCannonFragment);
            }
            // If fragments are randomised but recipes are not, force more depth calculations for fragments.
            if (_config.RandomiseFragments.Value && (!_config.EnableRecipeModule.Value || !_config.RandomiseRecipes.Value))
            {
                var fragmentAdditions = new[]
                {
                    TechType.SeaglideFragment,
                    TechType.SeamothFragment,
                    TechType.ExosuitFragment,
                    TechType.CyclopsBridgeFragment,
                    TechType.CyclopsEngineFragment,
                    TechType.CyclopsHullFragment
                };
                args.ProgressionEntities.AddRange(fragmentAdditions);
            }
        }

        /// <summary>
        /// Change vehicle depths if recipes are not randomised.
        /// </summary>
        private void OnSetupVehicles(object sender, SetupVehiclesEventArgs args)
        {
            // If fragments are randomised but recipes are not, there is no way of achieving lower depths.
            // Define new vehicle depths to counteract that.
            if (_config.RandomiseFragments.Value && (!_config.EnableRecipeModule.Value || !_config.RandomiseRecipes.Value))
            {
                args.VehicleDepths.Add(new[] { TechType.SeaglideFragment }, 50);
                args.VehicleDepths.Add(new[] { TechType.SeamothFragment }, 100);
                args.VehicleDepths.Add(new[] { TechType.ExosuitFragment }, 1700);
                args.VehicleDepths.Add(new[]
                {
                    TechType.CyclopsBridgeFragment,
                    TechType.CyclopsEngineFragment,
                    TechType.CyclopsHullFragment
                }, 1700);
            }
        }

        /// <summary>
        /// Add all biomes inside the Aurora to the list of available biomes.
        /// </summary>
        private void AddAuroraBiomes()
        {
            var additions = _allBiomes.FindAll(biome => biome.Name.Contains("Ship"));
            _availableBiomes.AddRange(additions);
        }

        /// <summary>
        /// Add all biomes that are locked behind needing a laser cutter to the list of available biomes.
        /// </summary>
        private void AddLaserCutterBiomes()
        {
            var additions = _allBiomes.Where(x => x.Name.ToLower().Contains("barrier"));
            _availableBiomes.AddRange(additions);
        }

        /// <summary>
        /// Calculate the spawn rate for an entity in the given biome.
        /// </summary>
        /// <param name="biome">The biome.</param>
        /// <returns>The spawn rate.</returns>
        private float CalcFragmentSpawnRate(Biome biome)
        {
            // Choose a percentage of the biome's combined original spawn rates.
            float percentage = _config.FragmentSpawnChanceMult.Value + (float)_random.NextDouble();
            // If the number of scans needed per fragment is very high, increase the spawn rate proportionally.
            int maxFragments = (int)_config.MaxFragmentsToUnlock.Entry.DefaultValue;
            if (_config.MaxFragmentsToUnlock.Value > maxFragments)
                percentage += 0.04f * (_config.MaxFragmentsToUnlock.Value - maxFragments);
            
            return percentage * biome.FragmentRate ?? 0.0f;
        }

        /// <summary>
        /// Change the number of fragments needed to unlock the blueprint to the given entity.
        /// </summary>
        /// <param name="entity">The entity that is unlocked on scan completion.</param>
        private void ChangeNumFragmentsToUnlock(LogicEntity entity)
        {
            if (!_config.RandomiseNumFragments.Value)
                return;
            
            int numFragments = _random.Next(_config.MinFragmentsToUnlock.Value, _config.MaxFragmentsToUnlock.Value + 1);
            _log.Debug($"[F] New number of fragments required for {entity}: {numFragments}");
            _serializer.AddFragmentUnlockNum(entity.TechType, numFragments);
        }

        /// <summary>
        /// Choose a suitable biome which is also accessible at this depth, and has not been chosen before.
        /// </summary>
        /// <param name="previousChoices">The list of SpawnData resulting from previously chosen biomes.</param>
        /// <param name="depth">The maximum depth to consider.</param>
        /// <returns>The chosen biome.</returns>
        private Biome ChooseBiome(List<SpawnData> previousChoices, int depth)
        {
            List<Biome> choices = _availableBiomes.FindAll(bio =>
                bio.AverageDepth <= depth && !previousChoices.Any(sd => sd.ContainsBiome(bio.Variant)));
            
            // In case no good biome is available, ignore overpopulation restrictions and choose any.
            if (choices.Count == 0)
            {
                _log.Debug("[F] ! No valid biome choices, using fallback");
                choices = _allBiomes.FindAll(x => x.AverageDepth <= depth);
                if (choices.Count == 0)
                    throw new RandomisationException("No valid biome options for depth " + depth);
            }

            Biome biome = _random.Choice(choices);
            biome.Used++;

            // Remove the biome from the pool if it gets too populated.
            if (biome.Used == _config.MaxFragmentTypesPerBiome.Value)
                _availableBiomes.Remove(biome);

            return biome;
        }

        /// <summary>
        /// Set up the dictionary of possible rewards for scanning an already unlocked fragment.
        /// </summary>
        private void CreateDuplicateScanYieldDict()
        {
            LootTable<TechType> loot = new LootTable<TechType>();
            var materials = _coreLogic.EntityHandler.GetAllRawMaterials(50);
            // Gaining seeds from fragments is not great for balance. Remove that.
            materials.Remove(_coreLogic.EntityHandler.GetEntity(TechType.CreepvineSeedCluster));

            foreach (LogicEntity entity in materials)
            {
                // Two random calls will tend to produce less extreme and more evenly distributed values.
                double weight = _random.NextDouble() + _random.NextDouble();
                loot.Add(entity.TechType, weight);
            }
            
            // Additionally, add some spicy rare rewards.
            double rareWeight = Math.Max(loot.TotalWeights() * _config.RareDropChance.Value, 0.01);
            loot.Add(TechType.SeamothTorpedoModule, rareWeight);
            loot.Add(TechType.VehicleStorageModule, rareWeight);
            loot.Add(TechType.ExosuitJetUpgradeModule, rareWeight);
            loot.Add(TechType.PrecursorIonCrystal, rareWeight * 2);

            _serializer.FragmentMaterialYield = loot;
        }
        
        /// <summary>
        /// Force Subnautica and SMLHelper to index and cache the classIds, setup the databases, and prepare a blank
        /// slate by removing all existing fragment spawns from the game.
        /// </summary>
        private static void Init()
        {
            // This forces SMLHelper (and the game) to cache the classIds.
            // Without this, anything below will fail.
            _ = CraftData.GetClassIdForTechType(TechType.Titanium);

            PrepareClassIdDatabase();
            ResetFragmentSpawns();
        }
        
        private async Task ParseDataFileAsync()
        {
            List<Biome> biomes = await CSVReader.ParseDataFileAsync(Initialiser._BiomeFile, CSVReader.ParseBiomeLine);
            // Set up the lists of biomes.
            _allBiomes = biomes.Where(b => b.FragmentRate != null).ToList();
            _availableBiomes = _allBiomes.FindAll(b => !b.Name.ToLower().Contains("barrier")
                                                       && !b.Name.Contains("Ship"));
        }
        
        /// <summary>
        /// Assemble a dictionary of all relevant prefabs with their unique classId identifier.
        /// </summary>
        private static void PrepareClassIdDatabase()
        {
            _classIdDatabase = new Dictionary<TechType, List<string>>();

            // Get the unique identifier of every single prefab currently loaded
            // by the game.
            var keys = UWE.PrefabDatabase.prefabFiles.Keys;

            foreach (string classId in keys)
            {
                string dataPath = UWE.PrefabDatabase.prefabFiles[classId];
                //InitMod._log.Debug($"KEY: {classId}, VALUE: {UWE.PrefabDatabase.prefabFiles[classId]}");

                // If the prefab is not part of the predefined dictionary of fragments,
                // discard it and continue. Acts as a filter for only those fragments
                // which have actual BiomeData used by the game.
                if (!_fragmentDataPaths.TryGetValue(Path.GetFileNameWithoutExtension(dataPath), out TechType type))
                    continue;

                if (!_classIdDatabase.ContainsKey(type))
                    _classIdDatabase.Add(type, new List<string> { classId });
                else
                    _classIdDatabase[type].Add(classId);
            }
        }
        
        /// <summary>
        /// Change the number of scans required to unlock the blueprint for all fragments.
        /// </summary>
        /// <param name="fragments">The list of fragments to change scan numbers for.</param>
        private void RandomiseNumFragments(List<LogicEntity> fragments)
        {
            foreach (LogicEntity entity in fragments)
            {
                ChangeNumFragmentsToUnlock(entity);
            }
        }
        
        /// <summary>
        /// Go through all the BiomeData in the game and reset any fragment spawn rates to 0.0f, effectively "deleting"
        /// them from the game until the randomiser has decided on a new distribution.
        /// </summary>
        private static void ResetFragmentSpawns()
        {
            //_log.Debug("---Resetting vanilla fragment spawn rates---");

            // For the rest of all the randomisation, we need TechTypes to classId.
            // Unfortunately, for just this once, we need the opposite.
            Dictionary<string, TechType> fragmentDatabase = ReverseClassIdDatabase();

            // Grab a copy of all vanilla BiomeData. This loads it fresh from disk
            // and will thus be unaffected by any existing randomisation.
            LootDistributionData loot = LootDistributionData.Load(LootDistributionData.dataPath);

            foreach (KeyValuePair<BiomeType, DstData> keyValuePair in loot.dstDistribution)
            {
                BiomeType biome = keyValuePair.Key;
                DstData dstData = keyValuePair.Value;

                foreach (PrefabData prefab in dstData.prefabs)
                {
                    // Ensure the prefab is actually a fragment.
                    if (fragmentDatabase.ContainsKey(prefab.classId))
                    {
                        // Whatever spawn chance there was before, set it to 0.
                        LootDistributionHandler.EditLootDistributionData(prefab.classId, biome, 0f, 0);
                    }
                }
            }
            //_log.Debug("---Completed resetting vanilla fragment spawn rates---");
        }
        
        /// <summary>
        /// Reverse the classId dictionary to allow for ID to TechType matching.
        /// </summary>
        /// <returns>The inverted dictionary.</returns>
        private static Dictionary<string, TechType> ReverseClassIdDatabase()
        {
            Dictionary<string, TechType> database = new Dictionary<string, TechType>();

            foreach (KeyValuePair<TechType, List<string>> kv in _classIdDatabase)
            {
                foreach (string classId in kv.Value)
                {
                    // Ensure no duplicates.
                    if (!database.ContainsKey(classId))
                    {
                        database.Add(classId, kv.Key);
                        //LogHandler.Debug("Added to reversed fragment database: " + kv.Key.AsString());
                    }
                }
            }

            return database;
        }
        
        /// <summary>
        /// Split a fragment's spawn rate into a number of randomly sized parts.
        /// </summary>
        /// <param name="spawnRate">The spawn rate.</param>
        /// <param name="parts">The number of parts to split into.</param>
        /// <returns>An array containing each part's spawn rate.</returns>
        /// <exception cref="ArgumentException">Raised if parts is smaller than 1.</exception>
        private float[] SplitFragmentSpawnRate(float spawnRate, int parts)
        {
            if (parts < 1)
                throw new ArgumentException("Cannot split spawn rate into less than one pieces!");
            if (parts == 1)
                return new[] { spawnRate };

            // Initially, get some random values.
            float[] result = new float[parts];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = (float)_random.NextDouble();
            }

            // Adjust the values so they sum up to spawnRate.
            float adjust = spawnRate / result.Sum();
            for (int i = 0; i < result.Length; i++)
            {
                result[i] *= adjust;
            }

            return result;
        }

        /// <summary>
        /// Add modified SpawnData to the game and any place it needs to go to be stored for later use.
        /// </summary>
        /// <param name="entity">The entity to modify spawn rates for.</param>
        /// <param name="spawnList">The list of modified SpawnData to use.</param>
        private void ApplyRandomisedFragment(LogicEntity entity, List<SpawnData> spawnList)
        {
            entity.SpawnData = spawnList;
            _serializer.AddSpawnData(entity.TechType, spawnList);
        }
    }
}
