using System.Collections.Generic;
using JetBrains.Annotations;
using SubnauticaRandomiser.RandomiserObjects;
using UnityEngine;

namespace SubnauticaRandomiser.Logic
{
    internal class DataboxLogic
    {
        private readonly CoreLogic _logic;

        private List<Databox> _databoxes => _logic._databoxes;
        private EntitySerializer _masterDict => _logic._masterDict;
        private System.Random _random => _logic._random;

        internal DataboxLogic(CoreLogic logic)
        {
            _logic = logic;
        }

        /// <summary>
        /// Randomise (shuffle) the blueprints found inside databoxes.
        /// </summary>
        /// <returns>The list of newly randomised databoxes.</returns>
        [NotNull]
        internal List<Databox> RandomiseDataboxes()
        {
            _masterDict.Databoxes = new Dictionary<RandomiserVector, TechType>();
            List<Databox> randomDataboxes = new List<Databox>();
            List<Vector3> toBeRandomised = new List<Vector3>();

            foreach (Databox dbox in _databoxes)
            {
                toBeRandomised.Add(dbox.Coordinates);
            }

            foreach (Databox originalBox in _databoxes)
            {
                int next = _random.Next(0, toBeRandomised.Count);
                Databox replacementBox = _databoxes.Find(x => x.Coordinates.Equals(toBeRandomised[next]));

                randomDataboxes.Add(new Databox(originalBox.TechType, toBeRandomised[next], replacementBox.Wreck, 
                    replacementBox.RequiresLaserCutter, replacementBox.RequiresPropulsionCannon));
                _masterDict.Databoxes.Add(new RandomiserVector(toBeRandomised[next]), originalBox.TechType);
                LogHandler.Debug("Databox " + toBeRandomised[next].ToString() + " with "
                                 + replacementBox.TechType.AsString() + " now contains " + originalBox.TechType.AsString());
                toBeRandomised.RemoveAt(next);
            }
            _masterDict.isDataboxRandomised = true;

            return randomDataboxes;
        }
    }
}