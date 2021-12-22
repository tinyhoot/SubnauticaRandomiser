using System;
using System.Collections.Generic;
using SMLHelper.V2.Handlers;
using SubnauticaRandomiser.RandomiserObjects;
using UnityEngine;

namespace SubnauticaRandomiser.Logic
{
    internal class RandomiserLogic
    {
        private readonly System.Random _random;

        private readonly EntitySerializer _masterDict;
        private readonly RandomiserConfig _config;
        private Materials _materials;
        private ProgressionTree _tree;
        private List<Databox> _databoxes;
        private Mode _mode;

        public RandomiserLogic(System.Random random, EntitySerializer masterDict, RandomiserConfig config, List<LogicEntity> allMaterials, List<Databox> databoxes = null)
        {
            _random = random;
            _masterDict = masterDict;
            _config = config;
            _materials = new Materials(allMaterials);
            _databoxes = databoxes;
            _mode = null;
        }

        internal void RandomSmart(FragmentLogic fragmentLogic)
        {
            // This function uses the progression tree to randomise materials
            // and game progression in an intelligent way.
            //
            // Basic structure looks something like this:
            // - Set up vanilla progression tree
            // - Calculate reachable depth
            // - Put reachable raw materials and fish into _reachableMaterials
            // - Pick a random item from _allMaterials
            //   - Check if it has all dependencies, both as an item and as a
            //     blueprint, fulfilled. Abort and skip if not.
            //   - Randomise its ingredients using available materials
            //   - Add it to the list of reachable materials
            //     - If it's a knife, also add all seeds and chunks
            //     - If it's not an item (like a base piece), skip this step
            //   - Add it to the master dictionary
            //   - Recalculate reachable depth
            //   - Repeat
            // - Once all items have been randomised, do an integrity check for
            //   safety. Rocket, vehicles, and hatching enzymes must be on the list.

            LogHandler.Info("Randomising using logic-based system...");

            List<LogicEntity> toBeRandomised = new List<LogicEntity>();
            Dictionary<TechType, bool> unlockedProgressionItems = new Dictionary<TechType, bool>();
            _tree = new ProgressionTree();
            int reachableDepth = 0;

            // Init the progression tree
            _tree.SetupVanillaTree();
            if (_config.bVanillaUpgradeChains)
                _tree.ApplyUpgradeChainToPrerequisites(_materials.GetAll());

            // Init the mode that will be used
            switch (_config.iRandomiserMode)
            {
                case (0):
                    _mode = new ModeBalanced(_config, _materials, _tree, _random);
                    break;
                case (1):
                    _mode = new ModeRandom(_config, _materials, _tree, _random);
                    break;
            }

            // If databox randomising is enabled, go and do that.
            if (_config.bRandomiseDataboxes && _databoxes != null)
            {
                _databoxes = RandomiseDataboxes(_masterDict, _databoxes);
            }

            foreach (LogicEntity e in _materials.GetAll().FindAll(x => 
                                                            !x.Category.Equals(ETechTypeCategory.RawMaterials) 
                                                         && !x.Category.Equals(ETechTypeCategory.Fish) 
                                                         && !x.Category.Equals(ETechTypeCategory.Seeds)
                                                         && !x.Category.Equals(ETechTypeCategory.Eggs)))
            {
                toBeRandomised.Add(e);
            }

            // Iterate over every single entity in the game until all of them
            // are considered randomised.
            bool newProgressionItem = true;
            int circuitbreaker = 0;
            while (toBeRandomised.Count > 0)
            {
                circuitbreaker++;
                if (circuitbreaker > 3000)
                {
                    LogHandler.MainMenuMessage("Failed to randomise items: stuck in infinite loop!");
                    LogHandler.Fatal("Encountered infinite loop, aborting!");
                    break;
                }

                int newDepth = 0;
                // If the previous cycle randomised an entity that was critical
                // and possibly allows for reaching greater depths, recalculate.
                if (newProgressionItem)
                {
                    newDepth = CalculateReachableDepth(_tree, unlockedProgressionItems, _config.iDepthSearchTime);
                    if (SpoilerLog.s_progression.Count > 0)
                    {
                        KeyValuePair<TechType, int> valuePair = new KeyValuePair<TechType, int>(SpoilerLog.s_progression[SpoilerLog.s_progression.Count - 1].Key, newDepth);
                        SpoilerLog.s_progression.RemoveAt(SpoilerLog.s_progression.Count - 1);
                        SpoilerLog.s_progression.Add(valuePair);
                    }
                }

                // If the most recently randomised entity opened up some new paths
                // to progress, update the list of reachable materials.
                if (newProgressionItem || (newDepth > reachableDepth))
                {
                    reachableDepth = newDepth > reachableDepth ? newDepth : reachableDepth;
                    UpdateReachableMaterials(reachableDepth);
                }

                LogicEntity nextEntity = null;
                newProgressionItem = false;
                bool isPriority = false;

                // Make sure the list of absolutely essential items is done first,
                // for each depth level. This guarantees certain recipes are done
                // by a certain depth, e.g. waterparks by 500m.
                nextEntity = GetPriorityEntity(reachableDepth);

                // Once all essentials and electives are done, grab a random entity 
                // which has not yet been randomised.
                if (nextEntity is null)
                    nextEntity = GetRandom(toBeRandomised);
                else
                    isPriority = true;

                // If the entity is a fragment, go handle that.
                // TODO implement proper depth restrictions and config options.
                if (nextEntity.Category.Equals(ETechTypeCategory.Fragments))
                {
                    if (_config.bRandomiseFragments && fragmentLogic != null)
                        fragmentLogic.RandomiseFragment(nextEntity, reachableDepth);

                    toBeRandomised.Remove(nextEntity);
                    nextEntity.InLogic = true;
                    continue;
                }

                // HACK improve this. Currently makes logic only consider recipes.
                if (!nextEntity.HasRecipe)
                    continue;

                // Does this recipe have all of its prerequisites fulfilled?
                // Skip this check if the recipe is a priority (essential or elective)
                if (isPriority || (CheckRecipeForBlueprint(_masterDict, _databoxes, nextEntity, reachableDepth) && CheckRecipeForPrerequisites(_masterDict, nextEntity)))
                {
                    // Found a good recipe! Randomise it.
                    toBeRandomised.Remove(nextEntity);
                    newProgressionItem = RandomiseRecipeEntity(nextEntity, unlockedProgressionItems, reachableDepth);

                    LogHandler.Debug("[+] Randomised recipe for [" + nextEntity.TechType.AsString() + "].");
                }
                else
                {
                    LogHandler.Debug("--- Recipe [" + nextEntity.TechType.AsString() + "] did not fulfill requirements, skipping.");
                }
            }

            LogHandler.Info("Finished randomising within " + circuitbreaker + " cycles!");
        }

