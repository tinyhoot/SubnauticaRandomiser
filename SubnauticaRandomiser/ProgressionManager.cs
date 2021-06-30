using System;
using System.Collections.Generic;
using SMLHelper.V2.Handlers;
using UnityEngine;

namespace SubnauticaRandomiser
{
    internal class ProgressionManager
    {
        private readonly System.Random _random;

        // I was really debating making this a dictionary instead. I still made
        // this into a list since the searchability of _all is important,
        // and _reachable often gets iterated over anyway. Plus, lists have the
        // advantage of making it very easy to call up a random element.
        internal List<RandomiserRecipe> _allMaterials;
        private List<RandomiserRecipe> _reachableMaterials;
        internal ProgressionTree _tree;
        private List<TechType> _depthProgressionItems;
        private List<Databox> _databoxes;
        private int _basicOutpostSize;
        private RandomiserRecipe _baseTheme;

        public ProgressionManager(List<RandomiserRecipe> allMaterials, List<Databox> databoxes = null, int seed = 0)
        {
            if (seed == 0)
                _random = new System.Random();
            else
                _random = new System.Random(seed);
            _allMaterials = allMaterials;
            _reachableMaterials = new List<RandomiserRecipe>();
            _depthProgressionItems = new List<TechType>();
            _databoxes = databoxes;
        }

        internal bool AddMaterialsToReachableList(ETechTypeCategory category, int reachableDepth)
        {
            bool success = false;
            LogHandler.Debug("Updating list of reachable materials: "+category.ToString()+", "+reachableDepth);
            // This is a stupidly complicated expression. It uses a lambda to
            // compare the search parameters against all materials contained
            // in the _allMaterials master list.
            List<RandomiserRecipe> additions = _allMaterials.FindAll(x => x.Category.Equals(category) && x.Depth <= reachableDepth);
            
            // Ensure no duplicates are added to the list. This loop *must* go
            // in reverse, otherwise the computer gets very unhappy.
            for (int i = additions.Count - 1; i >= 0; i--)
            {
                if (_reachableMaterials.Contains(additions[i]) || !CheckRecipeForUsesLeft(additions[i]))
                {
                    additions.RemoveAt(i);
                }
                else
                {
                    // LogHandler.Debug("Added " + additions[i].ItemType.AsString());
                }
            }
            if (additions.Count > 0)
            {
                _reachableMaterials.AddRange(additions);
                success = true;
            }

            return success;
        }

        internal bool AddMaterialsToReachableList(params RandomiserRecipe[] additions)
        {
            bool success = false;

            foreach (RandomiserRecipe r in additions)
            {
                if (!_reachableMaterials.Contains(r) && CheckRecipeForUsesLeft(r))
                {
                    _reachableMaterials.Add(r);
                    success = true;
                    LogHandler.Debug("Updated list of reachable materials: "+r.TechType.AsString());
                }
            }

            return success;
        }

