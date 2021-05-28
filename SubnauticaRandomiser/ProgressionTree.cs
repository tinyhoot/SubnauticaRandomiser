using System;
using System.Collections.Generic;

namespace SubnauticaRandomiser
{
    public class ProgressionTree
    {
        private Dictionary<EProgressionNode, ProgressionPath> _depthDifficulties;
        public Dictionary<TechType, bool> depthProgressionItems;

        public ProgressionTree()
        {
            _depthDifficulties = new Dictionary<EProgressionNode, ProgressionPath>();
            depthProgressionItems = new Dictionary<TechType, bool>();
        }

        public void SetupVanillaTree()
        {
            // This is where the progression tree with all the vanilla roadblocks
            // and checkpoints gets set up. It includes the major depth milestones
            // and the aurora, as well as which vehicles let you reach them.
            // Getting to places on foot is handled by the depth calculcation
            // logic in ProgressionManager.
            // If mod support ever becomes a thing there will likely have to be
            // more flexible solutions than this.

            ProgressionPath path;
            // Aurora. Radiation suit required, unless you're fast.
            path = new ProgressionPath(EProgressionNode.Aurora);
            path.AddPath(TechType.RadiationSuit);
            SetProgressionPath(EProgressionNode.Aurora, path);

            // 100m.
            path = new ProgressionPath(EProgressionNode.Depth100m);
            path.AddPath(TechType.Seamoth);
            path.AddPath(TechType.Exosuit);
            path.AddPath(TechType.Cyclops);
            SetProgressionPath(EProgressionNode.Depth100m, path);

            // 200m.
            path = new ProgressionPath(EProgressionNode.Depth200m);
            path.AddPath(TechType.Seamoth);
            path.AddPath(TechType.Exosuit);
            path.AddPath(TechType.Cyclops);
            SetProgressionPath(EProgressionNode.Depth200m, path);

            // 300m. Requires Seamoth I.
            path = new ProgressionPath(EProgressionNode.Depth300m);
            path.AddPath(new TechType[] { TechType.Seamoth, TechType.VehicleHullModule1 });
            path.AddPath(TechType.Exosuit);
            path.AddPath(TechType.Cyclops);
            SetProgressionPath(EProgressionNode.Depth300m, path);

            // 500m. Reachable with Seamoth II, Prawn, or Cyclops.
            path = new ProgressionPath(EProgressionNode.Depth500m);
            path.AddPath(new TechType[] { TechType.Seamoth, TechType.VehicleHullModule2 });
            path.AddPath(TechType.Exosuit);
            path.AddPath(TechType.Cyclops);
            SetProgressionPath(EProgressionNode.Depth500m, path);

            // 900m. Reachable with Seamoth III, Prawn, or Cyclops I.
            path = new ProgressionPath(EProgressionNode.Depth900m);
            path.AddPath(new TechType[] {TechType.Seamoth, TechType.VehicleHullModule3 });
            path.AddPath(TechType.Exosuit);
            path.AddPath(new TechType[] { TechType.Cyclops, TechType.CyclopsHullModule1 });
            SetProgressionPath(EProgressionNode.Depth900m, path);

            // 1300m. Reachable with Prawn I or Cyclops II.
            path = new ProgressionPath(EProgressionNode.Depth1300m);
            path.AddPath(new TechType[] { TechType.Exosuit, TechType.ExoHullModule1 });
            path.AddPath(new TechType[] { TechType.Cyclops, TechType.CyclopsHullModule2 });
            SetProgressionPath(EProgressionNode.Depth1300m, path);

            // 1700m. Only Prawn II and Cyclops III can reach here.
            path = new ProgressionPath(EProgressionNode.Depth1700m);
            path.AddPath(new TechType[] { TechType.Exosuit, TechType.ExoHullModule2 });
            path.AddPath(new TechType[] { TechType.Cyclops, TechType.CyclopsHullModule3 });
            SetProgressionPath(EProgressionNode.Depth1700m, path);

            // Putting every item or vehicle that can help the player achieve
            // lower depths in one dictionary.
            depthProgressionItems.Add(TechType.Fins, true);
            depthProgressionItems.Add(TechType.UltraGlideFins, true);
            depthProgressionItems.Add(TechType.Tank, true);
            depthProgressionItems.Add(TechType.DoubleTank, true);
            depthProgressionItems.Add(TechType.HighCapacityTank, true);
            depthProgressionItems.Add(TechType.PlasteelTank, true);
            depthProgressionItems.Add(TechType.Rebreather, true);

            depthProgressionItems.Add(TechType.Seaglide, true);
            depthProgressionItems.Add(TechType.Seamoth, true);
            depthProgressionItems.Add(TechType.Exosuit, true);
            depthProgressionItems.Add(TechType.Cyclops, true);
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
