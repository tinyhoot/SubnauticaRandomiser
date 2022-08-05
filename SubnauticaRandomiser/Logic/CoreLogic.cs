using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using SMLHelper.V2.Handlers;
using SubnauticaRandomiser.Logic.Recipes;
using SubnauticaRandomiser.RandomiserObjects;
using UnityEngine;
using Random = System.Random;

namespace SubnauticaRandomiser.Logic
{
    /// <summary>
    /// Acts as the core for handling all randomising logic in the mod, and turning modules on/off as needed.
    /// </summary>
    internal class CoreLogic
    {
        internal readonly RandomiserConfig _config;
        internal readonly List<Databox> _databoxes;
        internal readonly EntitySerializer _masterDict;
        internal readonly Materials _materials;
        internal readonly System.Random _random;
        internal readonly SpoilerLog _spoilerLog;
        internal readonly ProgressionTree _tree;

        private readonly DataboxLogic _databoxLogic;
        private readonly FragmentLogic _fragmentLogic;
        private readonly RecipeLogic _recipeLogic;

        public CoreLogic(System.Random random, EntitySerializer masterDict, RandomiserConfig config,
            List<LogicEntity> allMaterials, List<BiomeCollection> biomes = null, List<Databox> databoxes = null)
        {
            _config = config;
            _databoxes = databoxes;
            _masterDict = masterDict;
            _materials = new Materials(allMaterials);
            _random = random;
            _spoilerLog = new SpoilerLog(config);

            // TODO: Respect config options.
            _databoxLogic = new DataboxLogic(this);
            _fragmentLogic = new FragmentLogic(this, biomes);
            _recipeLogic = new RecipeLogic(this);
            _tree = new ProgressionTree();
        }

        /// <summary>
        /// Set up all the necessary structures for later.
        /// </summary>
        private void Setup(List<LogicEntity> notRandomised, Dictionary<TechType, bool> unlockedProgressionItems)
        {
            if (_recipeLogic != null)
            {
                _recipeLogic.UpdateReachableMaterials(0);
                // Queue up all craftables to be randomised.
                notRandomised.AddRange(_materials.GetAllCraftables());
                
                // Init the progression tree.
                _tree.SetupVanillaTree();
                if (_config.bVanillaUpgradeChains)
                    _tree.ApplyUpgradeChainToPrerequisites(_materials.GetAll());
            }

            if (_databoxLogic != null)
            {
                // Just randomise those flat out for now, instead of including them in the core loop.
                _databoxLogic.RandomiseDataboxes();
            }

            if (_fragmentLogic != null)
            {
                // Initialise the fragment cache and remove vanilla spawns.
                FragmentLogic.Init();
                // Queue up all fragments to be randomised.
                notRandomised.AddRange(_materials.GetAllFragments());
            }
        }
        
        internal void Randomise()
        {
            LogHandler.Info("Randomising using logic-based system...");
            
            List<LogicEntity> notRandomised = new List<LogicEntity>();
            Dictionary<TechType, bool> unlockedProgressionItems = new Dictionary<TechType, bool>();

            // Set up basic structures.
            Setup(notRandomised, unlockedProgressionItems);

            int circuitbreaker = 0;
            int currentDepth = 0;
            int numProgressionItems = unlockedProgressionItems.Count;
            while (notRandomised.Count > 0)
            {
                circuitbreaker++;
                if (circuitbreaker > 3000)
                {
                    LogHandler.MainMenuMessage("Failed to randomise items: stuck in infinite loop!");
                    LogHandler.Fatal("Encountered infinite loop, aborting!");
                    // TODO: Throw exception.
                    break;
                }
                
                // Update depth and reachable materials.
                currentDepth = UpdateReachableDepth(currentDepth, unlockedProgressionItems, numProgressionItems);
                numProgressionItems = unlockedProgressionItems.Count;

                LogicEntity nextEntity = ChooseNextEntity(notRandomised, currentDepth);

                // Choose a logic appropriate to the entity.
                if (nextEntity.IsFragment)
                {
                    // TODO implement proper depth restrictions and config options.
                    if (_config.bRandomiseFragments && _fragmentLogic != null)
                        _fragmentLogic.RandomiseFragment(nextEntity, currentDepth);

                    notRandomised.Remove(nextEntity);
                    nextEntity.InLogic = true;
                    continue;
                }

                if (nextEntity.HasRecipe)
                {
                    bool success = _recipeLogic.RandomiseRecipe(nextEntity, unlockedProgressionItems, currentDepth);
                    if (success)
                    {
                        notRandomised.Remove(nextEntity);
                        nextEntity.InLogic = true;
                    }

                    continue;
                }
                
                LogHandler.Warn("Unsupported entity in loop: " + nextEntity);
            }

            _spoilerLog.WriteLog();
            LogHandler.Info("Finished randomising within " + circuitbreaker + " cycles!");
        }

