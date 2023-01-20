using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HarmonyLib;
using SubnauticaRandomiser.Handlers;
using SubnauticaRandomiser.Interfaces;
using SubnauticaRandomiser.Objects;
using SubnauticaRandomiser.Objects.Enums;
using SubnauticaRandomiser.Objects.Events;
using SubnauticaRandomiser.Patches;
using UnityEngine;
using ILogHandler = SubnauticaRandomiser.Interfaces.ILogHandler;

namespace SubnauticaRandomiser.Logic
{
    [RequireComponent(typeof(CoreLogic))]
    internal class DataboxLogic : MonoBehaviour, ILogicModule
    {
        private CoreLogic _coreLogic;
        private List<Databox> _databoxes;
        private ILogHandler _log;
        private IRandomHandler _random;

        public void Awake()
        {
            _coreLogic = GetComponent<CoreLogic>();
            _log = _coreLogic._Log;
            _random = _coreLogic.Random;
            
            // Register this module as a handler for databox entities.
            _coreLogic.RegisterEntityHandler(EntityType.Databox, this);
            // Register events.
            _coreLogic.CollectingEntities += OnCollectDataboxes;

            _coreLogic.RegisterFileLoadTask(ParseDataFileAsync());
        }

        public void RandomiseOutOfLoop(EntitySerializer serializer)
        {
            RandomiseDataboxes(serializer);
            UpdateBlueprints(_coreLogic.EntityHandler.GetAll());
            LinkCyclopsHullModules(_coreLogic.EntityHandler);
        }

        public bool RandomiseEntity(ref LogicEntity entity)
        {
            throw new NotImplementedException();
        }

        public void SetupHarmonyPatches(Harmony harmony)
        {
            harmony.PatchAll(typeof(DataboxPatcher));
        }

        /// <summary>
        /// Add databox entities into the main loop.
        /// </summary>
        private void OnCollectDataboxes(object sender, CollectEntitiesEventArgs args)
        {
            // TODO: convert databoxes to proper logicEntities on parse.
            // args.ToBeRandomised.AddRange();
        }

        /// <summary>
        /// Get a dictionary with a list of all databox depths, keyed to the TechType they belong to.
        /// </summary>
        public Dictionary<TechType, List<float>> GetDepthsByTechType(List<Databox> databoxes)
        {
            if (databoxes is null || databoxes.Count == 0)
                throw new ArgumentException("Cannot get databox depths: Databox list is null or invalid.");

            var databoxDepths = new Dictionary<TechType, List<float>>();
            foreach (Databox databox in databoxes)
            {
                float depth = databox.Coordinates.y;
                if (databoxDepths.ContainsKey(databox.TechType))
                {
                    databoxDepths[databox.TechType].Add(depth);
                }
                else
                {
                    List<float> depthList = new List<float> { depth };
                    databoxDepths.Add(databox.TechType, depthList);
                }
            }

            return databoxDepths;
        }

        /// <summary>
        /// Get a dictionary with every databox' accessibility requirements (like laser cutter).
        /// </summary>
        /// <returns>A dictionary keyed to TechTypes with an array, where idx 0 is the number of databoxes for the
        /// TechType, idx 1 is the number that require a laser cutter to access, and idx 2 is the number that require
        /// a propulsion cannon.</returns>
        public Dictionary<TechType, int[]> GetRequirementsByTechType(List<Databox> databoxes)
        {
            if (databoxes is null || databoxes.Count == 0)
                throw new ArgumentException("Cannot get databox depths: Databox list is null or invalid.");

            var requirements = new Dictionary<TechType, int[]>();
            foreach (Databox databox in databoxes)
            {
                if (!requirements.ContainsKey(databox.TechType))
                    requirements.Add(databox.TechType, new int[3]);

                // Count the databox itself.
                requirements[databox.TechType][0] += 1;
                // Count the laser cutters.
                if (databox.RequiresLaserCutter)
                    requirements[databox.TechType][1] += 1;
                // Count the propulsion cannons.
                if (databox.RequiresPropulsionCannon)
                    requirements[databox.TechType][2] += 1;
            }

            return requirements;
        }
        
