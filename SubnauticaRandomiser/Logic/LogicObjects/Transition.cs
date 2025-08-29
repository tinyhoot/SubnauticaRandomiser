using System;

namespace SubnauticaRandomiser.Logic.LogicObjects
{
    internal class Transition
    {
        public Region Entry;
        public Region Exit;

        public bool IsUnlocked()
        {
            // What can it be?
            // Laser cutter, Propulsion cannon, Teleporter IonCrystal, PrecursorKeys
            // *Also* depth/distance from the nearest reachable Region
            // Enum for lock type?
            throw new NotImplementedException();
        }
    }
}