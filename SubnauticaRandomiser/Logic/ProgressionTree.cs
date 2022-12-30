using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using SubnauticaRandomiser.Logic.Recipes;
using SubnauticaRandomiser.Objects;
using SubnauticaRandomiser.Objects.Enums;

namespace SubnauticaRandomiser.Logic
{
    internal class ProgressionTree
    {
        internal Dictionary<EProgressionNode, ProgressionPath> _depthDifficulties;
        internal Dictionary<EProgressionNode, List<TechType>> _essentialItems;
        internal Dictionary<EProgressionNode, List<TechType[]>> _electiveItems;
        internal Dictionary<TechType, TechType> _upgradeChains;
        public Dictionary<TechType, int> BasicOutpostPieces;
        public Dictionary<TechType, bool> DepthProgressionItems;

        public ProgressionTree()
        {
            _depthDifficulties = new Dictionary<EProgressionNode, ProgressionPath>();
            _essentialItems = new Dictionary<EProgressionNode, List<TechType>>();
            _electiveItems = new Dictionary<EProgressionNode, List<TechType[]>>();
            _upgradeChains = new Dictionary<TechType, TechType>();
            BasicOutpostPieces = new Dictionary<TechType, int>();
            DepthProgressionItems = new Dictionary<TechType, bool>();
        }

        /// <summary>
        /// Set up a progression tree with all the vanilla roadblocks and checkpoints. It includes the major depth
        /// milestones and the aurora, as well as which vehicles let you reach them. Getting to places on foot is
        /// handled by the depth calculcation logic elsewhere.
        /// If mod support ever becomes a thing there will likely have to be more flexible solutions than this.
        /// </summary>
        public void SetupVanillaTree()
        {
            // Aurora. Radiation suit required, unless you're fast.
            var path = new ProgressionPath(EProgressionNode.Aurora);
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
            path.AddPath(new [] { TechType.Seamoth, TechType.VehicleHullModule1 });
            path.AddPath(new [] { TechType.Seamoth, TechType.VehicleHullModule2 });
            path.AddPath(new [] { TechType.Seamoth, TechType.VehicleHullModule3 });
            path.AddPath(TechType.Exosuit);
            path.AddPath(TechType.Cyclops);
            SetProgressionPath(EProgressionNode.Depth300m, path);

            // 500m. Reachable with Seamoth II, Prawn, or Cyclops.
            path = new ProgressionPath(EProgressionNode.Depth500m);
            path.AddPath(new [] { TechType.Seamoth, TechType.VehicleHullModule2 });
            path.AddPath(new [] { TechType.Seamoth, TechType.VehicleHullModule3 });
            path.AddPath(TechType.Exosuit);
            path.AddPath(TechType.Cyclops);
            SetProgressionPath(EProgressionNode.Depth500m, path);

            // 900m. Reachable with Seamoth III, Prawn, or Cyclops I.
            path = new ProgressionPath(EProgressionNode.Depth900m);
            path.AddPath(new [] {TechType.Seamoth, TechType.VehicleHullModule3 });
            path.AddPath(TechType.Exosuit);
            path.AddPath(new [] { TechType.Cyclops, TechType.CyclopsHullModule1 });
            path.AddPath(new [] { TechType.Cyclops, TechType.CyclopsHullModule2 });
            path.AddPath(new [] { TechType.Cyclops, TechType.CyclopsHullModule3 });
            SetProgressionPath(EProgressionNode.Depth900m, path);

            // 1300m. Reachable with Prawn I or Cyclops II.
            path = new ProgressionPath(EProgressionNode.Depth1300m);
            path.AddPath(new [] { TechType.Exosuit, TechType.ExoHullModule1 });
            path.AddPath(new [] { TechType.Exosuit, TechType.ExoHullModule2 });
            path.AddPath(new [] { TechType.Cyclops, TechType.CyclopsHullModule2 });
            path.AddPath(new [] { TechType.Cyclops, TechType.CyclopsHullModule3 });
            SetProgressionPath(EProgressionNode.Depth1300m, path);

            // 1700m. Only Prawn II and Cyclops III can reach here.
            path = new ProgressionPath(EProgressionNode.Depth1700m);
            path.AddPath(new [] { TechType.Exosuit, TechType.ExoHullModule2 });
            path.AddPath(new [] { TechType.Cyclops, TechType.CyclopsHullModule3 });
            SetProgressionPath(EProgressionNode.Depth1700m, path);

            // Putting every item or vehicle that can help the player achieve
            // lower depths in one dictionary.
            DepthProgressionItems.Add(TechType.Fins, true);
            DepthProgressionItems.Add(TechType.UltraGlideFins, true);
            DepthProgressionItems.Add(TechType.Tank, true);
            DepthProgressionItems.Add(TechType.DoubleTank, true);
            DepthProgressionItems.Add(TechType.HighCapacityTank, true);
            DepthProgressionItems.Add(TechType.PlasteelTank, true);
            DepthProgressionItems.Add(TechType.Rebreather, true);
            // While not technically an item for depth progression, the laser cutter still unlocks a lot of things.
            DepthProgressionItems.Add(TechType.LaserCutter, true);

            DepthProgressionItems.Add(TechType.Seaglide, true);
            DepthProgressionItems.Add(TechType.Seamoth, true);
            DepthProgressionItems.Add(TechType.Exosuit, true);
            DepthProgressionItems.Add(TechType.Cyclops, true);

            DepthProgressionItems.Add(TechType.VehicleHullModule1, true);
            DepthProgressionItems.Add(TechType.VehicleHullModule2, true);
            DepthProgressionItems.Add(TechType.VehicleHullModule3, true);
            DepthProgressionItems.Add(TechType.ExoHullModule1, true);
            DepthProgressionItems.Add(TechType.ExoHullModule2, true);
            DepthProgressionItems.Add(TechType.CyclopsHullModule1, true);
            DepthProgressionItems.Add(TechType.CyclopsHullModule2, true);
            DepthProgressionItems.Add(TechType.CyclopsHullModule3, true);
        }