        /// <summary>
        /// Get the next entity to be randomised, prioritising essential or elective ones.
        /// </summary>
        /// <returns>The next entity.</returns>
        private LogicEntity ChooseNextEntity(List<LogicEntity> notRandomised, int depth)
        {
            // Make sure the list of absolutely essential items is done first, for each depth level. This guarantees
            // certain recipes are done by a certain depth, e.g. waterparks by 500m.
            // Automatically fails if recipes do not get randomised.
            LogicEntity next = _recipeLogic?.GetPriorityEntity(depth);
            next ??= GetRandom(notRandomised);

            return next;
        }

        /// <summary>
        /// This function calculates the maximum reachable depth based on what vehicles the player has attained, as well
        /// as how much further they can go "on foot"
        /// TODO: Simplify this.
        /// </summary>
        /// <param name="progressionItems">A list of all currently reachable items relevant for progression.</param>
        /// <param name="depthTime">The minimum time that it must be possible to spend at the reachable depth before
        /// resurfacing.</param>
        /// <returns>The reachable depth.</returns>
        internal int CalculateReachableDepth(Dictionary<TechType, bool> progressionItems, int depthTime = 15)
        {
            double swimmingSpeed = 4.7; // Assuming player is holding a tool.
            double seaglideSpeed = 11.0;
            bool seaglide = progressionItems.ContainsKey(TechType.Seaglide);
            double finSpeed = 0.0;
            double tankPenalty = 0.0;
            int breathTime = 45;

            // How long should the player be able to remain at this depth and still make it back just fine?
            int searchTime = depthTime;
            // Never assume the player has to go deeper than this on foot.
            int maxSoloDepth = 300;
            int vehicleDepth = 0;
            double playerDepthRaw;
            double totalDepth;

            LogHandler.Debug("===== Recalculating reachable depth =====");

            // This feels like it could be simplified.
            // Also, this trusts that the tree is set up correctly.
            foreach (TechType[] path in _tree.GetProgressionPath(EProgressionNode.Depth200m).Pathways)
            {
                if (CheckDictForAllTechTypes(progressionItems, path))
                    vehicleDepth = 200;
            }
            foreach (TechType[] path in _tree.GetProgressionPath(EProgressionNode.Depth300m).Pathways)
            {
                if (CheckDictForAllTechTypes(progressionItems, path))
                    vehicleDepth = 300;
            }
            foreach (TechType[] path in _tree.GetProgressionPath(EProgressionNode.Depth500m).Pathways)
            {
                if (CheckDictForAllTechTypes(progressionItems, path))
                    vehicleDepth = 500;
            }
            foreach (TechType[] path in _tree.GetProgressionPath(EProgressionNode.Depth900m).Pathways)
            {
                if (CheckDictForAllTechTypes(progressionItems, path))
                    vehicleDepth = 900;
            }
            foreach (TechType[] path in _tree.GetProgressionPath(EProgressionNode.Depth1300m).Pathways)
            {
                if (CheckDictForAllTechTypes(progressionItems, path))
                    vehicleDepth = 1300;
            }
            foreach (TechType[] path in _tree.GetProgressionPath(EProgressionNode.Depth1700m).Pathways)
            {
                if (CheckDictForAllTechTypes(progressionItems, path))
                    vehicleDepth = 1700;
            }

            if (progressionItems.ContainsKey(TechType.Fins))
                finSpeed = 1.41;
            if (progressionItems.ContainsKey(TechType.UltraGlideFins))
                finSpeed = 1.88;

            // How deep can the player go without any tanks?
            playerDepthRaw = (breathTime - searchTime) * (seaglide ? seaglideSpeed : (swimmingSpeed + finSpeed)) / 2;

            // But can they go deeper with a tank? (Yes.)
            if (progressionItems.ContainsKey(TechType.Tank))
            {
                breathTime = 75;
                tankPenalty = 0.4;
                double depth = (breathTime - searchTime) * (seaglide ? seaglideSpeed : (swimmingSpeed + finSpeed - tankPenalty)) / 2;
                playerDepthRaw = depth > playerDepthRaw ? depth : playerDepthRaw;
            }

            if (progressionItems.ContainsKey(TechType.DoubleTank))
            {
                breathTime = 135;
                tankPenalty = 0.47;
                double depth = (breathTime - searchTime) * (seaglide ? seaglideSpeed : (swimmingSpeed + finSpeed - tankPenalty)) / 2;
                playerDepthRaw = depth > playerDepthRaw ? depth : playerDepthRaw;
            }

            if (progressionItems.ContainsKey(TechType.HighCapacityTank))
            {
                breathTime = 225;
                tankPenalty = 0.6;
                double depth = (breathTime - searchTime) * (seaglide ? seaglideSpeed : (swimmingSpeed + finSpeed - tankPenalty)) / 2;
                playerDepthRaw = depth > playerDepthRaw ? depth : playerDepthRaw;
            }

            if (progressionItems.ContainsKey(TechType.PlasteelTank))
            {
                breathTime = 135;
                tankPenalty = 0.1;
                double depth = (breathTime - searchTime) * (seaglide ? seaglideSpeed : (swimmingSpeed + finSpeed - tankPenalty)) / 2;
                playerDepthRaw = depth > playerDepthRaw ? depth : playerDepthRaw;
            }

            // The vehicle depth and whether or not the player has a rebreather can modify the raw achievable diving depth.
            if (progressionItems.ContainsKey(TechType.Rebreather))
            {
                totalDepth = vehicleDepth + (playerDepthRaw > maxSoloDepth ? maxSoloDepth : playerDepthRaw);
            }
            else
            {
                // Below 100 meters, air is consumed three times as fast.
                // Below 200 meters, it is consumed five times as fast.
                double depth = 0.0;

                if (vehicleDepth == 0)
                {
                    if (playerDepthRaw <= 100)
                    {
                        depth = playerDepthRaw;
                    }
                    else
                    {
                        depth += 100;
                        playerDepthRaw -= 100;

                        // For anything between 100-200 meters, triple air consumption
                        if (playerDepthRaw <= 100)
                        {
                            depth += playerDepthRaw / 3;
                        }
                        else
                        {
                            depth += 33.3;
                            playerDepthRaw -= 100;
                            // For anything below 200 meters, quintuple it.
                            depth += playerDepthRaw / 5;
                        }
                    }
                }
                else
                {
                    depth = playerDepthRaw / 5;
                }

                totalDepth = vehicleDepth + (depth > maxSoloDepth ? maxSoloDepth : depth);
            }
            LogHandler.Debug("===== New reachable depth: " + totalDepth + " =====");

            return (int)totalDepth;
        }

