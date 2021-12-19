using System;
using System.Collections.Generic;

namespace SubnauticaRandomiser.RandomiserObjects
{
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
