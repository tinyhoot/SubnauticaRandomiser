using System;
using System.Collections.Generic;
using System.Linq;
using SubnauticaRandomiser.Objects;
using SubnauticaRandomiser.Objects.Events;
using UnityEngine;
using ILogHandler = SubnauticaRandomiser.Interfaces.ILogHandler;

namespace SubnauticaRandomiser.Logic
{
    [RequireComponent(typeof(CoreLogic))]
    internal class ProgressionManager : MonoBehaviour
    {
        private CoreLogic _coreLogic;
        private RandomiserConfig _config;
        private ILogHandler _log;

        private Dictionary<int, List<TechType>> _essentialEntities;
        private Dictionary<int, List<TechType[]>> _electiveEntities;
        private HashSet<TechType> _progressionEntities;
        private HashSet<TechType> _unlockedProgressionEntities;
        private Dictionary<TechType[], int> _vehicleDepths;

        public int ReachableDepth { get; private set; }

        /// <summary>
        /// Invoked when entities are marked as mandatory or elective for randomisation by certain depths. Use this
        /// to add your own.
        /// </summary>
        public event EventHandler<SetupPriorityEventArgs> SetupPriority;

        /// <summary>
        /// Invoked when entities are marked as progression items. Use this to add your own.
        /// </summary>
        public event EventHandler<SetupProgressionEventArgs> SetupProgression;

        /// <summary>
        /// Invoked when an entity that was previously marked as a progression item is successfully randomised. Always
        /// executes <em>after</em> the generic event for a successful randomisation.
        /// </summary>
        public event EventHandler<EntityEventArgs> HasProgressed;

        /// <summary>
        /// Invoked when the successful randomisation of a progression item caused the maximum reachable depth to
        /// increase. Always executes <em>after</em> the event for a progression item.
        /// </summary>
        public event EventHandler<EntityEventArgs> DepthIncreased;

        private void Awake()
        {
            _coreLogic = GetComponent<CoreLogic>();
            _config = _coreLogic._Config;
            _log = _coreLogic._Log;
            
            SetupPriorityEntities();
            SetupProgressionEntities();
            SetupVehicleDepths();
        }

        /// <summary>
        /// Add entities as priority electives, meaning that at least one of the entities is guaranteed to be
        /// randomised by the given depth. Intended to be used during the OnSetupPriority event.
        /// </summary>
        public void AddElectiveEntities(int depth, IEnumerable<TechType[]> additions)
        {
            if (!_electiveEntities.TryGetValue(depth, out List<TechType[]> techTypes))
                techTypes = new List<TechType[]>();
            techTypes.AddRange(additions);
            _electiveEntities[depth] = techTypes;
        }

        /// <summary>
        /// Add entities as priority essentials, meaning that they are guaranteed to be randomised by the given depth.
        /// Intended to be used during the OnSetupPriority event.
        /// </summary>
        public void AddEssentialEntities(int depth, IEnumerable<TechType> additions)
        {
            if (!_essentialEntities.TryGetValue(depth, out List<TechType> techTypes))
                techTypes = new List<TechType>();
            techTypes.AddRange(additions);
            _essentialEntities[depth] = techTypes;
        }

        /// <summary>
        /// This function calculates the maximum reachable depth based on what vehicles the player has attained,
        /// as well as how much further they can go "on foot"
        /// </summary>
        /// <param name="progressionItems">A list of all currently reachable items relevant for progression.</param>
        /// <param name="depthTime">The minimum time that it must be possible to spend at the reachable depth before
        /// resurfacing.</param>
        /// <returns>The reachable depth.</returns>
        private int CalculateReachableDepth(HashSet<TechType> progressionItems, int depthTime = 15)
        {
            const double swimmingSpeed = 4.7; // Always assume that the player is holding a tool.
            const double seaglideSpeed = 11.0;
            bool seaglide = progressionItems.Contains(TechType.Seaglide);
            double finSpeed = 0.0;
            Dictionary<TechType, double[]> tanks = new Dictionary<TechType, double[]>
            {
                { TechType.Tank, new[] { 75, 0.4 } }, // Tank type, oxygen, weight factor.
                { TechType.DoubleTank, new[] { 135, 0.47 } },
                { TechType.HighCapacityTank, new[] { 225, 0.6 } },
                { TechType.PlasteelTank, new[] { 135, 0.1 } }
            };

            _log.Debug("===== Recalculating reachable depth =====");

            if (progressionItems.Contains(TechType.Fins))
                finSpeed = 1.41;
            if (progressionItems.Contains(TechType.UltraGlideFins))
                finSpeed = 1.88;

            // How deep can the player go without any tanks?
            double soloDepthRaw = (45 - depthTime) * (seaglide ? seaglideSpeed : swimmingSpeed + finSpeed) / 2;

            // How deep can they go with tanks?
            foreach (var kv in tanks)
            {
                if (progressionItems.Contains(kv.Key))
                {
                    // Value[0] is the oxygen granted by the tank, Value[1] its weight factor.
                    double depth = (kv.Value[0] - depthTime)
                        * (seaglide ? seaglideSpeed : swimmingSpeed + finSpeed - kv.Value[1]) / 2;
                    soloDepthRaw = Math.Max(soloDepthRaw, depth);
                }
            }

            // How they deep can they go with a vehicle?
            int vehicleDepth = CalculateVehicleDepth();
            // Given everything above, calculate the total.
            int totalDepth;
            // With a rebreather, no funky extra oxygen calculations are necessary.
            if (progressionItems.Contains(TechType.Rebreather))
                totalDepth = vehicleDepth + Math.Min((int)soloDepthRaw, _config.iMaxDepthWithoutVehicle);
            else
                totalDepth = vehicleDepth + Math.Min(CalculateSoloDepth(vehicleDepth, (int)soloDepthRaw),
                    _config.iMaxDepthWithoutVehicle);

            _log.Debug("===== New reachable depth: " + totalDepth + " =====");

            return totalDepth;
        }

