using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using HootLib;
using SubnauticaRandomiser.Interfaces;
using SubnauticaRandomiser.Logic.LogicObjects;
using SubnauticaRandomiser.Patches;
using SubnauticaRandomiser.Serialization;
using SubnauticaRandomiser.Serialization.Modules;
using LogicEntity = SubnauticaRandomiser.Logic.LogicObjects.LogicEntity;

namespace SubnauticaRandomiser.Logic.Modules
{
    /// <summary>
    /// Handles everything related to randomising databoxes.
    /// </summary>
    internal class DataboxModule : BaseLogicModule
    {
        public override Type HandledEntityType => typeof(LogicDatabox);
        public override string LogPrefix => "[Databox]";

        private RandoContext _ctx;
        private List<LogicDatabox> _databoxes;
        private List<LogicDatabox> _progressionBoxes;
        private List<LogicDatabox> _recheckLater = new List<LogicDatabox>();
        private List<RegionNode> _availableNodes = new List<RegionNode>();
        private Dictionary<string, List<RegionNode>> _unfilledNodes = new Dictionary<string, List<RegionNode>>();

        public override BaseModuleSaveData SetupSaveData()
        {
            return new DataboxSaveData();
        }

        public override void PrepareRandomisation(RandoContext ctx)
        {
            _monitor.SphereCreated += OnSphereCreated;
            _ctx = ctx;
            _databoxes = _ctx.EntityManager.GetAllEntities<LogicDatabox>().ToList();
            
            // Grab a list of everything that can cause a new region to open. Filter to just those with databoxes.
            var prog = ctx.TravelManager.GetProgressionEntities(ctx.EntityManager, ctx.RegionManager);
            _progressionBoxes = _databoxes.Where(box => prog.Any(e => box.TechType == e.TechType)).ToList();
            
            // Take note of the available nodes for later.
            foreach (var box in _databoxes)
            {
                foreach (var node in box.DataboxPositions)
                {
                    if (!_unfilledNodes.TryGetValue(node.RegionName, out var nodes))
                    {
                        nodes = new List<RegionNode>();
                        _unfilledNodes.Add(node.RegionName, nodes);
                    }
                    nodes.Add(node);
                }
                
                // Remove the vanilla positions to make way for the randomised ones.
                box.DataboxPositions.Clear();
            }
        }

        public override void PreEntityRandomisation(IRandomHandler rng, SaveData saveData)
        {
            // It is possible for a databox entity to be up for randomising even though there are no valid spawn slots
            // available. To circumvent this, put at least one into a slot that is fairly accessible, at 200m or higher,
            // before the main loop even starts.
            var nodes = new List<RegionNode>();
            foreach (var (k, v) in _unfilledNodes)
            {
                nodes.AddRange(v.Where(node => node.Position.y > -200f));
            }
            
            foreach (var box in _progressionBoxes)
            {
                // Guarantee at least one fairly accessible box. 
                int idx = rng.Next(nodes.Count);
                var node = nodes[idx];
                box.DataboxPositions.Add(node);
                _log.Debug($"Essential box {box.TechType.AsString()} randomised into {node}");
                // Make sure the node does not get reused later.
                var filled = _unfilledNodes.First(kv => kv.Value.Contains(node));
                _unfilledNodes[filled.Key].Remove(node);
                nodes.RemoveAt(idx);
                
                // Make sure this box unlocks as soon as the region it is in becomes available.
                _ctx.RegionManager.GetRegion(filled.Key).Entities.Add(box);
                // Because the entity now unlocks instantly, it will not show up during the main loop. Add its other
                // spawns later, during post.
            }
        }

        public override void RandomiseEntity(IRandomHandler rng, SaveData saveData, LogicEntity entity)
        {
            var box = entity as LogicDatabox;
            while (box!.DataboxPositions.Count < 2)
            {
                if (_availableNodes.Count == 0)
                {
                    _log.Debug($"Ran out of spawn nodes in main loop. Postponing {box.TechType.AsString()}");
                    _recheckLater.Add(box);
                    break;
                }
                
                AddNode(rng, box);
            }
        }

        public override void PostEntityRandomisation(IRandomHandler rng, SaveData saveData)
        {
            // Ensure that every databox with a blueprint essential for progression exists at least three times.
            foreach (var essential in _progressionBoxes)
            {
                while (essential.DataboxPositions.Count < 3)
                {
                    AddNode(rng, essential);
                }
            }
            
            // Ensure at least two spawns for every databox that did not manage this during the main loop.
            foreach (var recheck in _recheckLater)
            {
                while (recheck.DataboxPositions.Count < 2)
                {
                    AddNode(rng, recheck);
                }
            }
            
            _log.Debug($"Distributing {_availableNodes.Count} spawn nodes at random.");
            var boxes = _databoxes.ShallowCopy();
            int max = 4;
            // Distribute the rest of the spawn points at random.
            while (_availableNodes.Count > 0 && boxes.Count > 0)
            {
                var boxIdx = rng.Next(boxes.Count);
                var box = boxes[boxIdx];
                if (box.DataboxPositions.Count >= max)
                {
                    boxes.RemoveAt(boxIdx);
                    continue;
                }
                
                AddNode(rng, box);
            }
            
            var save = saveData.GetModuleData<DataboxSaveData>();
            foreach (var box in _databoxes)
            {
                save.AddBox(box);
            }
        }

        private void AddNode(IRandomHandler rng, LogicDatabox databox)
        {
            if (_availableNodes.Count == 0)
                return;
            
            int idx = rng.Next(_availableNodes.Count);
            databox.DataboxPositions.Add(_availableNodes[idx]);
            _log.Debug($"Adding {_availableNodes[idx]} to box {databox.TechType.AsString()}.");
            _availableNodes.RemoveAt(idx);
        }

        public override void RegisterHarmonyPatches(Harmony harmony, SaveData saveData)
        {
            harmony.PatchAll(typeof(DataboxPatcher));
        }

        public override void ApplySerializedState(SaveData saveData)
        {
            // Happens via harmony patches.
        }

        public override void UndoSerializedState(SaveData saveData)
        {
        }

        private void OnSphereCreated(Sphere sphere)
        {
            foreach (var region in sphere.Regions)
            {
                if (!_unfilledNodes.TryGetValue(region.Name, out var nodes))
                    continue;
                
                _availableNodes.AddRange(nodes);
                // Remove the entry so we don't end up re-adding already randomised nodes with future spheres.
                _unfilledNodes.Remove(region.Name);
            }
        }
    }
}