        /// <summary>
        /// Set up everything needed for randomising fragments.
        /// </summary>
        public void SetupFragments()
        {
            if (_depthDifficulties.Count == 0)
                SetupVanillaTree();
            
            // Ensure certain fragments are available by the given depth.
            AddEssentialItem(EProgressionNode.Depth0m, TechType.SeaglideFragment);
            
            AddEssentialItem(EProgressionNode.Depth100m, TechType.LaserCutterFragment);
            
            AddEssentialItem(EProgressionNode.Depth200m, TechType.BaseBioReactorFragment);
        }

        /// <summary>
        /// Set up everything needed for randomising recipes.
        /// </summary>
        /// <param name="useVanillaUpgradeChains">If true, set up vanilla upgrade chains like Knife to Heatblade.</param>
        public void SetupRecipes(bool useVanillaUpgradeChains)
        {
            if (_depthDifficulties.Count == 0)
                SetupVanillaTree();
            
            // Assemble a dictionary of what's considered basic outpost pieces
            // which together should not exceed the cost of config.iMaxBasicOutpostSize
            BasicOutpostPieces.Add(TechType.BaseCorridorI, 1);
            BasicOutpostPieces.Add(TechType.BaseHatch, 1);
            BasicOutpostPieces.Add(TechType.BaseMapRoom, 1);
            BasicOutpostPieces.Add(TechType.BaseWindow, 1);
            BasicOutpostPieces.Add(TechType.Beacon, 1);
            BasicOutpostPieces.Add(TechType.SolarPanel, 2);

            // The scanner and repair tool are absolutely required to get the
            // early game going, without the others it can get tedious.
            AddEssentialItem(EProgressionNode.Depth0m, TechType.Scanner);
            AddEssentialItem(EProgressionNode.Depth0m, TechType.Welder);
            AddEssentialItem(EProgressionNode.Depth0m, TechType.SmallStorage);
            AddEssentialItem(EProgressionNode.Depth0m, TechType.BaseHatch);
            AddEssentialItem(EProgressionNode.Depth0m, TechType.Fabricator);

            AddEssentialItem(EProgressionNode.Depth100m, TechType.Builder);
            AddEssentialItem(EProgressionNode.Depth100m, TechType.BaseRoom);
            AddEssentialItem(EProgressionNode.Depth100m, TechType.Seaglide);
            AddEssentialItem(EProgressionNode.Depth100m, TechType.Tank);

            AddEssentialItem(EProgressionNode.Depth300m, TechType.BaseWaterPark);

            // From among these, at least one has to be accessible by the provided
            // depth level. Ensures e.g. at least one power source by 200m.
            AddElectiveItems(EProgressionNode.Depth100m, new [] { TechType.Battery, TechType.BatteryCharger });

            AddElectiveItems(EProgressionNode.Depth200m, new [] { TechType.BaseBioReactor, TechType.SolarPanel });
            AddElectiveItems(EProgressionNode.Depth200m, new [] { TechType.PowerCell, TechType.PowerCellCharger, TechType.SeamothSolarCharge });
            AddElectiveItems(EProgressionNode.Depth200m, new [] { TechType.BaseBulkhead, TechType.BaseFoundation, TechType.BaseReinforcement });

            // Assemble a vanilla upgrade chain. These are the upgrades as the
            // base game intends you to progress through them.
            if (useVanillaUpgradeChains)
            {
                AddUpgradeChain(TechType.VehicleHullModule2, TechType.VehicleHullModule1);
                AddUpgradeChain(TechType.VehicleHullModule3, TechType.VehicleHullModule2);
                AddUpgradeChain(TechType.ExoHullModule2, TechType.ExoHullModule1);
                AddUpgradeChain(TechType.CyclopsHullModule2, TechType.CyclopsHullModule1);
                AddUpgradeChain(TechType.CyclopsHullModule3, TechType.CyclopsHullModule2);
                
                AddUpgradeChain(TechType.HeatBlade, TechType.Knife);
                AddUpgradeChain(TechType.RepulsionCannon, TechType.PropulsionCannon);
                AddUpgradeChain(TechType.SwimChargeFins, TechType.Fins);
                AddUpgradeChain(TechType.UltraGlideFins, TechType.Fins);
                AddUpgradeChain(TechType.DoubleTank, TechType.Tank);
                AddUpgradeChain(TechType.PlasteelTank, TechType.DoubleTank);
                AddUpgradeChain(TechType.HighCapacityTank, TechType.DoubleTank);
            }
        }
        