        /// <summary>
        /// Cyclops hull modules are linked and unlock together once the blueprint for module1 is found. Synchronise
        /// hull modules 2 and 3 with module 1 here.
        /// </summary>
        /// <exception cref="ArgumentException">If the LogicEntity or databox for one of the hull modules cannot be
        /// found.</exception>
        public void LinkCyclopsHullModules(EntityHandler entityHandler)
        {
            LogicEntity mod1 = entityHandler.GetEntity(TechType.CyclopsHullModule1);
            LogicEntity mod2 = entityHandler.GetEntity(TechType.CyclopsHullModule2);
            LogicEntity mod3 = entityHandler.GetEntity(TechType.CyclopsHullModule3);

            if (mod1 is null || mod2 is null || mod3 is null)
                throw new ArgumentException("Tried to link Cyclops Hull Modules, but found null for entities.");
            
            mod2.Blueprint.UnlockDepth = mod1.Blueprint.UnlockDepth;
            mod3.Blueprint.UnlockDepth = mod1.Blueprint.UnlockDepth;

            mod2.Blueprint.UnlockConditions ??= new List<TechType>();
            mod3.Blueprint.UnlockConditions ??= new List<TechType>();

            if (mod1.Blueprint.UnlockConditions.Contains(TechType.LaserCutter))
            {
                mod2.Blueprint.UnlockConditions.Add(TechType.LaserCutter);
                mod3.Blueprint.UnlockConditions.Add(TechType.LaserCutter);
            }

            if (mod1.Blueprint.UnlockConditions.Contains(TechType.PropulsionCannon))
            {
                mod2.Blueprint.UnlockConditions.Add(TechType.PropulsionCannon);
                mod3.Blueprint.UnlockConditions.Add(TechType.PropulsionCannon);
            }

            _log.Debug("Linked Cyclops Hull Modules.");
        }
        
        private async Task ParseDataFileAsync()
        {
            _databoxes = await CSVReader.ParseDataFileAsync(Initialiser._WreckageFile, CSVReader.ParseWreckageLine);
        }

        /// <summary>
        /// Randomise (shuffle) the blueprints found inside databoxes.
        /// </summary>
        /// <returns>The list of newly randomised databoxes.</returns>
        public List<Databox> RandomiseDataboxes(EntitySerializer serializer)
        {
            serializer.Databoxes = new Dictionary<RandomiserVector, TechType>();
            List<Databox> randomDataboxes = new List<Databox>();
            List<Vector3> toBeRandomised = new List<Vector3>();

            foreach (Databox databox in _databoxes)
            {
                toBeRandomised.Add(databox.Coordinates);
            }

            foreach (Databox originalBox in _databoxes)
            {
                Vector3 next = _random.Choice(toBeRandomised);
                Databox replacementBox = _databoxes.Find(x => x.Coordinates.Equals(next));

                randomDataboxes.Add(new Databox(originalBox.TechType, next, replacementBox.Wreck, 
                    replacementBox.RequiresLaserCutter, replacementBox.RequiresPropulsionCannon));
                serializer.Databoxes.Add(new RandomiserVector(next), originalBox.TechType);
                _log.Debug($"[D] Databox {next.ToString()} with {replacementBox}"
                           + " now contains " + originalBox);
                toBeRandomised.Remove(next);
            }

            _databoxes = randomDataboxes;
            return randomDataboxes;
        }

        /// <summary>
        /// After databoxes were randomised, ensure all blueprints' unlock requirements are updated to reflect the new
        /// positions.
        /// </summary>
        /// <param name="blueprintEntities">A list of entities with blueprints to be updated.</param>
        /// <param name="requirementThreshold">The proportion of databoxes that must require something for it to be
        /// added as a new unlock requirement to the blueprint itself.</param>
        /// <exception cref="ArgumentException">Raised if databoxes weren't randomised in the given Logic.</exception>
        public void UpdateBlueprints(List<LogicEntity> blueprintEntities, float requirementThreshold = 0.5f)
        {
            if (_databoxes is null || _databoxes.Count == 0)
                throw new ArgumentException("Cannot update databox unlocks: Databox list is null or empty.");
            
            Dictionary<TechType, List<float>> depths = GetDepthsByTechType(_databoxes);
            Dictionary<TechType, int[]> requirements = GetRequirementsByTechType(_databoxes);
            
            // Update the average depth required to unlock each blueprint.
            foreach (var kv in depths)
            {
                TechType techType = kv.Key;
                List<float> depth = kv.Value;

                LogicEntity entity = blueprintEntities.Find(e => e.TechType.Equals(techType));
                if (entity is null)
                    throw new ArgumentException($"Failed to find entity for TechType {techType} in provided list!");
                entity.Blueprint ??= new Blueprint(entity.TechType);
                entity.Blueprint.UnlockDepth = (int)depth.Average();
            }
            
            // Update the requirements for unlocking each blueprint.
            foreach (var kv in requirements)
            {
                TechType techType = kv.Key;
                int[] reqs = kv.Value;
                
                LogicEntity entity = blueprintEntities.Find(e => e.TechType.Equals(techType));
                entity.Blueprint.UnlockConditions ??= new List<TechType>();
                if (((float)reqs[1] / reqs[0]) >= requirementThreshold)
                    entity.Blueprint.UnlockConditions.Add(TechType.LaserCutter);
                if (((float)reqs[2] / reqs[0]) >= requirementThreshold)
                    entity.Blueprint.UnlockConditions.Add(TechType.PropulsionCannon);
            }
            
            _log.Debug($"[D] Updated unlock requirements for {_databoxes.Count} databoxes.");
        }
    }
}