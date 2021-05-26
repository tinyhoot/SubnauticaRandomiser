using System;
using System.Collections.Generic;
using SMLHelper.V2.Crafting;
using SMLHelper.V2.Handlers;

namespace SubnauticaRandomiser
{
    public class ProgressionManager
    {
        public readonly int Seed;
        private readonly Random _random;

        // I was really debating making this a dictionary instead. I still made
        // this into a list since the searchability of _all is important,
        // and _reachable often gets iterated over anyway. Plus, lists have the
        // advantage of making it very easy to call up a random element.
        internal List<Recipe> _allMaterials;
        private List<Recipe> _reachableMaterials;

        public ProgressionManager(int seed, List<Recipe> allMaterials)
        {
            Seed = seed;
            _random = new Random(seed);
            _allMaterials = allMaterials;
            _reachableMaterials = new List<Recipe>();
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

            // This is ugly as all hell, but it will do until the CSV is more complete.
            // Tools and Rocket both crash as they try to replace recipes they
            // cannot pull proper data on (Bladderfish + Shield Module)
            //randomRecipes = _allMaterials.FindAll(x => x.Category.Equals(ETechTypeCategory.BasicMaterials));
            //randomRecipes = _allMaterials.FindAll(x => x.Category.Equals(ETechTypeCategory.BasicMaterials) 
                                                    //|| x.Category.Equals(ETechTypeCategory.AdvancedMaterials)
                                                    //|| x.Category.Equals(ETechTypeCategory.Electronics) 
                                                    //|| x.Category.Equals(ETechTypeCategory.Deployables) 
                                                    //|| x.Category.Equals(ETechTypeCategory.Equipment)
                                                    //|| x.Category.Equals(ETechTypeCategory.Rocket)
                                                    //|| x.Category.Equals(ETechTypeCategory.Tablets)
                                                    //|| x.Category.Equals(ETechTypeCategory.Tools)
                                                    //|| x.Category.Equals(ETechTypeCategory.Vehicles)
                                                    //);

            randomRecipes = _allMaterials.FindAll(x => !x.Category.Equals(ETechTypeCategory.RawMaterials)
                                                    && !x.Category.Equals(ETechTypeCategory.Fish)
                                                    && !x.Category.Equals(ETechTypeCategory.Seeds)
                                                    );

            foreach (Recipe r in randomRecipes)
            {
                List<RandomiserIngredient> ingredients = r.Ingredients;
                EProgressionNode node = r.Node;
                LogHandler.Debug("Randomising recipe for " + r.TechType.AsString());

                for (int i=0; i<ingredients.Count; i++)
                {
                    LogHandler.Debug("  Found ingredient " + ((TechType)ingredients[i].TechTypeInt).AsString());

                    // Find the Recipe object that matches the TechType of the
                    // ingredient we aim to randomise. With the Recipe, we have
                    // access to much more complete data like the item's category.
                    Recipe matchRecipe = _allMaterials.Find(x => x.TechType.Equals((TechType)ingredients[i].TechTypeInt));

                    // Special handling for Fish and Seeds, which are treated as 
                    // raw materials if enabled in the config.
                    List<Recipe> match = new List<Recipe>();
                    if (matchRecipe.Category.Equals(ETechTypeCategory.RawMaterials) && (useFish || useSeeds))
                    {
                        if (useFish && useSeeds)
                            match = _allMaterials.FindAll(x => (x.Category.Equals(matchRecipe.Category) || x.Category.Equals(ETechTypeCategory.Fish) || x.Category.Equals(ETechTypeCategory.Seeds)) && x.Node <= r.Node);
                        if (useFish && !useSeeds)
                            match = _allMaterials.FindAll(x => (x.Category.Equals(matchRecipe.Category) || x.Category.Equals(ETechTypeCategory.Fish)) && x.Node <= r.Node);
                        if (!useFish && useSeeds)
                            match = _allMaterials.FindAll(x => (x.Category.Equals(matchRecipe.Category) || x.Category.Equals(ETechTypeCategory.Seeds)) && x.Node <= r.Node);
                    }
                    else
                    {
                        match = _allMaterials.FindAll(x => x.Category.Equals(matchRecipe.Category) && x.Node <= r.Node);
                    }

                    if (match.Count > 0)
                    {
                        int index = _random.Next(0, match.Count - 1);
                        r.Ingredients[i].TechTypeInt = (int)match[index].TechType;
                        LogHandler.Debug("  Replacing ingredient " + ((TechType)ingredients[i].TechTypeInt).AsString() + " with " + match[index].TechType.AsString());
                    }
                }

                ApplyRandomisedRecipe(masterDict, r);
            }
            LogHandler.Info("Finished randomising.");
            
            return;
        }

        public void RandomSmart()
        {
            // TODO
            // This function should use the progression tree to randomise materials
            // and game progression in an intelligent way.
        }

        public static void ApplyMasterDict(RecipeDictionary masterDict)
        {
            // Grab a collection of all keys in the dictionary, then use them to
            // apply every single one as a recipe change in the game.
            Dictionary<int, Recipe>.KeyCollection keys = masterDict.DictionaryInstance.Keys;

            foreach (int key in keys)
            {
                CraftDataHandler.SetTechData((TechType)key, masterDict.DictionaryInstance[key]);
            }
        }

        public static void ApplyRandomisedRecipe(RecipeDictionary masterDict, Recipe recipe)
        {
            // This function handles applying a randomised recipe to the in-game
            // craft data, and stores a copy in the master dictionary.
            CraftDataHandler.SetTechData(recipe.TechType, recipe);
            masterDict.Add(recipe.TechType, recipe);
        }
        




        public void RandomiseTest()
        {
            Random r = new Random(Seed);

            // A test run of how randomising recipes could look like based on
            // a seed. Of course you'd replace the hard coded lists with
            // lists you've extracted from a recipe info CSV.
            TechType[] testrecipes = { TechType.ComputerChip, TechType.Welder, TechType.Knife };

            foreach (TechType type in testrecipes)
            {
                Recipe replacementRecipe = new Recipe(type, ETechTypeCategory.Tools);
                replacementRecipe.CraftAmount = 1;

                int ingredientNumber = r.Next(1, 6);
                for (int i=1; i<=ingredientNumber; i++)
                {
                    replacementRecipe.Ingredients.Add(new RandomiserIngredient((int)_reachableMaterials[r.Next(0, _reachableMaterials.Count-1)].TechType, 1));
                }
                InitMod.s_masterDict.DictionaryInstance.Add((int)type, replacementRecipe);
                CraftDataHandler.SetTechData(type, replacementRecipe);
            }
        }
    }
}