        /// <summary>
        /// Add early elements of an upgrade chain as prerequisites of the later pieces to ensure that they are always
        /// randomised in order, and no Knife can require a Heatblade as ingredient.
        /// </summary>
        /// <param name="materials">The list of all materials in the game.</param>
        public void ApplyUpgradeChainToPrerequisites(List<LogicEntity> materials)
        {
            if (materials == null || materials.Count == 0 || _upgradeChains == null || _upgradeChains.Count == 0)
                return;
            
            foreach (TechType upgrade in _upgradeChains.Keys)
            {
                TechType ingredient = _upgradeChains[upgrade];
                LogicEntity entity = materials.Find(x => x.TechType.Equals(upgrade));

                if (!entity.HasPrerequisites)
                    entity.Prerequisites = new List<TechType>();
                entity.Prerequisites.Add(ingredient);
            }
        }

        /// <summary>
        /// Get all possible ways to progress past the given progression node.
        /// </summary>
        /// <param name="node">The node to progress past, commonly a depth.</param>
        /// <returns>The paths to progress, or null if the node or path do not exist.</returns>
        [CanBeNull]
        public ProgressionPath GetProgressionPath(EProgressionNode node)
        {
            if (_depthDifficulties.TryGetValue(node, out ProgressionPath path))
                return path;

            return null;
        }
        
        /// <summary>
        /// Set a pathway of progression for the given progression node.
        /// </summary>
        /// <param name="node">The node to set a path for.</param>
        /// <param name="path">The path to set.</param>
        public void SetProgressionPath(EProgressionNode node, ProgressionPath path)
        {
            if (_depthDifficulties.ContainsKey(node))
            {
                _depthDifficulties.Remove(node);
            }
            _depthDifficulties.Add(node, path);
        }

        /// <summary>
        /// Add a pathway of progression to an existing ProgressionPath.
        /// </summary>
        /// <param name="node">The node to add a path for.</param>
        /// <param name="path">The TechType that allows for progression.</param>
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

        /// <summary>
        /// Add an entity that absolutely must be accessible by the time of the given progression node.
        /// </summary>
        /// <param name="node">The node representing the latest point at which the entity must be accessible.</param>
        /// <param name="type">The entity which must be accessible.</param>
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

        /// <summary>
        /// Add a range of entities at least one of which must be accessible by the time of the given progression node.
        /// </summary>
        /// <param name="node">The node representing the latest point at which at least one entity must be accessible.</param>
        /// <param name="types">The entities to choose from.</param>
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

        /// <summary>
        /// Define one entity as a direct upgrade of another.
        /// </summary>
        /// <param name="upgrade">The "Tier 2", or higher order entity.</param>
        /// <param name="ingredient">The "Tier 1", or lower order entity.</param>
        /// <returns>True if successful, false if the upgrade is already head of an existing chain.</returns>
        public bool AddUpgradeChain(TechType upgrade, TechType ingredient)
        {
            if (_upgradeChains.ContainsKey(upgrade))
                return false;
            
            _upgradeChains.Add(upgrade, ingredient);
            return true;
        }
        
