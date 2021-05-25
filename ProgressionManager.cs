using System;
using System.Collections.Generic;
using SMLHelper.V2.Crafting;
using SMLHelper.V2.Handlers;

namespace SubnauticaRandomiser
{
    public class ProgressionManager
    {
        /* MAJOR UPGRADE SEQUENCE:
         * 100m from the start
         * 200m with the seaglide, fins, better tank, or seamoth
         * 300m with the seamoth upgrade 1
         * 500m with the seamoth upgrade 2, or the cyclops
         * 900m with the seamoth upgrade 3, cyclops upgrade 1 or prawn suit        
         * 1300m with cyclops upgrade 2 or prawn suit 1
         * 1700m with cyclops upgrade 3 or prawn suit 2
         * Escape with cyclops shield
         * 
         * Going forward, some items and technologies will likely have to be
         * defined as roadblocks of some sort - as in without at least one
         * of them unlocked, things will not progress.
         * It would make sense to define these items as recipes, their
         * components, and their unlock conditions.
         */

        public readonly int Seed;
        private readonly Random _random;
        internal List<Recipe> _allMaterials;
        private List<Recipe> _reachableMaterials;

        public ProgressionManager(int seed, List<Recipe> allMaterials)
        {
            Seed = seed;
            _random = new Random(seed);
            _allMaterials = allMaterials;
            _reachableMaterials = new List<Recipe>();
        }

        public bool AddMaterialsToReachableList(ETechTypeCategory category, int depth)
        {
            bool success = false;
            LogHandler.Debug("Updating list of reachable materials...");
            // This is a stupidly complicated expression. It uses a lambda to
            // compare the search parameters against all materials contained
            // in the _allMaterials master list.
            List<Recipe> additions = _allMaterials.FindAll(x => x.Category.Equals(category) && x.DepthDifficulty == depth);
            
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
                    LogHandler.Debug("Added " + additions[i].ItemType.AsString());
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
            LogHandler.Debug("Updating list of reachable materials...");

            foreach (Recipe r in additions)
            {
                if (!_reachableMaterials.Contains(r))
                {
                    _reachableMaterials.Add(r);
                    success = true;
                    LogHandler.Debug("Added " + r.ItemType.AsString());
                }
            }

            return success;
        }

        public void Randomise()
        {
            // TODO
        }

        public static void ApplyMasterList(RecipeDictionary masterList)
        {
            // Grab a collection of all keys in the dictionary, then use them to
            // apply every single one as a recipe change in the game.
            Dictionary<int, Recipe>.KeyCollection keys = masterList.DictionaryInstance.Keys;

            foreach (int key in keys)
            {
                CraftDataHandler.SetTechData((TechType)key, masterList.DictionaryInstance[key]);
            }
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
                    replacementRecipe.Ingredients.Add(new RandomiserIngredient((int)_reachableMaterials[r.Next(0, _reachableMaterials.Count-1)].ItemType, 1));
                }
                InitMod.s_randomisedRecipes.DictionaryInstance.Add((int)type, replacementRecipe);
                CraftDataHandler.SetTechData(type, replacementRecipe);
            }
        }
    }
}
