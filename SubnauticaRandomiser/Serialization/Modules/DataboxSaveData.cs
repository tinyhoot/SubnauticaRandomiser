using System.Collections.Generic;
using SubnauticaRandomiser.Objects;

namespace SubnauticaRandomiser.Serialization.Modules
{
    internal class DataboxSaveData : BaseModuleSaveData
    {
        public List<Databox> Databoxes = new List<Databox>();
    }
}