        /// <summary>
        /// Calculate the depth that can be comfortably reached on foot.
        /// </summary>
        /// <param name="vehicleDepth">The depth reachable by vehicle.</param>
        /// <param name="soloDepthRaw">The raw depth reachable on foot given no depth restrictions.</param>
        /// <returns>The depth that can be covered on foot in addition to the depth reachable by vehicle.</returns>
        private int CalculateSoloDepth(int vehicleDepth, int soloDepthRaw)
        {
            // Calculate how much of the 0-100m and 100-200m range is already covered by vehicles.
            int[] vehicleDepths = { Mathf.Clamp(vehicleDepth, 0, 100), Mathf.Clamp(vehicleDepth - 100, 0, 100) };
            int[] soloDepths =
            {
                Mathf.Clamp(soloDepthRaw, 0, 100 - vehicleDepths[0]),
                Mathf.Clamp(soloDepthRaw + vehicleDepths[0] - 100, 0, 100 - vehicleDepths[1]),
                Mathf.Clamp(soloDepthRaw + vehicleDepths[1] - 200, 0, 10000)
            };

            // Below 100 meters, air is consumed three times as fast.
            // Below 200 meters, it is consumed five times as fast.
            return soloDepths[0] + (soloDepths[1] / 3) + (soloDepths[2] / 5);
        }

        /// <summary>
        /// Calculate the deepest depth that can be reached with the currently unlocked vehicles.
        /// </summary>
        private int CalculateVehicleDepth()
        {
            int vehicleDepth = 0;
            foreach (var kvpair in _vehicleDepths)
            {
                // Only consider combinations where each part is already accessible to the player.
                if (kvpair.Key.All(t => _coreLogic.HasRandomised(t)))
                    vehicleDepth = Math.Max(vehicleDepth, kvpair.Value);
            }

            return vehicleDepth;
        }

        /// <summary>
        /// Yield all priority TechTypes up to the given depth, but only if they have not been randomised yet.
        /// </summary>
        private IEnumerable<TechType> GetUnusedPriorityEntities(int depth)
        {
            // Grab all entries of depth equal to or less than the currently accessible one.
            foreach (var kvpair in _essentialEntities.Where(kv => kv.Key <= depth))
            {
                foreach (TechType techType in kvpair.Value)
                {
                    if (!_coreLogic.HasRandomised(techType))
                        yield return techType;
                }
            }

            // Do the same for electives.
            foreach (var kvpair in _electiveEntities.Where(kv => kv.Key <= depth))
            {
                foreach (TechType[] techTypes in kvpair.Value)
                {
                    // Only ever prioritise one of the electives. If one is already in logic, skip all others.
                    if (techTypes.Length > 0 && techTypes.All(t => !_coreLogic.HasRandomised(t)))
                    {
                        TechType choice = _coreLogic.Random.Choice(techTypes);
                        yield return choice;
                    }
                }
            }
        }

        /// <summary>
        /// Check whether the given entity is part of any essential or elective items in any node up to the given depth.
        /// </summary>
        /// <param name="entity">The entity to check.</param>
        /// <param name="depth">Consider entities up to this depth.</param>
        /// <returns>True if the entity is part of essential or elective items, false otherwise.</returns>
        public bool IsPriorityEntity(LogicEntity entity, int depth)
        {
            if (_essentialEntities
                .Where(kv => kv.Key <= depth)
                .Any(kv => kv.Value.Contains(entity.TechType)))
                return true;
            if (_electiveEntities
                .Where(kv => kv.Key <= depth)
                .Any(kv => kv.Value.Any(arr => arr.Contains(entity.TechType))))
                return true;

            return false;
        }

        public bool IsProgressionEntity(TechType techType) => _progressionEntities.Contains(techType);
        public bool IsProgressionEntity(LogicEntity entity) => _progressionEntities.Contains(entity.TechType);