        // Deprecated.
        internal void RandomSubstituteMaterials(RecipeDictionary masterDict, bool useFish, bool useSeeds)
        {
            // This is the simplest way of randomisation. Merely take all materials
            // and substitute them with other materials of the same category and
            // depth difficulty.
            List<RandomiserRecipe> randomRecipes = new List<RandomiserRecipe>();
            LogHandler.Info("Randomising using simple substitution...");

            randomRecipes = _allMaterials.FindAll(x => !x.Category.Equals(ETechTypeCategory.RawMaterials)
                                                    && !x.Category.Equals(ETechTypeCategory.Fish)
                                                    && !x.Category.Equals(ETechTypeCategory.Seeds)
                                                    );

            foreach (RandomiserRecipe randomiseMe in randomRecipes)
            {
                List<RandomiserIngredient> ingredients = randomiseMe.Ingredients;
                int depth = randomiseMe.Depth;
                LogHandler.Debug("Randomising recipe for " + randomiseMe.TechType.AsString());

                for (int i=0; i<ingredients.Count; i++)
                {
                    LogHandler.Debug("  Found ingredient " + (ingredients[i].techType).AsString());

                    // Find the Recipe object that matches the TechType of the
                    // ingredient we aim to randomise. With the Recipe, we have
                    // access to much more complete data like the item's category.
                    RandomiserRecipe matchRecipe = _allMaterials.Find(x => x.TechType.Equals(ingredients[i].techType));

                    if (randomiseMe.Prerequisites != null && randomiseMe.Prerequisites.Count > i)
                    {
                        // In vanilla Subnautica, in a recipe where something gets
                        // upgraded (commonly at the Modification Station), it is
                        // always in the very first position.
                        // Thus, we skip randomising in this case.
                        LogHandler.Debug("  Ingredient is a prerequisite, skipping.");
                        continue;
                    }

                    // Special handling for Fish and Seeds, which are treated as 
                    // raw materials if enabled in the config.
                    List<RandomiserRecipe> match = new List<RandomiserRecipe>();
                    if (matchRecipe.Category.Equals(ETechTypeCategory.RawMaterials) && (useFish || useSeeds))
                    {
                        if (useFish && useSeeds)
                            match = _allMaterials.FindAll(x => (x.Category.Equals(matchRecipe.Category) || x.Category.Equals(ETechTypeCategory.Fish) || x.Category.Equals(ETechTypeCategory.Seeds)) && x.Depth <= randomiseMe.Depth);
                        if (useFish && !useSeeds)
                            match = _allMaterials.FindAll(x => (x.Category.Equals(matchRecipe.Category) || x.Category.Equals(ETechTypeCategory.Fish)) && x.Depth <= randomiseMe.Depth);
                        if (!useFish && useSeeds)
                            match = _allMaterials.FindAll(x => (x.Category.Equals(matchRecipe.Category) || x.Category.Equals(ETechTypeCategory.Seeds)) && x.Depth <= randomiseMe.Depth);
                    }
                    else
                    {
                        match = _allMaterials.FindAll(x => x.Category.Equals(matchRecipe.Category) && x.Depth <= randomiseMe.Depth);
                    }
                    
                    if (match.Count > 0)
                    {
                        bool foundMatch = false;
                        int index = 0;

                        // Prevent duplicate ingredients
                        while (!foundMatch)
                        {
                            index = _random.Next(0, match.Count - 1);
                            if (ingredients.Exists(x => x.techType == match[index].TechType))
                            {
                                if (match.Count != 1)
                                    match.RemoveAt(index);
                                else
                                    break;
                            }
                            else
                            {
                                foundMatch = true;
                                break;
                            }
                        }
                        
                        LogHandler.Debug("  Replacing ingredient with " + match[index].TechType.AsString());
                        randomiseMe.Ingredients[i].techType = match[index].TechType;
                    }
                    else
                    {
                        LogHandler.Debug("  Found no matching replacements for " + ingredients[i]);
                    }
                }
                
                ApplyRandomisedRecipe(masterDict, randomiseMe);
            }
            LogHandler.Info("Finished randomising.");
            
            return;
        }

        internal void RandomSmart(RecipeDictionary masterDict, RandomiserConfig config)
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
            //   - Add it to _reachableMaterials
            //     - If it's a knife, also add all seeds and chunks
            //     - If it's not an item (like a base piece), skip this step
            //   - Add it to the master dictionary
            //   - Recalculate reachable depth
            //   - Repeat
            // - Once all items have been randomised, do an integrity check for
            //   safety. Rocket, vehicles, and hatching enzymes must be on the list.

            LogHandler.Info("Randomising using logic-based system...");

            List<TechType> toBeRandomised = new List<TechType>();
            Dictionary<TechType, bool> unlockedProgressionItems = new Dictionary<TechType, bool>();
            _reachableMaterials = new List<RandomiserRecipe>();
            _tree = new ProgressionTree();
            int reachableDepth = 0;

            _tree.SetupVanillaTree();
            if (config.bVanillaUpgradeChains)
                _tree.ApplyUpgradeChainToPrerequisites(_allMaterials);

            // If databox randomising is enabled, go and do that.
            if (config.bRandomiseDataboxes && _databoxes != null)
            {
                _databoxes = RandomiseDataboxes(masterDict, _databoxes);
            }