        /// <summary>
        /// Get the ingredient required for a given upgrade, if any. E.g. Seamoth Depth MK2 will return MK1.
        /// </summary>
        /// <param name="upgrade">The "Tier 2" entity to investigate for ingredients.</param>
        /// <returns>The TechType of the required "Tier 1" ingredient, or TechType.None if no such requirement exists.
        /// </returns>
        public TechType GetBaseOfUpgrade(TechType upgrade)
        {
            if (_upgradeChains.TryGetValue(upgrade, out TechType type))  
                return type;

            return TechType.None;
        }

        /// <summary>
        /// If vanilla upgrade chains are enabled, return that which this recipe upgrades from.
        /// <example>Returns the basic Knife when given HeatBlade.</example>
        /// </summary>
        /// <param name="upgrade">The upgrade to check for a base.</param>
        /// <param name="materials">The list of all materials.</param>
        /// <returns>A LogicEntity if the given upgrade has a base it upgrades from, null otherwise.</returns>
        [CanBeNull]
        public LogicEntity GetBaseOfUpgrade(TechType upgrade, Materials materials)
        {
            TechType basicEntity = GetBaseOfUpgrade(upgrade);
            if (basicEntity.Equals(TechType.None))
                return null;

            return materials.Find(basicEntity);
        }

        /// <summary>
        /// Get the essential items for the given progression node.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <returns>The list of essential items, or null if it doesn't exist or the node is invalid.</returns>
        [CanBeNull]
        public List<TechType> GetEssentialNodeItems(EProgressionNode node)
        {
            if (_essentialItems.TryGetValue(node, out List<TechType> items))
                return items;

            return null;
        }

        /// <summary>
        /// Get all essential items up to the given depth.
        /// </summary>
        /// <param name="depth">The maximum depth to look for.</param>
        /// <returns>The list of essential items, or null if it doesn't exist.</returns>
        [NotNull]
        public List<TechType> GetEssentialItems(int depth)
        {
            var essentials = new List<TechType>();
            
            foreach (EProgressionNode node in EProgressionNodeExtensions.AllDepthNodes)
            {
                if ((int)node > depth)
                    break;

                if (_essentialItems.TryGetValue(node, out var list))
                    essentials.AddRange(list);
            }

            return essentials;
        }

        /// <summary>
        /// Get the list of lists of elective items for the given progression node.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <returns>The list of elective items, or null if it doesn't exist or the node is invalid.</returns>
        [CanBeNull]
        public List<TechType[]> GetElectiveNodeItems(EProgressionNode node)
        {
            if (_electiveItems.TryGetValue(node, out List<TechType[]> items))
                return items;

            return null;
        }

        /// <summary>
        /// Get the list of lists of elective items up to the given depth.
        /// </summary>
        /// <param name="depth">The maximum depth to look for.</param>
        /// <returns>The list of elective items, or null if it doesn't exist.</returns>
        [NotNull]
        public List<TechType[]> GetElectiveItems(int depth)
        {
            var electives = new List<TechType[]>();
            
            foreach (EProgressionNode node in EProgressionNodeExtensions.AllDepthNodes)
            {
                if ((int)node > depth)
                    break;

                if (_electiveItems.TryGetValue(node, out var list))
                    electives.AddRange(list);
            }

            return electives;
        }

        /// <summary>
        /// Check whether the given entity is part of any essential or elective items in any node up to the given depth.
        /// </summary>
        /// <param name="entity">The entity to check.</param>
        /// <param name="depth">Consider entities up to this depth.</param>
        /// <returns>True if the entity is part of essential or elective items, false otherwise.</returns>
        public bool IsPriorityEntity(LogicEntity entity, int depth)
        {
            if (_essentialItems
                .Where(kv => !kv.Key.isDeeperThan(depth))
                .Any(kv => kv.Value.Contains(entity.TechType)))
                return true;
            if (_electiveItems
                .Where(kv => !kv.Key.isDeeperThan(depth))
                .Any(kv => kv.Value.Any(arr => arr.Contains(entity.TechType))))
                return true;

            return false;
        }

        /// <summary>
        /// Check whether the given entity is a depth progression item.
        /// </summary>
        /// <param name="entity">The entity to check.</param>
        /// <returns>True if the entity is a depth progression item, false otherwise.</returns>
        public bool IsProgressionItem(LogicEntity entity)
        {
            return DepthProgressionItems.ContainsKey(entity.TechType);
        }
    }
}
