using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HootLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using SubnauticaRandomiser.Configuration;
using SubnauticaRandomiser.Logic.LogicObjects;
using SubnauticaRandomiser.Serialization;
using SubnauticaRandomiser.Serialization.Converters;
using UnityEngine;

namespace SubnauticaRandomiser.Logic
{
    /// <summary>
    /// Responsible for determining how far the player can travel given their current progress.
    /// </summary>
    internal class TravelDistanceManager
    {
        /// <summary>
        /// The maximum depth that can be covered by the player without vehicles.
        /// </summary>
        public float MaxDiverDepth { get; private set; }
        
        /// <summary>
        /// The maximum depth that can be covered by the player with the help of their unlocked vehicles.
        /// </summary>
        public float MaxVehicleDepth { get; private set; }

        private bool _hasRebreather;
        
        private const string TravelDataFile = "travelData.json";
        private TravelData _travelData;
        private Config _config;

        public TravelDistanceManager(Config config)
        {
            _config = config;
        }

        public IEnumerator LoadTravelDataFromDiskAsync(EntityManager entities)
        {
            var task = SerdeUtils.ReadFileContents(TravelDataFile);
            yield return new WaitUntil(() => task.IsCompleted);

            _travelData = JsonConvert.DeserializeObject<TravelData>(task.Result, new StringEnumConverter(),
                new StringEntityConverter(entities));
        }

        /// <summary>
        /// Check whether the player can reach a point at a specific depth with all their unlocked equipment.
        /// </summary>
        public bool CanReach(float targetDepth)
        {
            // If a vehicle can cover the distance there is no problem.
            float leftoverDepth = targetDepth - MaxVehicleDepth;
            if (leftoverDepth <= 0)
                return true;

            float diverDepth = MaxDiverDepth;
            // Assume the worst case and always apply the 5x oxygen consumption that happens below 200m.
            if (!_hasRebreather && targetDepth > 100f)
                diverDepth /= 5;

            diverDepth = Mathf.Min(diverDepth, _config.MaxDepthWithoutVehicle.Value);
            return leftoverDepth < diverDepth;
        }

        public void UpdateDepths(EntityManager manager)
        {
            _hasRebreather = manager.IsAccessible<LogicInventoryItem>(TechType.Rebreather);
            UpdateDiverDepth(manager);
            UpdateVehicleDepth();
        }

        private void UpdateDiverDepth(EntityManager manager)
        {
            // Find the fastest speed of the best accessible fins.
            float finSpeed = 0f;
            foreach (var (fin, speed) in _travelData.FinSpeeds)
            {
                if (manager.IsAccessible<LogicInventoryItem>(fin))
                    finSpeed = Mathf.Max(finSpeed, speed);
            }

            // Find the highest capacity of the best accessible tank.
            TravelData.TankData tankData = default;
            foreach (var data in _travelData.TankCapacities)
            {
                if (manager.IsAccessible<LogicInventoryItem>(data.TechType) && tankData.Capacity < data.Capacity)
                    tankData = data;
            }

            float diverSpeed = _travelData.BaseSpeed + finSpeed - tankData.WeightFactor;
            if (manager.IsAccessible<LogicInventoryItem>(TechType.Seaglide))
                diverSpeed = _travelData.SeaglideSpeed;

            // Deduct a portion of the available oxygen to keep it as budget for exploration time at depth.
            float oxygen = _travelData.BaseOxygen + tankData.Capacity - _config.DepthSearchTime.Value;
            MaxDiverDepth = (oxygen / 2) * diverSpeed;
        }

        private void UpdateVehicleDepth()
        {
            foreach (var data in _travelData.VehicleDepths)
            {
                // If all vehicle objects have a sphere assigned that means they're all in logic.
                if (data.Entities.All(v => v.Sphere >= 0))
                    MaxVehicleDepth = Mathf.Max(MaxVehicleDepth, data.Depth);
            }
        }
    }

    [Serializable]
    internal struct TravelData
    {
        public float BaseSpeed;
        public float BaseOxygen;
        public float SeaglideSpeed;
        public Dictionary<TechType, float> FinSpeeds;
        public List<TankData> TankCapacities;
        public List<VehicleData> VehicleDepths;

        internal struct TankData
        {
            public TechType TechType;
            public float Capacity;
            public float WeightFactor;
        }

        internal struct VehicleData
        {
            public int Depth;
            public List<LogicEntity> Entities;
        }
    }
}