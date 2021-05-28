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

        public bool AddMaterialsToReachableList(ETechTypeCategory category, EProgressionNode node)
        {
            bool success = false;
            LogHandler.Debug("Updating list of reachable materials: "+category.ToString()+", "+node.ToString());
            // This is a stupidly complicated expression. It uses a lambda to
            // compare the search parameters against all materials contained
            // in the _allMaterials master list.
            List<Recipe> additions = _allMaterials.FindAll(x => x.Category.Equals(category) && x.Node.Equals(node));
            
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
                EProgressionNode node = randomiseMe.Node;
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
                            match = _allMaterials.FindAll(x => (x.Category.Equals(matchRecipe.Category) || x.Category.Equals(ETechTypeCategory.Fish) || x.Category.Equals(ETechTypeCategory.Seeds)) && x.Node <= randomiseMe.Node);
                        if (useFish && !useSeeds)
                            match = _allMaterials.FindAll(x => (x.Category.Equals(matchRecipe.Category) || x.Category.Equals(ETechTypeCategory.Fish)) && x.Node <= randomiseMe.Node);
                        if (!useFish && useSeeds)
                            match = _allMaterials.FindAll(x => (x.Category.Equals(matchRecipe.Category) || x.Category.Equals(ETechTypeCategory.Seeds)) && x.Node <= randomiseMe.Node);
                    }
                    else
                    {
                        match = _allMaterials.FindAll(x => x.Category.Equals(matchRecipe.Category) && x.Node <= randomiseMe.Node);
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

        public void RandomSmart()
        {
            // TODO
            // This function should use the progression tree to randomise materials
            // and game progression in an intelligent way.

            // Basic structure might look something like this:
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

        }

        // This function calculates the maximum reachable depth based on
        // what vehicles the player has attained, as well as how much
        // further they can go "on foot"
        public static int CalculateReachableDepth(ProgressionTree tree, List<TechType> progressionItems, int depthTime = 15)
        {
            double swimmingSpeed = 5.75;
            double seaglideSpeed = 11.0;
            bool seaglide = progressionItems.Contains(TechType.Seaglide);
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
                if (CheckListForAllTechTypes(progressionItems, path))
                    vehicleDepth = 200;
            }
            foreach (TechType[] path in tree.GetProgressionPath(EProgressionNode.Depth300m).Pathways)
            {
                if (CheckListForAllTechTypes(progressionItems, path))
                    vehicleDepth = 300;
            }
            foreach (TechType[] path in tree.GetProgressionPath(EProgressionNode.Depth500m).Pathways)
            {
                if (CheckListForAllTechTypes(progressionItems, path))
                    vehicleDepth = 500;
            }
            foreach (TechType[] path in tree.GetProgressionPath(EProgressionNode.Depth900m).Pathways)
            {
                if (CheckListForAllTechTypes(progressionItems, path))
                    vehicleDepth = 900;
            }
            foreach (TechType[] path in tree.GetProgressionPath(EProgressionNode.Depth1300m).Pathways)
            {
                if (CheckListForAllTechTypes(progressionItems, path))
                    vehicleDepth = 1300;
            }
            foreach (TechType[] path in tree.GetProgressionPath(EProgressionNode.Depth1700m).Pathways)
            {
                if (CheckListForAllTechTypes(progressionItems, path))
                    vehicleDepth = 1700;
            }

            if (progressionItems.Contains(TechType.Fins))
                finSpeed = 1.41;
            if (progressionItems.Contains(TechType.UltraGlideFins))
                finSpeed = 1.88;

            // How deep can the player go without any tanks?
            playerDepthRaw = (breathTime - searchTime) * (seaglide ? seaglideSpeed : (swimmingSpeed + finSpeed)) / 2;

            // But can they go deeper with a tank? (Yes.)
            if (progressionItems.Contains(TechType.Tank))
            {
                breathTime = 75;
                tankPenalty = 0.4;
                double depth = (breathTime - searchTime) * (seaglide ? seaglideSpeed : (swimmingSpeed + finSpeed - tankPenalty) / 2;
                playerDepthRaw = depth > playerDepthRaw ? depth : playerDepthRaw;
            }

            if (progressionItems.Contains(TechType.DoubleTank))
            {
                breathTime = 135;
                tankPenalty = 0.47;
                double depth = (breathTime - searchTime) * (seaglide ? seaglideSpeed : (swimmingSpeed + finSpeed - tankPenalty)) / 2;
                playerDepthRaw = depth > playerDepthRaw ? depth : playerDepthRaw;
            }

            if (progressionItems.Contains(TechType.HighCapacityTank))
            {
                breathTime = 225;
                tankPenalty = 0.6;
                double depth = (breathTime - searchTime) * (seaglide ? seaglideSpeed : (swimmingSpeed + finSpeed - tankPenalty)) / 2;
                playerDepthRaw = depth > playerDepthRaw ? depth : playerDepthRaw;
            }

            if (progressionItems.Contains(TechType.PlasteelTank))
            {
                breathTime = 135;
                tankPenalty = 0.1;
                double depth = (breathTime - searchTime) * (seaglide ? seaglideSpeed : (swimmingSpeed + finSpeed - tankPenalty)) / 2;
                playerDepthRaw = depth > playerDepthRaw ? depth : playerDepthRaw;
            }

            // The vehicle depth and whether or not the player has a rebreather
            // can modify the raw achievable diving depth.
            if (progressionItems.Contains(TechType.Rebreather))
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