            // If base theming is enabled, choose a theming ingredient.
            if (config.bDoBaseTheming)
            {
                _baseTheme = ChooseBaseTheme(config, 100);
                ChangeScrapMetalResult(_baseTheme);
                LogHandler.Debug("Chosen " + _baseTheme.TechType.AsString() + " as base theme.");
            }

            foreach (RandomiserRecipe r in _allMaterials.FindAll(x => !x.Category.Equals(ETechTypeCategory.RawMaterials) 
                                                         && !x.Category.Equals(ETechTypeCategory.Fish) 
                                                         && !x.Category.Equals(ETechTypeCategory.Seeds)
                                                         && !x.Category.Equals(ETechTypeCategory.Eggs)))
            {
                toBeRandomised.Add(r.TechType);
            }

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

                TechType nextType = TechType.None;
                int newDepth = 0;
                if (newProgressionItem)
                {
                    newDepth = CalculateReachableDepth(_tree, unlockedProgressionItems, config.iDepthSearchTime);
                    if (SpoilerLog.s_progression.Count > 0)
                    {
                        KeyValuePair<TechType, int> valuePair = new KeyValuePair<TechType, int>(SpoilerLog.s_progression[SpoilerLog.s_progression.Count - 1].Key, newDepth);
                        SpoilerLog.s_progression.RemoveAt(SpoilerLog.s_progression.Count - 1);
                        SpoilerLog.s_progression.Add(valuePair);
                    }
                    newProgressionItem = false;
                }

                // If the most recently randomised item opened up some new paths
                // to progress, there's extra stuff to handle.
                if (newDepth > reachableDepth)
                {
                    reachableDepth = newDepth;

                    // Exclude creepvine and samples until a knife is obtained.
                    if (masterDict.DictionaryInstance.ContainsKey(TechType.Knife))
                    {
                        AddMaterialsToReachableList(ETechTypeCategory.RawMaterials, reachableDepth);
                    }
                    else
                    {
                        AddMaterialsToReachableList(_allMaterials.FindAll(x => x.Category.Equals(ETechTypeCategory.RawMaterials) 
                                                                            && x.Depth <= reachableDepth
                                                                            && x.Prerequisites != null
                                                                            && !x.Prerequisites.Contains(TechType.Knife)
                                                                            ).ToArray());
                    }

                    if (config.bUseFish)
                        AddMaterialsToReachableList(ETechTypeCategory.Fish, reachableDepth);
                    if (config.bUseSeeds && unlockedProgressionItems.ContainsKey(TechType.Knife))
                        AddMaterialsToReachableList(ETechTypeCategory.Seeds, reachableDepth);
                    if (config.bUseEggs && masterDict.DictionaryInstance.ContainsKey(TechType.BaseWaterPark))
                        AddMaterialsToReachableList(ETechTypeCategory.Eggs, reachableDepth);
                }

                // Make sure the list of absolutely essential items is done first,
                // for each depth level. This guarantees certain recipes are done
                // by a certain depth, e.g. waterparks by 500m.
                List<TechType> essentialItems = _tree.GetEssentialItems(reachableDepth);
                if (essentialItems != null)
                {
                    nextType = essentialItems[0];
                    essentialItems.RemoveAt(0);
                    LogHandler.Debug("Prioritising essential item " + nextType.AsString() + " for depth " + reachableDepth);

                    // If this has already been randomised, all the better.
                    if (masterDict.DictionaryInstance.ContainsKey(nextType))
                    {
                        nextType = TechType.None;
                        LogHandler.Debug("Priority item was already randomised, skipping.");
                    }
                }

                // Similarly, if all essential items are done, grab one from among
                // the elective items and leave the rest up to chance.
                List<TechType[]> electiveItems = _tree.GetElectiveItems(reachableDepth);
                if (nextType.Equals(TechType.None) && electiveItems != null && electiveItems.Count > 0)
                {
                    TechType[] electiveTypes = electiveItems[0];
                    electiveItems.RemoveAt(0);

                    if (ContainsAny(masterDict, electiveTypes))
                    {
                        LogHandler.Debug("Priority elective containing " + electiveTypes[0].AsString() + " was already randomised, skipping.");
                    }
                    else
                    {
                        nextType = electiveTypes[_random.Next(0, electiveTypes.Length)];
                        LogHandler.Debug("Prioritising elective item " + nextType.AsString() + " for depth " + reachableDepth);
                    }
                }

