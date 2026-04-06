using System.Collections.Generic;
using SubnauticaRandomiser.Logic.LogicObjects;
using UnityEngine;

namespace SubnauticaRandomiser.Serialization.Modules
{
    internal class DataboxSaveData : BaseModuleSaveData
    {
        public List<Databox> Databoxes = new List<Databox>();

        public void AddBox(LogicDatabox logicBox)
        {
            foreach (var node in logicBox.DataboxPositions)
            {
                Databoxes.Add(new Databox { TechType = logicBox.TechType, Position = node.Position });
            }
        }

        public struct Databox
        {
            public TechType TechType;
            public Vector3 Position;
        }
    }
}