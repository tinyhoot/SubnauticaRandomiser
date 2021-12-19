using System;
using System.Collections.Generic;
using SMLHelper.V2.Crafting;
using SMLHelper.V2.Handlers;
using SubnauticaRandomiser.RandomiserObjects;
using UnityEngine;

namespace SubnauticaRandomiser.Logic
{
    internal class RandomiserLogic
    {
        private readonly System.Random _random;

        private readonly RandomiserConfig _config;
        private Materials _materials;
        private ProgressionTree _tree;
        private List<Databox> _databoxes;
        private Mode _mode;

        public RandomiserLogic(RandomiserConfig config, List<LogicEntity> allMaterials, List<Databox> databoxes = null, int seed = 0)
        {
            if (seed == 0)
                _random = new System.Random();
            else
                _random = new System.Random(seed);

            _config = config;
            _materials = new Materials(allMaterials);
            _databoxes = databoxes;
            _mode = null;
        }

        internal void RandomSmart(RecipeDictionary masterDict)
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
                _databoxes = RandomiseDataboxes(masterDict, _databoxes);
            }

            // If base theming is enabled, choose a theming ingredient.
            // FIXME Overlap with Mode.ChooseBaseTheme()
            if (_config.bDoBaseTheming)
            {
                // TODO Get this working.
                //ChangeScrapMetalResult(_baseTheme);
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
                    newProgressionItem = false;
                }

                // If the most recently randomised entity opened up some new paths
                // to progress, there's extra stuff to handle.
                if (newDepth > reachableDepth)
                {
                    reachableDepth = newDepth;

                    // Exclude creepvine and samples until a knife is obtained.
                    if (masterDict.DictionaryInstance.ContainsKey(TechType.Knife))
                    {
                        _materials.AddReachable(ETechTypeCategory.RawMaterials, reachableDepth);
                    }
                    else
                    {
                        _materials.AddReachableWithPrereqs(ETechTypeCategory.RawMaterials, reachableDepth, TechType.Knife, true);
                    }

                    if (_config.bUseFish)
                        _materials.AddReachable(ETechTypeCategory.Fish, reachableDepth);
                    if (_config.bUseSeeds && unlockedProgressionItems.ContainsKey(TechType.Knife))
                        _materials.AddReachable(ETechTypeCategory.Seeds, reachableDepth);
                    if (_config.bUseEggs && masterDict.DictionaryInstance.ContainsKey(TechType.BaseWaterPark))
                        _materials.AddReachable(ETechTypeCategory.Eggs, reachableDepth);
                }

                LogicEntity nextEntity = null;

                // Make sure the list of absolutely essential items is done first,
                // for each depth level. This guarantees certain recipes are done
                // by a certain depth, e.g. waterparks by 500m.
                List<TechType> essentialItems = _tree.GetEssentialItems(reachableDepth);
                if (essentialItems != null)
                {
                    nextEntity = _materials.GetAll().Find(x => x.TechType.Equals(essentialItems[0]));
                    essentialItems.RemoveAt(0);
                    LogHandler.Debug("Prioritising essential item " + nextEntity.TechType.AsString() + " for depth " + reachableDepth);

                    // If this has already been randomised, all the better.
                    if (masterDict.DictionaryInstance.ContainsKey(nextEntity.TechType))
                    {
                        nextEntity = null;
                        LogHandler.Debug("Priority item was already randomised, skipping.");
                    }
                }

                // Similarly, if all essential items are done, grab one from among
                // the elective items and leave the rest up to chance.
                List<TechType[]> electiveItems = _tree.GetElectiveItems(reachableDepth);
                if (nextEntity is null && electiveItems != null && electiveItems.Count > 0)
                {
                    TechType[] electiveTypes = electiveItems[0];
                    electiveItems.RemoveAt(0);

                    if (ContainsAny(masterDict, electiveTypes))
                    {
                        LogHandler.Debug("Priority elective containing " + electiveTypes[0].AsString() + " was already randomised, skipping.");
                    }
                    else
                    {
                        TechType nextType = GetRandom(new List<TechType>(electiveTypes));
                        nextEntity = _materials.GetAll().Find(x => x.TechType.Equals(nextType));
                        LogHandler.Debug("Prioritising elective item " + nextEntity.TechType.AsString() + " for depth " + reachableDepth);
                    }
                }

