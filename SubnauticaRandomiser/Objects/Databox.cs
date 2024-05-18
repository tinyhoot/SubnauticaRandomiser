using System;
using Newtonsoft.Json;
using SubnauticaRandomiser.Objects.Enums;
using UnityEngine;

namespace SubnauticaRandomiser.Objects
{
    /// <summary>
    /// A databox containing a blueprint it unlocks.
    /// </summary>
    [Serializable]
    [JsonObject(MemberSerialization.OptIn)]
    internal class Databox
    {
        [JsonProperty] public TechType TechType;
        [JsonProperty] public Vector3 Coordinates;
        public Wreckage Wreck;
        public bool RequiresLaserCutter;
        public bool RequiresPropulsionCannon;

        public Databox(TechType techType, Vector3 coordinates, Wreckage wreck = Wreckage.None, bool laserCutter = false, bool propulsionCannon = false)
        {
            TechType = techType;
            Coordinates = coordinates;
            Wreck = wreck;
            RequiresLaserCutter = laserCutter;
            RequiresPropulsionCannon = propulsionCannon;
        }

        public override string ToString()
        {
            return TechType.ToString();
        }
    }
}