        // Handle everything related to actually randomising the recipe itself,
        // and ensure all special cases are covered.
        // Returns true if a new progression item was unlocked.
        private bool RandomiseRecipeEntity(LogicEntity entity, Dictionary<TechType, bool> unlockedProgressionItems, int reachableDepth)
        {
            bool newProgressionItem = false;

            entity = _mode.RandomiseIngredients(entity);
            ApplyRandomisedRecipe(_masterDict, entity.Recipe);

            // Only add this entity to the materials list if it can be an ingredient.
            if (entity.CanFunctionAsIngredient())
                _materials.AddReachable(entity);

            // Knives are a special case that open up a lot of new materials.
            if ((entity.TechType.Equals(TechType.Knife) || entity.TechType.Equals(TechType.HeatBlade)) && !unlockedProgressionItems.ContainsKey(TechType.Knife))
            {
                unlockedProgressionItems.Add(TechType.Knife, true);
                newProgressionItem = true;
            }

            // Similarly, Alien Containment is a special case for eggs.
            if (entity.TechType.Equals(TechType.BaseWaterPark) && _config.bUseEggs)
            {
                unlockedProgressionItems.Add(TechType.BaseWaterPark, true);
                newProgressionItem = true;
            }

            // If it is a central depth progression item, consider it unlocked.
            if (_tree.DepthProgressionItems.ContainsKey(entity.TechType) && !unlockedProgressionItems.ContainsKey(entity.TechType))
            {
                unlockedProgressionItems.Add(entity.TechType, true);
                SpoilerLog.s_progression.Add(new KeyValuePair<TechType, int>(entity.TechType, 0));
                newProgressionItem = true;

                LogHandler.Debug("[+] Added " + entity.TechType.AsString() + " to progression items.");
            }

            entity.InLogic = true;

            return newProgressionItem;
        }