                // Once all essentials and electives are done, grab a random entity 
                // which has not yet been randomised.
                if (nextEntity is null)
                    nextEntity = GetRandom(toBeRandomised);

                // HACK improve this.
                if (!nextEntity.HasRecipe)
                    continue;

                // Does this recipe have all of its prerequisites fulfilled?
                if (CheckRecipeForBlueprint(masterDict, _databoxes, nextEntity, reachableDepth) && CheckRecipeForPrerequisites(masterDict, nextEntity))
                {
                    // Found a good recipe! Randomise it.
                    nextEntity = _mode.RandomiseIngredients(nextEntity);

                    // Make sure it's not an item that cannot be an ingredient.
                    if (nextEntity.CanFunctionAsIngredient())
                        _materials.AddReachable(nextEntity);
                    ApplyRandomisedRecipe(masterDict, nextEntity.Recipe);
                    toBeRandomised.Remove(nextEntity);
                    
                    // Handling knives as a special case.
                    if ((nextEntity.Equals(TechType.Knife) || nextEntity.Equals(TechType.HeatBlade)) && !unlockedProgressionItems.ContainsKey(TechType.Knife))
                    {
                        unlockedProgressionItems.Add(TechType.Knife, true);
                        newProgressionItem = true;
                        // Add raw materials like creepvine and mushroom samples.
                        _materials.AddReachableWithPrereqs(ETechTypeCategory.RawMaterials, reachableDepth, TechType.Knife);
                        if (_config.bUseSeeds)
                            _materials.AddReachable(ETechTypeCategory.Seeds, reachableDepth);
                    }
                    // Similarly, Alien Containment is a special case for eggs.
                    if (nextEntity.Equals(TechType.BaseWaterPark) && _config.bUseEggs)
                        _materials.AddReachable(ETechTypeCategory.Eggs, reachableDepth);

                    // If it is a central progression item, consider it unlocked.
                    if (_tree.DepthProgressionItems.ContainsKey(nextEntity.TechType) && !unlockedProgressionItems.ContainsKey(nextEntity.TechType))
                    {
                        unlockedProgressionItems.Add(nextEntity.TechType, true);
                        SpoilerLog.s_progression.Add(new KeyValuePair<TechType, int>(nextEntity.TechType, 0));
                        newProgressionItem = true;
                        LogHandler.Debug("[+] Added " + nextEntity.TechType.AsString() + " to progression items.");
                    }

                    nextEntity.InLogic = true;
                    LogHandler.Debug("[+] Randomised recipe for [" + nextEntity.TechType.AsString() + "].");
                }
                else
                {
                    LogHandler.Debug("--- Recipe [" + nextEntity.TechType.AsString() + "] did not fulfill requirements, skipping.");
                }
            }

            LogHandler.Info("Finished randomising within " + circuitbreaker + " cycles!");
        }

        // Randomise the blueprints found inside databoxes.
        internal List<Databox> RandomiseDataboxes(RecipeDictionary masterDict, List<Databox> databoxes)
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

        // This function changes the output of the metal salvage recipe by removing
        // the titanium one and replacing it with the new one.
        // As a minor caveat, the new recipe shows up at the bottom of the tree.
        internal static void ChangeScrapMetalResult(Recipe replacement)
        {
            if (replacement.TechType.Equals(TechType.Titanium))
                return;

            // This techdata was used as a futile and desparate attempt to get things
            // working. It acts just like a RandomiserRecipe would though.
            TechData td = new TechData();
            td.Ingredients = new List<Ingredient>();
            td.Ingredients.Add(new Ingredient(TechType.ScrapMetal, 1));
            td.craftAmount = 1;
            TechType yeet = TechType.GasPod;

            replacement.Ingredients = new List<RandomiserIngredient>();
            replacement.Ingredients.Add(new RandomiserIngredient(TechType.ScrapMetal, 1));
            replacement.CraftAmount = 4;

            //CraftDataHandler.SetTechData(replacement.TechType, replacement);
            CraftDataHandler.SetTechData(yeet, td);

            LogHandler.Debug("!!! TechType contained in replacement: " + replacement.TechType.AsString());
            foreach(RandomiserIngredient i in replacement.Ingredients)
            {
                LogHandler.Debug("!!! Ingredient: " + i.techType.AsString() + ", " + i.amount);
            }

            // FIXME for whatever reason, this code works for some items, but not for others????
            // Fish seem to work, and so does lead, but every other raw material does not?
            // What's worse, CC2 has no issues with this at all despite apparently doing nothing different???
            CraftTreeHandler.RemoveNode(CraftTree.Type.Fabricator, "Resources", "BasicMaterials", "Titanium");

            //CraftTreeHandler.AddCraftingNode(CraftTree.Type.Fabricator, replacement.TechType, "Resources", "BasicMaterials");
            CraftTreeHandler.AddCraftingNode(CraftTree.Type.Fabricator, yeet, "Resources", "BasicMaterials");

            CraftDataHandler.RemoveFromGroup(TechGroup.Resources, TechCategory.BasicMaterials, TechType.Titanium);
            CraftDataHandler.AddToGroup(TechGroup.Resources, TechCategory.BasicMaterials, yeet);
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
        private bool CheckRecipeForBlueprint(RecipeDictionary masterDict, List<Databox> databoxes, LogicEntity entity, int depth)
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

                fulfilled &= (masterDict.DictionaryInstance.ContainsKey(condition) || _materials.GetReachable().Exists(x => x.TechType.Equals(condition)));

                if (!fulfilled)
                    return false;
            }

            if (entity.Blueprint.UnlockDepth > depth)
            {
                fulfilled = false;
            }

            return fulfilled;
        }

        private static bool CheckRecipeForPrerequisites(RecipeDictionary masterDict, LogicEntity entity)
        {
            bool fulfilled = true;

            // The builder tool must always be randomised before any base pieces
            // ever become accessible.
            if (entity.Category.IsBasePiece() && !masterDict.DictionaryInstance.ContainsKey(TechType.Builder))
                return false;

            if (entity.Prerequisites == null)
                return true;

            foreach (TechType t in entity.Prerequisites)
            {
                fulfilled &= masterDict.DictionaryInstance.ContainsKey(t);
                if (!fulfilled)
                    break;
            }

            return fulfilled;
        }

        private bool ContainsAny(RecipeDictionary masterDict, TechType[] types)
        {
            foreach (TechType type in types)
            {
                if (masterDict.DictionaryInstance.ContainsKey(type))
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
        internal static void ApplyMasterDict(RecipeDictionary masterDict)
        {
            Dictionary<TechType, Recipe>.KeyCollection keys = masterDict.DictionaryInstance.Keys;

            foreach (TechType key in keys)
            {
                CraftDataHandler.SetTechData(key, masterDict.DictionaryInstance[key]);
            }

            // TODO Once scrap metal is working, un-commenting this will apply the
            // change on every startup.
            //ChangeScrapMetalResult(masterDict.DictionaryInstance[TechType.Titanium]);
        }

        // This function handles applying a randomised recipe to the in-game
        // craft data, and stores a copy in the master dictionary.
        internal static void ApplyRandomisedRecipe(RecipeDictionary masterDict, Recipe recipe)
        {
            CraftDataHandler.SetTechData(recipe.TechType, recipe);
            masterDict.Add(recipe.TechType, recipe);
        }
    }
}
