using System;
using UnityEngine;

namespace SubnauticaRandomiser.RandomiserObjects
{
    /// <summary>
    /// A databox containing a blueprint it unlocks.
    /// </summary>
    [Serializable]
    public class Databox
    {
        public TechType TechType;
        public Vector3 Coordinates;
        public EWreckage Wreck;
        public bool RequiresLaserCutter;
        public bool RequiresPropulsionCannon;

        public Databox(TechType techType, Vector3 coordinates, EWreckage wreck = EWreckage.None, bool laserCutter = false, bool propulsionCannon = false)
        {
            TechType = techType;
            Coordinates = coordinates;
            Wreck = wreck;
            RequiresLaserCutter = laserCutter;
            RequiresPropulsionCannon = propulsionCannon;
        }
    }
}
