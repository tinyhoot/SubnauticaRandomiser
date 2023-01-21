using System.Collections.Generic;

namespace SubnauticaRandomiser.Objects.Events
{
    internal class SetupPriorityEventArgs
    {
        public Dictionary<int, List<TechType>> EssentialEntities;
        public Dictionary<int, List<TechType[]>> ElectiveEntities;

        public SetupPriorityEventArgs()
        {
            EssentialEntities = new Dictionary<int, List<TechType>>();
            ElectiveEntities = new Dictionary<int, List<TechType[]>>();
        }

        public SetupPriorityEventArgs(Dictionary<int, List<TechType>> essentials,
            Dictionary<int, List<TechType[]>> electives)
        {
            EssentialEntities = essentials;
            ElectiveEntities = electives;
        }
    }
}