using System;
using System.Collections.Generic;
using SMLHelper.V2.Crafting;
using SMLHelper.V2.Handlers;

namespace SubnauticaRandomiser
{
    public class ProgressionManager
    {
        // public readonly int Seed;
        private readonly Random _random;

        // I was really debating making this a dictionary instead. I still made
        // this into a list since the searchability of _all is important,
        // and _reachable often gets iterated over anyway. Plus, lists have the
        // advantage of making it very easy to call up a random element.
        internal List<Recipe> _allMaterials;
        private List<Recipe> _reachableMaterials;
        private List<TechType> _depthProgressionItems;

        public ProgressionManager(List<Recipe> allMaterials)
        {
            _random = new Random();
            _allMaterials = allMaterials;
            _reachableMaterials = new List<Recipe>();
            _depthProgressionItems = new List<TechType>();
        }

        public bool AddMaterialsToReachableList(ETechTypeCategory category, int reachableDepth)
        {
            bool success = false;
            LogHandler.Debug("Updating list of reachable materials: "+category.ToString()+", "+reachableDepth);
            // This is a stupidly complicated expression. It uses a lambda to
            // compare the search parameters against all materials contained
            // in the _allMaterials master list.
            List<Recipe> additions = _allMaterials.FindAll(x => x.Category.Equals(category) && x.Depth <= reachableDepth);
            
            // Ensure no duplicates are added to the list. This loop *must* go
            // in reverse, otherwise the computer gets very unhappy.
            for (int i = additions.Count - 1; i >= 0; i--)
            {
                if (_reachableMaterials.Contains(additions[i]))
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

        public bool AddMaterialsToReachableList(params Recipe[] additions)
        {
            bool success = false;

            foreach (Recipe r in additions)
            {
                if (!_reachableMaterials.Contains(r))
                {
                    _reachableMaterials.Add(r);
                    success = true;
                    LogHandler.Debug("Updated list of reachable materials: "+r.TechType.AsString());
                }
            }

            return success;
        }

        public void RandomSubstituteMaterials(RecipeDictionary masterDict, bool useFish, bool useSeeds)
        {
            // This is the simplest way of randomisation. Merely take all materials
            // and substitute them with other materials of the same category and
            // depth difficulty.
            List<Recipe> randomRecipes = new List<Recipe>();
            LogHandler.Info("Randomising using simple substitution...");

            randomRecipes = _allMaterials.FindAll(x => !x.Category.Equals(ETechTypeCategory.RawMaterials)
                                                    && !x.Category.Equals(ETechTypeCategory.Fish)
                                                    && !x.Category.Equals(ETechTypeCategory.Seeds)
                                                    );

            foreach (Recipe randomiseMe in randomRecipes)
            {
                List<RandomiserIngredient> ingredients = randomiseMe.Ingredients;
                int depth = randomiseMe.Depth;
                LogHandler.Debug("Randomising recipe for " + randomiseMe.TechType.AsString());

                for (int i=0; i<ingredients.Count; i++)
                {
                    LogHandler.Debug("  Found ingredient " + ((TechType)ingredients[i].TechTypeInt).AsString());

                    // Find the Recipe object that matches the TechType of the
                    // ingredient we aim to randomise. With the Recipe, we have
                    // access to much more complete data like the item's category.
                    Recipe matchRecipe = _allMaterials.Find(x => x.TechType.Equals((TechType)ingredients[i].TechTypeInt));

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
                    List<Recipe> match = new List<Recipe>();
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
                            if (ingredients.Exists(x => x.TechTypeInt == (int)match[index].TechType))
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
                        randomiseMe.Ingredients[i].TechTypeInt = (int)match[index].TechType;
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

        public void RandomSmart(RecipeDictionary masterDict, RandomiserConfig config)
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
            _reachableMaterials = new List<Recipe>();
            ProgressionTree tree = new ProgressionTree();
            int reachableDepth = 0;

            tree.SetupVanillaTree();

            foreach (Recipe r in _allMaterials.FindAll(x => !x.Category.Equals(ETechTypeCategory.RawMaterials) 
                                                         && !x.Category.Equals(ETechTypeCategory.Fish) 
                                                         && !x.Category.Equals(ETechTypeCategory.Seeds)))
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

                int newDepth = 0;
                if (newProgressionItem)
                {
                    newDepth = CalculateReachableDepth(tree, unlockedProgressionItems);
                    newProgressionItem = false;
                }

                // If the most recently randomised item opened up some new paths
                // to progress, there's extra stuff to handle.
                if (newDepth > reachableDepth)
                {
                    reachableDepth = newDepth;

                    // Exclude creepvine and samples until a knife is obtained.
                    if (masterDict.DictionaryInstance.ContainsKey((int)TechType.Knife))
                    {
                        AddMaterialsToReachableList(ETechTypeCategory.RawMaterials, reachableDepth);
                    }
                    else
                    {
                        AddMaterialsToReachableList(_allMaterials.FindAll(x => x.Category.Equals(ETechTypeCategory.RawMaterials) && x.Prerequisites != null && !x.Prerequisites.Contains(TechType.Knife)).ToArray());
                    }

                    if (config.bUseFish)
                        AddMaterialsToReachableList(ETechTypeCategory.Fish, reachableDepth);
                    if (config.bUseSeeds && unlockedProgressionItems.ContainsKey(TechType.Knife))
                        AddMaterialsToReachableList(ETechTypeCategory.Seeds, reachableDepth);
                }

                // Grab a random recipe which has not yet been randomised.
                TechType nextType = GetRandom(toBeRandomised);
                Recipe nextRecipe = _allMaterials.Find(x => x.TechType.Equals(nextType));

                // Does this recipe have all of its prerequisites fulfilled?
                if (CheckRecipeForBlueprint(masterDict, nextRecipe, reachableDepth) && CheckRecipeForPrerequisites(masterDict, nextRecipe))
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

                    // If it is a central progression item, consider it unlocked.
                    if (tree.depthProgressionItems.ContainsKey(nextType) && !unlockedProgressionItems.ContainsKey(nextType))
                    {
                        unlockedProgressionItems.Add(nextType, true);
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

        // TODO: Make this more sophisticated. Value-based approach?
        public Recipe RandomiseIngredients(Recipe recipe, List<Recipe> materials, RandomiserConfig config)
        {
            List<RandomiserIngredient> ingredients = new List<RandomiserIngredient>();
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

                // Try at least one big ingredient first, then do smaller ones
                List<Recipe> candidates = materials.FindAll(x => (targetValue * (config.dIngredientRatio + 0.05)) > x.Value && x.Value > (targetValue * (config.dIngredientRatio - 0.05)));
                if (candidates.Count == 0)
                {
                    // If we had no luck, just pick a random one.
                    candidates.Add(GetRandom(materials));
                }

                Recipe big = GetRandom(candidates);
                ingredients.Add(new RandomiserIngredient((int)big.TechType, 1));
                currentValue += big.Value;
                recipe.Value += currentValue;

                LogHandler.Debug("    Adding big ingredient " + big.TechType.AsString());

                // Now fill up with random materials until the value threshold
                // is more or less met, as defined by fuzziness.
                while ((targetValue - currentValue) > (targetValue * config.dFuzziness / 2))
                {
                    Recipe r = GetRandom(materials);
                    // Prevent duplicates.
                    if (ingredients.Exists(x => x.TechTypeInt == (int)r.TechType))
                        continue;

                    // What's the maximum amount of this ingredient the recipe can
                    // still sustain?
                    int max = (int)((targetValue + targetValue * config.dFuzziness / 2) - currentValue) / r.Value;
                    max = max > 0 ? max : 1;
                    // Figure out how many, but no more than 5.
                    int amount = _random.Next(1, max);
                    amount = amount > 5 ? 5 : amount;

                    RandomiserIngredient ing = new RandomiserIngredient((int)r.TechType, amount);
                    ingredients.Add(ing);
                    currentValue += r.Value * amount;
                    recipe.Value += currentValue;

                    LogHandler.Debug("    Adding ingredient: " + r.TechType.AsString() + ", " + amount);
                }
                LogHandler.Debug("    Recipe is now valued "+currentValue+" out of "+targetValue);
            }

            // True Random does not care about you. It does just the bare minimum
            // to not softlock the player, but otherwise does whatever.
            if (config.iRandomiserMode == 1)
            {
                int number = _random.Next(1, 6);

                for (int i = 1; i <= number; i++)
                {
                    TechType type = GetRandom(materials).TechType;

                    // Prevent duplicates.
                    if (ingredients.Exists(x => x.TechTypeInt == (int)type))
                    {
                        i--;
                        continue;
                    }
                    RandomiserIngredient ing = new RandomiserIngredient((int)type, _random.Next(1, 6));

                    ingredients.Add(ing);
                }
            }

            recipe.Ingredients = ingredients;
            return recipe;
        }

        // Base pieces and vehicles obviously cannot act as ingredients for
        // recipes, so this function detects and filters them.
        public static bool CanFunctionAsIngredient(Recipe recipe)
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
        public static int CalculateReachableDepth(ProgressionTree tree, Dictionary<TechType, bool> progressionItems, int depthTime = 15)
        {
            double swimmingSpeed = 5.75;
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

            LogHandler.Debug("Recalculating reachable depth.");

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
            LogHandler.Debug("New reachable depth: " + totalDepth);

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

        private bool CheckRecipeForBlueprint(RecipeDictionary masterDict, Recipe recipe, int depth)
        {
            bool fulfilled = true;

            if (recipe.Blueprint == null || (recipe.Blueprint.UnlockConditions == null && recipe.Blueprint.UnlockDepth == 0))
                return true;

            foreach (TechType t in recipe.Blueprint.UnlockConditions)
            {
                fulfilled &= (masterDict.DictionaryInstance.ContainsKey((int)t) || _reachableMaterials.Exists(x => x.TechType.Equals(t)));

                if (!fulfilled)
                    break;
            }

            if (recipe.Blueprint.UnlockDepth > depth)
            {
                fulfilled = false;
            }

            return fulfilled;
        }

        private static bool CheckRecipeForPrerequisites(RecipeDictionary masterDict, Recipe recipe)
        {
            bool fulfilled = true;

            if (recipe.Prerequisites == null)
                return true;

            foreach (TechType t in recipe.Prerequisites)
            {
                fulfilled &= masterDict.DictionaryInstance.ContainsKey((int)t);
                if (!fulfilled)
                    break;
            }

            return fulfilled;
        }

        private Recipe GetRandom(List<Recipe> list)
        {
            if (list == null || list.Count == 0)
            {
                return null;
            }

            return list[_random.Next(0, list.Count)];
        }

        private TechType GetRandom(List<TechType> list)
        {
            if (list == null || list.Count == 0)
            {
                return TechType.None;
            }

            return list[_random.Next(0, list.Count)];
        }

        // Grab a collection of all keys in the dictionary, then use them to
        // apply every single one as a recipe change in the game.
        public static void ApplyMasterDict(RecipeDictionary masterDict)
        {
            Dictionary<int, Recipe>.KeyCollection keys = masterDict.DictionaryInstance.Keys;

            foreach (int key in keys)
            {
                CraftDataHandler.SetTechData((TechType)key, masterDict.DictionaryInstance[key]);
            }
        }

        // This function handles applying a randomised recipe to the in-game
        // craft data, and stores a copy in the master dictionary.
        public static void ApplyRandomisedRecipe(RecipeDictionary masterDict, Recipe recipe)
        {
            CraftDataHandler.SetTechData(recipe.TechType, recipe);
            masterDict.Add(recipe.TechType, recipe);
        }
    }
}