                // Once all essentials and electives are done, grab a random recipe 
                // which has not yet been randomised.
                if (nextType.Equals(TechType.None))
                    nextType = GetRandom(toBeRandomised);
                RandomiserRecipe nextRecipe = _allMaterials.Find(x => x.TechType.Equals(nextType));

                // Does this recipe have all of its prerequisites fulfilled?
                if (CheckRecipeForBlueprint(masterDict, _databoxes, nextRecipe, reachableDepth) && CheckRecipeForPrerequisites(masterDict, nextRecipe))
                {
                    // Found a good recipe! Randomise it.
                    nextRecipe = RandomiseIngredients(nextRecipe, _reachableMaterials, config);

                    // Make sure it's not an item that cannot be an ingredient.
                    if (CanFunctionAsIngredient(nextRecipe))
                        _reachableMaterials.Add(nextRecipe);
                    ApplyRandomisedRecipe(masterDict, nextRecipe);
                    toBeRandomised.Remove(nextType);
                    
                    // Handling knives as a special case.
                    if ((nextType.Equals(TechType.Knife) || nextType.Equals(TechType.HeatBlade)) && !unlockedProgressionItems.ContainsKey(TechType.Knife))
                    {
                        unlockedProgressionItems.Add(TechType.Knife, true);
                        newProgressionItem = true;
                        // Add raw materials like creepvine and mushroom samples.
                        AddMaterialsToReachableList(_allMaterials.FindAll(x => x.Category.Equals(ETechTypeCategory.RawMaterials) && x.Prerequisites != null && x.Prerequisites.Contains(TechType.Knife)).ToArray());
                        if (config.bUseSeeds)
                            AddMaterialsToReachableList(ETechTypeCategory.Seeds, reachableDepth);
                    }
                    // Similarly, Alien Containment is a special case for eggs.
                    if (nextType.Equals(TechType.BaseWaterPark) && config.bUseEggs)
                        AddMaterialsToReachableList(ETechTypeCategory.Eggs, reachableDepth);

                    // If it is a central progression item, consider it unlocked.
                    if (_tree.DepthProgressionItems.ContainsKey(nextType) && !unlockedProgressionItems.ContainsKey(nextType))
                    {
                        unlockedProgressionItems.Add(nextType, true);
                        SpoilerLog.s_progression.Add(new KeyValuePair<TechType, int>(nextType, 0));
                        newProgressionItem = true;
                        LogHandler.Debug("[+] Added " + nextType.AsString() + " to progression items.");
                    }

                    LogHandler.Debug("[+] Randomised recipe for [" + nextType.AsString() + "].");
                }
                else
                {
                    LogHandler.Debug("--- Recipe [" + nextType.AsString() + "] did not fulfill requirements, skipping.");
                }
            }