        // Randomise the blueprints found inside databoxes.
        internal List<Databox> RandomiseDataboxes(EntitySerializer masterDict, List<Databox> databoxes)
        {
            masterDict.Databoxes = new Dictionary<RandomiserVector, TechType>();
            List<Databox> randomDataboxes = new List<Databox>();
            List<Vector3> toBeRandomised = new List<Vector3>();

            foreach (Databox dbox in databoxes)
            {
                toBeRandomised.Add(dbox.Coordinates);
            }

            foreach (Databox originalBox in databoxes)
            {
                int next = _random.Next(0, toBeRandomised.Count);
                Databox replacementBox = databoxes.Find(x => x.Coordinates.Equals(toBeRandomised[next]));

                randomDataboxes.Add(new Databox(originalBox.TechType, toBeRandomised[next], replacementBox.Wreck, replacementBox.RequiresLaserCutter, replacementBox.RequiresPropulsionCannon));
                masterDict.Databoxes.Add(new RandomiserVector(toBeRandomised[next]), originalBox.TechType);
                LogHandler.Debug("Databox " + toBeRandomised[next].ToString() + " with " + replacementBox.TechType.AsString() + " now contains " + originalBox.TechType.AsString());
                toBeRandomised.RemoveAt(next);
            }
            masterDict.isDataboxRandomised = true;

            return randomDataboxes;
        }

        // Grab an essential or elective entity for the currently reachable depth.
        private LogicEntity GetPriorityEntity(int depth)
        {
            List<TechType> essentialItems = _tree.GetEssentialItems(depth);
            List<TechType[]> electiveItems = _tree.GetElectiveItems(depth);
            LogicEntity entity = null;

            // Always get one of the essential items first, if available.
            if (essentialItems != null && essentialItems.Count > 0)
            {
                entity = _materials.GetAll().Find(x => x.TechType.Equals(essentialItems[0]));
                essentialItems.RemoveAt(0);
                LogHandler.Debug("Prioritising essential item " + entity.TechType.AsString() + " for depth " + depth);

                // If this has already been randomised, all the better.
                if (_masterDict.RecipeDict.ContainsKey(entity.TechType))
                {
                    entity = null;
                    LogHandler.Debug("Priority item was already randomised, skipping.");
                }
            }

            // Similarly, if all essential items are done, grab one from among
            // the elective items and leave the rest up to chance.
            if (entity is null && electiveItems != null && electiveItems.Count > 0)
            {
                TechType[] electiveTypes = electiveItems[0];
                electiveItems.RemoveAt(0);

                if (ContainsAny(_masterDict, electiveTypes))
                {
                    LogHandler.Debug("Priority elective containing " + electiveTypes[0].AsString() + " was already randomised, skipping.");
                }
                else
                {
                    TechType nextType = GetRandom(new List<TechType>(electiveTypes));
                    entity = _materials.GetAll().Find(x => x.TechType.Equals(nextType));
                    LogHandler.Debug("Prioritising elective item " + entity.TechType.AsString() + " for depth " + depth);
                }
            }

            return entity;
        }

        // Add all reachable materials to the list, taking into account depth and
        // any config options.
        internal void UpdateReachableMaterials(int depth)
        {
            if (_masterDict.ContainsKnife())
            {
                _materials.AddReachable(ETechTypeCategory.RawMaterials, depth);
            }
            else
            {
                _materials.AddReachableWithPrereqs(ETechTypeCategory.RawMaterials, depth, TechType.Knife, true);
            }

            if (_config.bUseFish)
                _materials.AddReachable(ETechTypeCategory.Fish, depth);
            if (_config.bUseSeeds && _masterDict.ContainsKnife())
                _materials.AddReachable(ETechTypeCategory.Seeds, depth);
            if (_config.bUseEggs && _masterDict.RecipeDict.ContainsKey(TechType.BaseWaterPark))
                _materials.AddReachable(ETechTypeCategory.Eggs, depth);
        }

