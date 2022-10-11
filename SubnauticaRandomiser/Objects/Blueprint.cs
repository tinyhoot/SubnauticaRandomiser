using System;
using System.Collections.Generic;
using System.IO;
using SubnauticaRandomiser.Logic;

namespace SubnauticaRandomiser.Objects
{
    /// <summary>
    /// A class representing the knowledge required for an entity to appear in the player's PDA.
    /// </summary>
    [Serializable]
    internal class Blueprint
    {
        public TechType TechType;
        public List<TechType> UnlockConditions;
        public List<TechType> Fragments;
        public int NumFragments;
        public bool NeedsDatabox;
        public int UnlockDepth;
        public bool WasUpdated;  // Was this one updated to account for changes in databox locations?

        public Blueprint(TechType techType, List<TechType> unlockConditions = null, TechType fragment = TechType.None,
            int numFragments = 3, bool databox = false, int unlockDepth = 0)
        {
            Fragments = new List<TechType>();

            TechType = techType;
            UnlockConditions = unlockConditions;
            Fragments.Add(fragment);
            NumFragments = numFragments;
            NeedsDatabox = databox;
            UnlockDepth = unlockDepth;
        }

        public Blueprint(TechType techType, List<TechType> unlockConditions = null, List<TechType> fragments = null, bool databox = false, int unlockDepth = 0)
        {
            TechType = techType;
            UnlockConditions = unlockConditions;
            Fragments = fragments;
            NeedsDatabox = databox;
            UnlockDepth = unlockDepth;
        }

        /// <summary>
        /// If databoxes were randomised, ensure the unlock requirements are updated to reflect the new positions.
        /// </summary>
        /// <param name="logic">The core logic.</param>
        /// <exception cref="ArgumentException">Raised if databoxes weren't randomised in the given Logic.</exception>
        /// <exception cref="InvalidDataException">Raised if the blueprint requires a databox, but no databoxes
        /// containing it were found.</exception>
        public void UpdateDataboxUnlocks(CoreLogic logic)
        {
            if (!NeedsDatabox || WasUpdated)
                return;
            if (!(logic?._databoxes?.Count > 0))
                throw new ArgumentException("Cannot update databox unlocks: Databox list is null or invalid.");

            int total = 0;
            int number = 0;
            int lasercutter = 0;
            int propulsioncannon = 0;

            foreach (Databox box in logic._databoxes.FindAll(x => x.TechType.Equals(TechType)))
            {
                total += (int)Math.Abs(box.Coordinates.y);
                number++;

                if (box.RequiresLaserCutter)
                    lasercutter++;
                if (box.RequiresPropulsionCannon)
                    propulsioncannon++;
            }

            if (number == 0)
                throw new InvalidDataException($"Entity {TechType.AsString()} requires a databox, but 0 were found!");

            logic._log.Debug($"[B] Found {number} databoxes for {TechType.AsString()}");

            UnlockDepth = total / number;
            UnlockConditions ??= new List<TechType>();

            // If more than half of all locations of this databox require a tool to access the box, add it to
            // the requirements for the recipe.
            if (lasercutter / number >= 0.5)
                UnlockConditions.Add(TechType.LaserCutter);

            if (propulsioncannon / number >= 0.5)
                UnlockConditions.Add(TechType.PropulsionCannon);

            WasUpdated = true; 
        }
    }
}