        /// <summary>
        /// Update the depth that can be reached and trigger any changes that need to happen if a new significant
        /// threshold has been passed.
        /// </summary>
        /// <param name="currentDepth">The previously reachable depth.</param>
        /// <param name="progressionItems">The unlocked progression items.</param>
        /// <param name="numItems">The number of progression items on the previous cycle.</param>
        /// <returns>The new maximum depth.</returns>
        private int UpdateReachableDepth(int currentDepth, Dictionary<TechType, bool> progressionItems, int numItems)
        {
            if (progressionItems.Count <= numItems)
                return currentDepth;
            
            int newDepth = CalculateReachableDepth(progressionItems);
            _spoilerLog.UpdateLastProgressionEntry(newDepth);
            currentDepth = Math.Max(currentDepth, newDepth);
            _recipeLogic?.UpdateReachableMaterials(currentDepth);

            return currentDepth;
        }

        /// <summary>
        /// Check whether all TechTypes given in the array are present in the given dictionary.
        /// </summary>
        /// <param name="dict">The dictionary to check.</param>
        /// <param name="types">The array of TechTypes.</param>
        /// <returns>True if all TechTypes are present in the dictionary, false otherwise.</returns>
        private static bool CheckDictForAllTechTypes(Dictionary<TechType, bool> dict, TechType[] types)
        {
            bool allItemsPresent = true;

            foreach (TechType t in types)
            {
                allItemsPresent &= dict.ContainsKey(t);
                if (!allItemsPresent)
                    break;
            }

            return allItemsPresent;
        }

        /// <summary>
        /// Check wether any of the given TechTypes have already been randomised.
        /// </summary>
        /// <param name="masterDict">The master dictionary.</param>
        /// <param name="types">The TechTypes.</param>
        /// <returns>True if any TechType in the array has been randomised, false otherwise.</returns>
        public bool ContainsAny(EntitySerializer masterDict, TechType[] types)
        {
            foreach (TechType type in types)
            {
                if (masterDict.RecipeDict.ContainsKey(type))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Get a random element from a list.
        /// </summary>
        public T GetRandom<T>(List<T> list)
        {
            if (list == null || list.Count == 0)
            {
                return default(T);
            }

            return list[_random.Next(0, list.Count)];
        }
    }
}