        // This function calculates the maximum reachable depth based on
        // what vehicles the player has attained, as well as how much
        // further they can go "on foot"
        // TODO: Simplify this.
        internal static int CalculateReachableDepth(ProgressionTree tree, Dictionary<TechType, bool> progressionItems, int depthTime = 15)
        {
            double swimmingSpeed = 4.7; // Assuming player is holding a tool.
            double seaglideSpeed = 11.0;
            bool seaglide = progressionItems.ContainsKey(TechType.Seaglide);
            double finSpeed = 0.0;
            double tankPenalty = 0.0;
            int breathTime = 45;

            // How long should the player be able to remain at this depth and
            // still make it back just fine?
            int searchTime = depthTime;
            // Never assume the player has to go deeper than this on foot.
            int maxSoloDepth = 300;
            int vehicleDepth = 0;
            double playerDepthRaw = 0.0;
            double totalDepth = 0.0;

            LogHandler.Debug("===== Recalculating reachable depth =====");

            // This feels like it could be simplified.
            // Also, this trusts that the tree is set up correctly.
            foreach (TechType[] path in tree.GetProgressionPath(EProgressionNode.Depth200m).Pathways)
            {
                if (CheckDictForAllTechTypes(progressionItems, path))
                    vehicleDepth = 200;
            }
            foreach (TechType[] path in tree.GetProgressionPath(EProgressionNode.Depth300m).Pathways)
            {
                if (CheckDictForAllTechTypes(progressionItems, path))
                    vehicleDepth = 300;
            }
            foreach (TechType[] path in tree.GetProgressionPath(EProgressionNode.Depth500m).Pathways)
            {
                if (CheckDictForAllTechTypes(progressionItems, path))
                    vehicleDepth = 500;
            }
            foreach (TechType[] path in tree.GetProgressionPath(EProgressionNode.Depth900m).Pathways)
            {
                if (CheckDictForAllTechTypes(progressionItems, path))
                    vehicleDepth = 900;
            }
            foreach (TechType[] path in tree.GetProgressionPath(EProgressionNode.Depth1300m).Pathways)
            {
                if (CheckDictForAllTechTypes(progressionItems, path))
                    vehicleDepth = 1300;
            }
            foreach (TechType[] path in tree.GetProgressionPath(EProgressionNode.Depth1700m).Pathways)
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

            // The vehicle depth and whether or not the player has a rebreather
            // can modify the raw achievable diving depth.
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

        // Check if this recipe fulfills all conditions to have its blueprint be unlocked
        private bool CheckRecipeForBlueprint(EntitySerializer masterDict, List<Databox> databoxes, LogicEntity entity, int depth)
        {
            bool fulfilled = true;

            if (entity.Blueprint == null || (entity.Blueprint.UnlockConditions == null && entity.Blueprint.UnlockDepth == 0))
                return true;

            // If the databox was randomised, do work to account for new locations.
            // Cyclops hull modules need extra special treatment.
            if (entity.Blueprint.NeedsDatabox && databoxes != null && databoxes.Count > 0 && !entity.TechType.Equals(TechType.CyclopsHullModule2) && !entity.TechType.Equals(TechType.CyclopsHullModule3))
            {
                int total = 0;
                int number = 0;
                int lasercutter = 0;
                int propulsioncannon = 0;

                foreach (Databox box in databoxes.FindAll(x => x.TechType.Equals(entity.TechType)))
                {
                    total += (int)Math.Abs(box.Coordinates.y);
                    number++;

                    if (box.RequiresLaserCutter)
                        lasercutter++;
                    if (box.RequiresPropulsionCannon)
                        propulsioncannon++;
                }

                LogHandler.Debug("[B] Found " + number + " databoxes for " + entity.TechType.AsString());

                entity.Blueprint.UnlockDepth = total / number;
                if (entity.TechType.Equals(TechType.CyclopsHullModule1))
                {
                    _materials.GetAll().Find(x => x.TechType.Equals(TechType.CyclopsHullModule2)).Blueprint.UnlockDepth = total / number;
                    _materials.GetAll().Find(x => x.TechType.Equals(TechType.CyclopsHullModule3)).Blueprint.UnlockDepth = total / number;
                }

                // If more than half of all locations of this databox require a
                // tool to access the box, add it to the requirements for the recipe
                if (lasercutter / number >= 0.5)
                {
                    entity.Blueprint.UnlockConditions.Add(TechType.LaserCutter);
                    if (entity.TechType.Equals(TechType.CyclopsHullModule1))
                    {
                        _materials.GetAll().Find(x => x.TechType.Equals(TechType.CyclopsHullModule2)).Blueprint.UnlockConditions.Add(TechType.LaserCutter);
                        _materials.GetAll().Find(x => x.TechType.Equals(TechType.CyclopsHullModule3)).Blueprint.UnlockConditions.Add(TechType.LaserCutter);
                    }
                }

                if (propulsioncannon / number >= 0.5)
                {
                    entity.Blueprint.UnlockConditions.Add(TechType.PropulsionCannon);
                    if (entity.TechType.Equals(TechType.CyclopsHullModule1))
                    {
                        _materials.GetAll().Find(x => x.TechType.Equals(TechType.CyclopsHullModule2)).Blueprint.UnlockConditions.Add(TechType.PropulsionCannon);
                        _materials.GetAll().Find(x => x.TechType.Equals(TechType.CyclopsHullModule3)).Blueprint.UnlockConditions.Add(TechType.PropulsionCannon);
                    }
                }
            }

            foreach (TechType condition in entity.Blueprint.UnlockConditions)
            {
                LogicEntity conditionEntity = _materials.GetAll().Find(x => x.TechType.Equals(condition));

                // Without this piece, the Air bladder will hang if fish are not
                // enabled for the logic, as it fruitlessly searches for a bladderfish
                // which never enters its algorithm.
                // Eggs and seeds are never problematic in vanilla, but are covered
                // in case users add their own modded items with those.
                if (!_config.bUseFish && conditionEntity.Category.Equals(ETechTypeCategory.Fish))
                    continue;
                if (!_config.bUseEggs && conditionEntity.Category.Equals(ETechTypeCategory.Eggs))
                    continue;
                if (!_config.bUseSeeds && conditionEntity.Category.Equals(ETechTypeCategory.Seeds))
                    continue;

                fulfilled &= (masterDict.RecipeDict.ContainsKey(condition) || _materials.GetReachable().Exists(x => x.TechType.Equals(condition)));

                if (!fulfilled)
                    return false;
            }

            if (entity.Blueprint.UnlockDepth > depth)
            {
                fulfilled = false;
            }

            return fulfilled;
        }

        private static bool CheckRecipeForPrerequisites(EntitySerializer masterDict, LogicEntity entity)
        {
            bool fulfilled = true;

            // The builder tool must always be randomised before any base pieces
            // ever become accessible.
            if (entity.Category.IsBasePiece() && !masterDict.RecipeDict.ContainsKey(TechType.Builder))
                return false;

            if (entity.Prerequisites == null)
                return true;

            foreach (TechType t in entity.Prerequisites)
            {
                fulfilled &= masterDict.RecipeDict.ContainsKey(t);
                if (!fulfilled)
                    break;
            }

            return fulfilled;
        }

        private bool ContainsAny(EntitySerializer masterDict, TechType[] types)
        {
            foreach (TechType type in types)
            {
                if (masterDict.RecipeDict.ContainsKey(type))
                    return true;
            }
            return false;
        }

        private T GetRandom<T>(List<T> list)
        {
            if (list == null || list.Count == 0)
            {
                return default(T);
            }

            return list[_random.Next(0, list.Count)];
        }

        // Grab a collection of all keys in the dictionary, then use them to
        // apply every single one as a recipe change in the game.
        internal static void ApplyMasterDict(EntitySerializer masterDict)
        {
            Dictionary<TechType, Recipe>.KeyCollection keys = masterDict.RecipeDict.Keys;

            foreach (TechType key in keys)
            {
                CraftDataHandler.SetTechData(key, masterDict.RecipeDict[key]);
            }

            // TODO Once scrap metal is working, un-commenting this will apply the
            // change on every startup.
            //ChangeScrapMetalResult(masterDict.DictionaryInstance[TechType.Titanium]);
        }

        // This function handles applying a randomised recipe to the in-game
        // craft data, and stores a copy in the master dictionary.
        internal static void ApplyRandomisedRecipe(EntitySerializer masterDict, Recipe recipe)
        {
            CraftDataHandler.SetTechData(recipe.TechType, recipe);
            masterDict.AddRecipe(recipe.TechType, recipe);
        }
    }
}
