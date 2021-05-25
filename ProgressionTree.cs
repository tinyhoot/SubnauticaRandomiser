using System;
using System.Collections.Generic;

namespace SubnauticaRandomiser
{
    public class ProgressionTree
    {
        private Dictionary<EProgressionNode, ProgressionPath> _depthDifficulties;

        public ProgressionTree()
        {
            _depthDifficulties = new Dictionary<EProgressionNode, ProgressionPath>();
        }

        public void SetupVanillaTree()
        {
            // This is where the progression tree with all the vanilla roadblocks
            // and checkpoints gets set up. It includes the major depth milestones
            // and the aurora, as well as which items, vehicles and technologies
            // let you reach them.
            // If mod support ever becomes a thing there will likely have to be
            // more flexible solutions than this.

            ProgressionPath path;
            // Aurora. Radiation suit required, unless you're fast.
            path = new ProgressionPath(EProgressionNode.Aurora);
            path.AddPath(TechType.RadiationSuit);
            SetProgressionPath(EProgressionNode.Aurora, path);

            // 100m. Doable with fins, O2 tank, or the seaglide.
            path = new ProgressionPath(EProgressionNode.Depth100m);
            path.AddPath(TechType.Fins);
            path.AddPath(TechType.Tank);
            path.AddPath(TechType.Seaglide);
            SetProgressionPath(EProgressionNode.Depth100m, path);

            // 200m. Although doable with doubletank and seaglide, we'll require
            // the seamoth for now.
            path = new ProgressionPath(EProgressionNode.Depth200m);
            path.AddPath(TechType.Seamoth);
            SetProgressionPath(EProgressionNode.Depth200m, path);

            // 300m. Requires Seamoth upgrade 1. Although you can always exit
            // your vehicle and dive deeper (meaning all you really need is the
            // seamoth and the seaglide at 200m), we'll use separate logic
            // somewhere else to handle this.
            path = new ProgressionPath(EProgressionNode.Depth300m);
            path.AddPath(TechType.VehicleHullModule1);
            SetProgressionPath(EProgressionNode.Depth300m, path);

            // 500m. Reachable with Seamoth II, Prawn, or Cyclops.
            path = new ProgressionPath(EProgressionNode.Depth500m);
            path.AddPath(TechType.VehicleHullModule2);
            path.AddPath(TechType.Exosuit);
            path.AddPath(TechType.Cyclops);
            SetProgressionPath(EProgressionNode.Depth500m, path);

            // 900m. Reachable with Seamoth III, Prawn, or Cyclops I.
            path = new ProgressionPath(EProgressionNode.Depth900m);
            path.AddPath(TechType.VehicleHullModule3);
            path.AddPath(TechType.Exosuit);
            path.AddPath(TechType.CyclopsHullModule1);
            SetProgressionPath(EProgressionNode.Depth900m, path);

            // 1300m. Reachable with Prawn I or Cyclops II.
            path = new ProgressionPath(EProgressionNode.Depth1300m);
            path.AddPath(TechType.ExoHullModule1);
            path.AddPath(TechType.CyclopsHullModule2);
            SetProgressionPath(EProgressionNode.Depth1300m, path);

            // 1700m. Only Prawn II and Cyclops III can reach here.
            path = new ProgressionPath(EProgressionNode.Depth1700m);
            path.AddPath(TechType.ExoHullModule2);
            path.AddPath(TechType.CyclopsHullModule3);
            SetProgressionPath(EProgressionNode.Depth1700m, path);
        }

        public ProgressionPath GetProgressionPath(EProgressionNode node)
        {
            if (_depthDifficulties.TryGetValue(node, out ProgressionPath path))
            {
                return path;
            }
            else
            {
                return null;
            }
        }
        
        public void SetProgressionPath(EProgressionNode node, ProgressionPath path)
        {
            if (_depthDifficulties.ContainsKey(node))
            {
                _depthDifficulties.Remove(node);
            }
            _depthDifficulties.Add(node, path);
        }

        public void AddToProgressionPath(EProgressionNode node, TechType path)
        {
            if (_depthDifficulties.TryGetValue(node, out ProgressionPath pathways))
            {
                pathways.AddPath(path);
            }
            else
            {
                ProgressionPath p = new ProgressionPath(node);
                p.AddPath(path);
                _depthDifficulties.Add(node, p);
            }
        }
    }
}
