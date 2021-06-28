using System;
using System.Collections.Generic;

namespace SubnauticaRandomiser
{
    public class ProgressionTree
    {
        private Dictionary<EProgressionNode, ProgressionPath> _depthDifficulties;
        public Dictionary<TechType, bool> depthProgressionItems;
        private Dictionary<EProgressionNode, List<TechType>> _essentialItems;
        private Dictionary<EProgressionNode, List<TechType[]>> _electiveItems;

        public ProgressionTree()
        {
            _depthDifficulties = new Dictionary<EProgressionNode, ProgressionPath>();
            depthProgressionItems = new Dictionary<TechType, bool>();
            _essentialItems = new Dictionary<EProgressionNode, List<TechType>>();
            _electiveItems = new Dictionary<EProgressionNode, List<TechType[]>>();
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
            path.AddPath(new TechType[] { TechType.Seamoth, TechType.VehicleHullModule2 });
            path.AddPath(new TechType[] { TechType.Seamoth, TechType.VehicleHullModule3 });
            path.AddPath(TechType.Exosuit);
            path.AddPath(TechType.Cyclops);
            SetProgressionPath(EProgressionNode.Depth300m, path);

            // 500m. Reachable with Seamoth II, Prawn, or Cyclops.
            path = new ProgressionPath(EProgressionNode.Depth500m);
            path.AddPath(new TechType[] { TechType.Seamoth, TechType.VehicleHullModule2 });
            path.AddPath(new TechType[] { TechType.Seamoth, TechType.VehicleHullModule3 });
            path.AddPath(TechType.Exosuit);
            path.AddPath(TechType.Cyclops);
            SetProgressionPath(EProgressionNode.Depth500m, path);

            // 900m. Reachable with Seamoth III, Prawn, or Cyclops I.
            path = new ProgressionPath(EProgressionNode.Depth900m);
            path.AddPath(new TechType[] {TechType.Seamoth, TechType.VehicleHullModule3 });
            path.AddPath(TechType.Exosuit);
            path.AddPath(new TechType[] { TechType.Cyclops, TechType.CyclopsHullModule1 });
            path.AddPath(new TechType[] { TechType.Cyclops, TechType.CyclopsHullModule2 });
            path.AddPath(new TechType[] { TechType.Cyclops, TechType.CyclopsHullModule3 });
            SetProgressionPath(EProgressionNode.Depth900m, path);

            // 1300m. Reachable with Prawn I or Cyclops II.
            path = new ProgressionPath(EProgressionNode.Depth1300m);
            path.AddPath(new TechType[] { TechType.Exosuit, TechType.ExoHullModule1 });
            path.AddPath(new TechType[] { TechType.Exosuit, TechType.ExoHullModule2 });
            path.AddPath(new TechType[] { TechType.Cyclops, TechType.CyclopsHullModule2 });
            path.AddPath(new TechType[] { TechType.Cyclops, TechType.CyclopsHullModule3 });
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

            depthProgressionItems.Add(TechType.VehicleHullModule1, true);
            depthProgressionItems.Add(TechType.VehicleHullModule2, true);
            depthProgressionItems.Add(TechType.VehicleHullModule3, true);
            depthProgressionItems.Add(TechType.ExoHullModule1, true);
            depthProgressionItems.Add(TechType.ExoHullModule2, true);
            depthProgressionItems.Add(TechType.CyclopsHullModule1, true);
            depthProgressionItems.Add(TechType.CyclopsHullModule2, true);
            depthProgressionItems.Add(TechType.CyclopsHullModule3, true);

            // The scanner and repair tool are absolutely required to get the
            // early game going, without the others it can get tedious.
            AddEssentialItem(EProgressionNode.Depth0m, TechType.Scanner);
            AddEssentialItem(EProgressionNode.Depth0m, TechType.Welder);
            AddEssentialItem(EProgressionNode.Depth0m, TechType.SmallStorage);
            AddEssentialItem(EProgressionNode.Depth0m, TechType.BaseHatch);
            AddEssentialItem(EProgressionNode.Depth0m, TechType.Fabricator);

            AddEssentialItem(EProgressionNode.Depth100m, TechType.BaseRoom);
            AddEssentialItem(EProgressionNode.Depth100m, TechType.Tank);

            AddEssentialItem(EProgressionNode.Depth300m, TechType.BaseWaterPark);

            // From among these, at least one has to be accessible by the provided
            // depth level. Ensures e.g. at least one power source by 200m.
            AddElectiveItems(EProgressionNode.Depth100m, new TechType[] { TechType.Battery, TechType.BatteryCharger });

            AddElectiveItems(EProgressionNode.Depth200m, new TechType[] { TechType.BaseBioReactor, TechType.SolarPanel });
            AddElectiveItems(EProgressionNode.Depth200m, new TechType[] { TechType.PowerCell, TechType.PowerCellCharger, TechType.SeamothSolarCharge });
            AddElectiveItems(EProgressionNode.Depth200m, new TechType[] { TechType.BaseBulkhead, TechType.BaseFoundation, TechType.BaseReinforcement });
        }

        public ProgressionPath GetProgressionPath(EProgressionNode node)
        {
            if (_depthDifficulties.TryGetValue(node, out ProgressionPath path))
                return path;

            return null;
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

        public void AddEssentialItem(EProgressionNode node, TechType type)
        {
            if (_essentialItems.TryGetValue(node, out List<TechType> items))
            {
                items.Add(type);
            }
            else
            {
                List<TechType> list = new List<TechType> { type };
                _essentialItems.Add(node, list);
            }
        }

        public void AddElectiveItems(EProgressionNode node, TechType[] types)
        {
            if (_electiveItems.TryGetValue(node, out List<TechType[]> existingItems))
            {
                existingItems.Add(types);
            }
            else
            {
                List<TechType[]> list = new List<TechType[]> { types };
                _electiveItems.Add(node, list);
            }
        }

        public List<TechType> GetEssentialItems(EProgressionNode node)
        {
            if (_essentialItems.TryGetValue(node, out List<TechType> items))
                return items;

            return null;
        }

        public List<TechType> GetEssentialItems(int depth)
        {
            foreach (EProgressionNode node in _essentialItems.Keys)
            {
                if ((int)node < depth && _essentialItems[node].Count > 0)
                    return _essentialItems[node];
            }
            return null;
        }

        public List<TechType[]> GetElectiveItems(EProgressionNode node)
        {
            if (_electiveItems.TryGetValue(node, out List<TechType[]> items))
                return items;

            return null;
        }

        public List<TechType[]> GetElectiveItems(int depth)
        {
            foreach (EProgressionNode node in _electiveItems.Keys)
            {
                if ((int)node < depth && _electiveItems[node].Count > 0)
                    return _electiveItems[node];
            }
            return null;
        }
    }
}
