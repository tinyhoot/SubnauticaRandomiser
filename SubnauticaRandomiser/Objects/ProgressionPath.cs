using System.Collections.Generic;
using SubnauticaRandomiser.Objects.Enums;

namespace SubnauticaRandomiser.Objects
{
    /// <summary>
    /// This class represents all the ways a progression roadblock (node) can be surpassed.
    /// </summary>
    public class ProgressionPath
    {
        public EProgressionNode Node;
        public List<TechType[]> Pathways;

        public ProgressionPath(EProgressionNode node)
        {
            Node = node;
            Pathways = new List<TechType[]>();
        }

        public void AddPath(TechType[] path)
        {
            Pathways.Add(path);
        }

        public void AddPath(TechType path)
        {
            Pathways.Add(new TechType[] { path });
        }
    }
}
