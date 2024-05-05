using System.Collections.Generic;
using SubnauticaRandomiser.Objects;

namespace SubnauticaRandomiser.Serialization.Modules
{
    internal class DataboxSaveData : BaseModuleSaveData
    {
        public Dictionary<RandomiserVector, TechType> Databoxes = new Dictionary<RandomiserVector, TechType>();
    }
}