        /// <summary>
        /// Define which entities should be considered a priority at what depths. This ensures that those entities
        /// are always made available by that point at the very latest.
        /// </summary>
        private void SetupPriorityEntities()
        {
            _essentialEntities = new Dictionary<int, List<TechType>>();
            _electiveEntities = new Dictionary<int, List<TechType[]>>();
        }
        
        /// <summary>
        /// Define the entities which progress the game state, i.e. "unlock" other entities. Mostly characterised
        /// by things that help you dive deeper.
        /// </summary>
        private void SetupProgressionEntities()
        {
            _progressionEntities = new HashSet<TechType>
            {
                TechType.Fins,
                TechType.UltraGlideFins,
                TechType.Tank,
                TechType.DoubleTank,
                TechType.HighCapacityTank,
                TechType.PlasteelTank,
                TechType.Rebreather,

                TechType.Seaglide,
                TechType.Seamoth,
                TechType.Exosuit,
                TechType.Cyclops,

                TechType.VehicleHullModule1,
                TechType.VehicleHullModule2,
                TechType.VehicleHullModule3,
                TechType.ExoHullModule1,
                TechType.ExoHullModule2,
                TechType.CyclopsHullModule1,
                TechType.CyclopsHullModule2,
                TechType.CyclopsHullModule3
            };
            _unlockedProgressionEntities = new HashSet<TechType>();
        }

        /// <summary>
        /// Define which vehicle and upgrade combinations allow you to go how deep.
        /// </summary>
        private void SetupVehicleDepths()
        {
            _vehicleDepths = new Dictionary<TechType[], int>
            {
                { new[] { TechType.Seamoth }, 200 },
                { new[] { TechType.Seamoth, TechType.VehicleHullModule1 }, 300 },
                { new[] { TechType.Seamoth, TechType.VehicleHullModule2 }, 500 },
                { new[] { TechType.Seamoth, TechType.VehicleHullModule3 }, 900 },
                { new[] { TechType.Exosuit }, 900 },
                { new[] { TechType.Exosuit, TechType.ExoHullModule1 }, 1300 },
                { new[] { TechType.Exosuit, TechType.ExoHullModule2 }, 1700 },
                { new[] { TechType.Cyclops }, 500 },
                { new[] { TechType.Cyclops, TechType.CyclopsHullModule1 }, 900 },
                { new[] { TechType.Cyclops, TechType.CyclopsHullModule2 }, 1300 },
                { new[] { TechType.Cyclops, TechType.CyclopsHullModule3 }, 1700 },
            };
        }

        /// <summary>
        /// Check whether the newly randomised entity was a progression item and invoke related events if so.
        /// </summary>
        /// <param name="entity">The entity randomised on this cycle of the main loop.</param>
        internal void TriggerProgressionEvents(LogicEntity entity)
        {
            // If the newly randomised entity wasn't important for progression, skip.
            if (!_progressionEntities.Contains(entity.TechType))
                return;

            _unlockedProgressionEntities.Add(entity.TechType);
            _log.Debug($"[PM] Unlocked new progression item {entity}");
            HasProgressed?.Invoke(this, new EntityEventArgs(entity));

            // A new progression item also necessitates new depth calculations.
            int newDepth = CalculateReachableDepth(_unlockedProgressionEntities, _config.iDepthSearchTime);
            if (newDepth > ReachableDepth)
            {
                ReachableDepth = newDepth;
                UpdatePriorityEntities(ReachableDepth);
                DepthIncreased?.Invoke(this, new EntityEventArgs(entity));
            }
        }

        /// <summary>
        /// Trigger events to let other modules add their own priority and progression items.
        /// </summary>
        internal void TriggerSetupEvents()
        {
            // Let other modules add their own priority items.
            SetupPriorityEventArgs priorityArgs = new SetupPriorityEventArgs(_essentialEntities, _electiveEntities);
            SetupPriority?.Invoke(this, priorityArgs);
            _essentialEntities = priorityArgs.EssentialEntities;
            _electiveEntities = priorityArgs.ElectiveEntities;
            
            // Let other modules add their own progression items.
            SetupProgressionEventArgs progressionArgs = new SetupProgressionEventArgs(_progressionEntities);
            SetupProgression?.Invoke(this, progressionArgs);
            _progressionEntities = progressionArgs.ProgressionEntities;
        }

        /// <summary>
        /// Add all priority entities which have not yet been randomised to the priority list.
        /// </summary>
        /// <param name="depth">The maximum depth to consider.</param>
        private void UpdatePriorityEntities(int depth)
        {
            List<LogicEntity> additions = new List<LogicEntity>();
            foreach (TechType techType in GetUnusedPriorityEntities(depth))
            {
                LogicEntity entity = _coreLogic.EntityHandler.GetEntity(techType);
                additions.Add(entity);
                _log.Debug($"[PM] Adding priority entity {entity} to priority queue for depth {depth}");
            }

            _coreLogic.AddPriorityEntities(additions);
        }
    }
}