            LogHandler.Info("Finished randomising within " + circuitbreaker + " cycles!");
        }

        private RandomiserRecipe RandomiseIngredients(RandomiserRecipe recipe, List<RandomiserRecipe> materials, RandomiserConfig config)
        {
            List<RandomiserIngredient> ingredients = new List<RandomiserIngredient>();
            List<ETechTypeCategory> blacklist = new List<ETechTypeCategory>();

            // Respect config values on tools and upgrades as ingredients.
            if (config.iEquipmentAsIngredients == 0 || (config.iEquipmentAsIngredients == 1 && CanFunctionAsIngredient(recipe)))
                blacklist.Add(ETechTypeCategory.Equipment);
            if (config.iToolsAsIngredients == 0 || (config.iToolsAsIngredients == 1 && CanFunctionAsIngredient(recipe)))
                blacklist.Add(ETechTypeCategory.Tools);
            if (config.iUpgradesAsIngredients == 0 || (config.iUpgradesAsIngredients == 1 && CanFunctionAsIngredient(recipe)))
            {
                blacklist.Add(ETechTypeCategory.VehicleUpgrades);
                blacklist.Add(ETechTypeCategory.WorkBenchUpgrades);
            }

            LogHandler.Debug("Figuring out ingredients for " + recipe.TechType.AsString());
            
            // Casual mode should preserve sequential upgrade lines and be a bit
            // more directive with its recipes.
            if (config.iRandomiserMode == 0)
            {
                // TODO
                // For figuring out if the first ingredient needs to be based on
                // a sequential upgrade, a dictionary<upgrade, base> would be neat.
                // Put it into the progression tree I think?
            }

            // Default mode still wants to randomise, but stays within reason
            // using the rough value of each recipe defined in the CSV.
            if (config.iRandomiserMode == 0)
            {
                double targetValue = recipe.Value;
                int currentValue = 0;
                recipe.Value = 0;
                int totalSize = 0;

                // Try at least one big ingredient first, then do smaller ones.
                List<RandomiserRecipe> bigIngredientCandidates = materials.FindAll(x => (targetValue * (config.dIngredientRatio + 0.05)) > x.Value
                                                                           && (targetValue * (config.dIngredientRatio - 0.05)) < x.Value
                                                                           && !blacklist.Contains(x.Category)
                                                                           );

                // If we had no luck, just pick a random one.
                if (bigIngredientCandidates.Count == 0)
                {
                    bigIngredientCandidates.Add(GetRandom(materials, blacklist));
                }

                RandomiserRecipe bigIngredient = GetRandom(bigIngredientCandidates);
                // If base theming is enabled and this is a base piece, replace
                // the big ingredient with the theming ingredient.
                if (config.bDoBaseTheming && _baseTheme != null && recipe.Category.Equals(ETechTypeCategory.BaseBasePieces))
                {
                    bigIngredient = _baseTheme;
                }
                // If vanilla upgrade chains are set to be preserved, replace
                // this big ingredient with the base item.
                if (config.bVanillaUpgradeChains)
                {
                    TechType basicUpgrade = _tree.GetUpgradeChain(recipe.TechType);
                    if (!basicUpgrade.Equals(TechType.None))
                    {
                        bigIngredient = _allMaterials.Find(x => x.TechType.Equals(basicUpgrade));
                    }
                }

                AddIngredientWithMaxUsesCheck(materials, ingredients, bigIngredient, 1);
                currentValue += bigIngredient.Value;
                recipe.Value += currentValue;

                LogHandler.Debug("    Adding big ingredient " + bigIngredient.TechType.AsString());

                // Now fill up with random materials until the value threshold
                // is more or less met, as defined by fuzziness.
                // Converted to do-while since we want this to happen at least once.
                do
                {
                    RandomiserRecipe r = GetRandom(materials, blacklist);

                    // Prevent duplicates.
                    if (ingredients.Exists(x => x.techType == r.TechType))
                        continue;

                    // What's the maximum amount of this ingredient the recipe can
                    // still sustain?
                    int max = (int)((targetValue + targetValue * config.dFuzziness / 2) - currentValue) / r.Value;
                    max = max > 0 ? max : 1;
                    max = max > config.iMaxAmountPerIngredient ? config.iMaxAmountPerIngredient : max;
                    // Tools and upgrades do not stack, but if the recipe would
                    // require several and you have more than one in inventory,
                    // it will consume all of them.
                    if (r.Category.Equals(ETechTypeCategory.Tools) || r.Category.Equals(ETechTypeCategory.VehicleUpgrades) || r.Category.Equals(ETechTypeCategory.WorkBenchUpgrades))
                        max = 1;
                    // Never require more than one (default) egg. That's tedious.
                    if (r.Category.Equals(ETechTypeCategory.Eggs))
                        max = config.iMaxEggsAsSingleIngredient;

                    // Figure out how many, but no more than 5.
                    int amount = _random.Next(1, max + 1);
                    amount = amount > config.iMaxAmountPerIngredient ? config.iMaxAmountPerIngredient : amount;
                    // If a recipe starts requiring a lot of inventory space to
                    // complete, try to minimise adding more ingredients.
                    if (totalSize + (GetItemSize(r.TechType) * amount) > config.iMaxInventorySizePerRecipe)
                        amount = 1;

                    AddIngredientWithMaxUsesCheck(materials, ingredients, r, amount);
                    currentValue += r.Value * amount;
                    recipe.Value += currentValue;
                    totalSize += GetItemSize(r.TechType) * amount;

                    LogHandler.Debug("    Adding ingredient: " + r.TechType.AsString() + ", " + amount);

                    // If a recipe starts getting out of hand, shut it down early.
                    if (totalSize >= config.iMaxInventorySizePerRecipe)
                    {
                        LogHandler.Debug("!   Recipe is getting too large, stopping.");
                        break;
                    }
                    // Same thing for special case of outpost base parts
                    if (_tree.BasicOutpostPieces.ContainsKey(recipe.TechType) && _basicOutpostSize > config.iMaxBasicOutpostSize * 0.6)
                    {
                        LogHandler.Debug("!   Basic outpost size is getting too large, stopping.");
                        break;
                    }
                } while ((targetValue - currentValue) > (targetValue * config.dFuzziness / 2));

                if (_tree.BasicOutpostPieces.ContainsKey(recipe.TechType))
                    _basicOutpostSize += (totalSize * _tree.BasicOutpostPieces[recipe.TechType]);
                LogHandler.Debug("    Recipe is now valued "+currentValue+" out of "+targetValue);
            }

            // True Random does not care about you. It does just the bare minimum
            // to not softlock the player, but otherwise does whatever.
            if (config.iRandomiserMode == 1)
            {
                int number = _random.Next(1, 6);
                int totalInvSize = 0;

                for (int i = 1; i <= number; i++)
                {
                    RandomiserRecipe r = GetRandom(materials, blacklist);

                    // Prevent duplicates.
                    if (ingredients.Exists(x => x.techType == r.TechType))
                    {
                        i--;
                        continue;
                    }
                    RandomiserIngredient ing = new RandomiserIngredient(r.TechType, _random.Next(1, config.iMaxAmountPerIngredient + 1));

                    AddIngredientWithMaxUsesCheck(materials, ingredients, r, ing.amount);
                    totalInvSize += GetItemSize(ing.techType) * ing.amount;

                    LogHandler.Debug("    Adding ingredient: " + ing.techType.AsString() + ", " + ing.amount);

                    if (totalInvSize > config.iMaxInventorySizePerRecipe)
                    {
                        LogHandler.Debug("!   Recipe is getting too large, stopping.");
                        break;
                    }
                }
            }

            recipe.Ingredients = ingredients;
            recipe.CraftAmount = CraftDataHandler.GetTechData(recipe.TechType).craftAmount;
            return recipe;
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

        // Base pieces and vehicles obviously cannot act as ingredients for
        // recipes, so this function detects and filters them.
        internal static bool CanFunctionAsIngredient(RandomiserRecipe recipe)
        {
            bool isIngredient = true;

            ETechTypeCategory[] bad = { ETechTypeCategory.BaseBasePieces,
                                        ETechTypeCategory.BaseExternalModules,
                                        ETechTypeCategory.BaseGenerators,
                                        ETechTypeCategory.BaseInternalModules,
                                        ETechTypeCategory.BaseInternalPieces,
                                        ETechTypeCategory.Deployables,
                                        ETechTypeCategory.None,
                                        ETechTypeCategory.Rocket,
                                        ETechTypeCategory.Vehicles};

            foreach (ETechTypeCategory cat in bad)
            {
                if (cat.Equals(recipe.Category))
                {
                    isIngredient = false;
                    break;
                }
            }

            return isIngredient;
        }

        // This function calculates the maximum reachable depth based on
        // what vehicles the player has attained, as well as how much
        // further they can go "on foot"
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
        private void ChangeScrapMetalResult(RandomiserRecipe replacement)
        {
            if (replacement.TechType.Equals(TechType.Titanium))
                return;

            replacement.Ingredients = new List<RandomiserIngredient>();
            replacement.Ingredients.Add(new RandomiserIngredient(TechType.ScrapMetal, 1));
            replacement.CraftAmount = 4;

            CraftDataHandler.SetTechData(TechType.Titanium, _allMaterials.Find(x => x.TechType.Equals(TechType.Titanium)));
            CraftDataHandler.SetTechData(replacement.TechType, replacement);

            CraftTreeHandler.RemoveNode(CraftTree.Type.Fabricator, "Resources", "BasicMaterials", "Titanium");
            CraftTreeHandler.AddCraftingNode(CraftTree.Type.Fabricator, replacement.TechType, "Resources", "BasicMaterials");
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

        private static bool CheckListForAllTechTypes(List<TechType> list, TechType[] types)
        {
            bool allItemsPresent = true;

            foreach (TechType t in types)
            {
                allItemsPresent &= list.Contains(t);
                if (!allItemsPresent)
                    break;
            }

            return allItemsPresent;
        }

        // Check if this recipe fulfills all conditions to have its blueprint be unlocked
        private bool CheckRecipeForBlueprint(RecipeDictionary masterDict, List<Databox> databoxes, RandomiserRecipe recipe, int depth)
        {
            bool fulfilled = true;

            if (recipe.Blueprint == null || (recipe.Blueprint.UnlockConditions == null && recipe.Blueprint.UnlockDepth == 0))
                return true;

            // If the databox was randomised, do work to account for new locations.
            // Cyclops hull modules need extra special treatment.
            if (recipe.Blueprint.NeedsDatabox && databoxes != null && databoxes.Count > 0 && !recipe.TechType.Equals(TechType.CyclopsHullModule2) && !recipe.TechType.Equals(TechType.CyclopsHullModule3))
            {
                int total = 0;
                int number = 0;
                int lasercutter = 0;
                int propulsioncannon = 0;

                foreach (Databox box in databoxes.FindAll(x => x.TechType.Equals(recipe.TechType)))
                {
                    total += (int)Math.Abs(box.Coordinates.y);
                    number++;

                    if (box.RequiresLaserCutter)
                        lasercutter++;
                    if (box.RequiresPropulsionCannon)
                        propulsioncannon++;
                }

                LogHandler.Debug("[B] Found " + number + " databoxes for " + recipe.TechType.AsString());

                recipe.Blueprint.UnlockDepth = total / number;
                if (recipe.TechType.Equals(TechType.CyclopsHullModule1))
                {
                    _allMaterials.Find(x => x.TechType.Equals(TechType.CyclopsHullModule2)).Blueprint.UnlockDepth = total / number;
                    _allMaterials.Find(x => x.TechType.Equals(TechType.CyclopsHullModule3)).Blueprint.UnlockDepth = total / number;
                }

                // If more than half of all locations of this databox require a
                // tool to access the box, add it to the requirements for the recipe
                if (lasercutter / number >= 0.5)
                {
                    recipe.Blueprint.UnlockConditions.Add(TechType.LaserCutter);
                    if (recipe.TechType.Equals(TechType.CyclopsHullModule1))
                    {
                        _allMaterials.Find(x => x.TechType.Equals(TechType.CyclopsHullModule2)).Blueprint.UnlockConditions.Add(TechType.LaserCutter);
                        _allMaterials.Find(x => x.TechType.Equals(TechType.CyclopsHullModule3)).Blueprint.UnlockConditions.Add(TechType.LaserCutter);
                    }
                }

                if (propulsioncannon / number >= 0.5)
                {
                    recipe.Blueprint.UnlockConditions.Add(TechType.PropulsionCannon);
                    if (recipe.TechType.Equals(TechType.CyclopsHullModule1))
                    {
                        _allMaterials.Find(x => x.TechType.Equals(TechType.CyclopsHullModule2)).Blueprint.UnlockConditions.Add(TechType.PropulsionCannon);
                        _allMaterials.Find(x => x.TechType.Equals(TechType.CyclopsHullModule3)).Blueprint.UnlockConditions.Add(TechType.PropulsionCannon);
                    }
                }
            }

            foreach (TechType t in recipe.Blueprint.UnlockConditions)
            {
                // Without this piece, the Air bladder will hang if fish are not
                // enabled for the logic.
                // HACK does not work for custom items using e.g. eggs or seeds
                if (!InitMod.s_config.bUseFish && _allMaterials.Find(x => x.TechType.Equals(t)).Category.Equals(ETechTypeCategory.Fish))
                    continue;

                fulfilled &= (masterDict.DictionaryInstance.ContainsKey(t) || _reachableMaterials.Exists(x => x.TechType.Equals(t)));

                if (!fulfilled)
                    return false;
            }

            if (recipe.Blueprint.UnlockDepth > depth)
            {
                fulfilled = false;
            }

            return fulfilled;
        }

        private static bool CheckRecipeForUsesLeft(RandomiserRecipe recipe)
        {
            if (recipe.MaxUsesPerGame <= 0)
            {
                return true;
            }

            if (recipe._usedInRecipes < recipe.MaxUsesPerGame)
            {
                return true;
            }

            return false;
        }

        private static bool CheckRecipeForPrerequisites(RecipeDictionary masterDict, RandomiserRecipe recipe)
        {
            bool fulfilled = true;

            if (recipe.Prerequisites == null)
                return true;

            foreach (TechType t in recipe.Prerequisites)
            {
                fulfilled &= masterDict.DictionaryInstance.ContainsKey(t);
                if (!fulfilled)
                    break;
            }

            return fulfilled;
        }

        // Choose a theming ingredient for the base from among a range of options.
        private RandomiserRecipe ChooseBaseTheme(RandomiserConfig config, int depth)
        {
            List<RandomiserRecipe> options = new List<RandomiserRecipe>();

            options.AddRange(_allMaterials.FindAll(x => x.Category.Equals(ETechTypeCategory.RawMaterials)
                                                     && x.Depth < depth
                                                     && (x.Prerequisites == null || x.Prerequisites.Count == 0)
                                                     && x.MaxUsesPerGame == 0
                                                     && GetItemSize(x.TechType) == 1));

            if (config.bUseFish)
            {
                options.AddRange(_allMaterials.FindAll(x => x.Category.Equals(ETechTypeCategory.Fish)
                                                         && x.Depth < depth
                                                         && (x.Prerequisites == null || x.Prerequisites.Count == 0)
                                                         && x.MaxUsesPerGame == 0
                                                         && GetItemSize(x.TechType) == 1));
            }
            
            return GetRandom(options);
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

        private int GetItemSize(TechType type)
        {
            int size = 0;

            size = CraftData.GetItemSize(type).x * CraftData.GetItemSize(type).y;

            return size;
        }

        private RandomiserRecipe GetRandom(List<RandomiserRecipe> list, List<ETechTypeCategory> blacklist = null)
        {
            if (list == null || list.Count == 0)
            {
                throw new InvalidOperationException("Failed to get valid recipe from materials list: list is null or empty.");
            }

            RandomiserRecipe r = null;
            while (true)
            {
                r = list[_random.Next(0, list.Count)];

                if (blacklist != null && blacklist.Count > 0)
                {
                    if (blacklist.Contains(r.Category))
                        continue;
                }
                break;
            }

            return r;
        }

        private TechType GetRandom(List<TechType> list)
        {
            if (list == null || list.Count == 0)
            {
                return TechType.None;
            }

            return list[_random.Next(0, list.Count)];
        }

        // Add an ingredient to the list of ingredients used to form a recipe,
        // but ensure its MaxUses field is respected.
        private void AddIngredientWithMaxUsesCheck(List<RandomiserRecipe> materials, List<RandomiserIngredient> ingredients, RandomiserRecipe type, int amount)
        {
            ingredients.Add(new RandomiserIngredient(type.TechType, amount));
            type._usedInRecipes++;
            
            if (!CheckRecipeForUsesLeft(type))
            {
                materials.Remove(type);
                LogHandler.Debug("!  Removing " + type.TechType.AsString() + " from materials list due to max uses reached: " + type._usedInRecipes);
            }
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
        }

        // This function handles applying a randomised recipe to the in-game
        // craft data, and stores a copy in the master dictionary.
        internal static void ApplyRandomisedRecipe(RecipeDictionary masterDict, RandomiserRecipe recipe)
        {
            CraftDataHandler.SetTechData(recipe.TechType, recipe);
            masterDict.Add(recipe.TechType, recipe.GetSerializableRecipe());
        }
    }
}
