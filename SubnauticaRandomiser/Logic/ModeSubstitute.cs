using System.Collections.Generic;

namespace SubnauticaRandomiser.Logic
{
    internal class ModeSubstitute
    {
        private RecipeDictionary _masterDict;
        private RandomiserConfig _config;
        private Materials _materials = null;
        private System.Random _random = null;

        internal ModeSubstitute(RecipeDictionary masterDict, RandomiserConfig config)
        {
            _masterDict = masterDict;
            _config = config;
        }

        // This is the simplest way of randomisation. Merely take all materials
        // and substitute them with other materials of the same category and
        // depth difficulty.
        // As of right now, not actively used in the randomiser, but could make
        // for a neat setting at some point in the future.
        // In that case, make this use the general smart logic of randomising,
        // but fall back on substitution for choosing ingredients.
        internal void RandomSubstituteMaterials(RecipeDictionary masterDict, bool useFish, bool useSeeds)
        {
            List<RandomiserRecipe> randomRecipes = new List<RandomiserRecipe>();
            LogHandler.Info("Randomising using simple substitution...");

            randomRecipes = _materials.GetAll().FindAll(x => !x.Category.Equals(ETechTypeCategory.RawMaterials)
                                                    && !x.Category.Equals(ETechTypeCategory.Fish)
                                                    && !x.Category.Equals(ETechTypeCategory.Seeds)
                                                    );

            foreach (RandomiserRecipe randomiseMe in randomRecipes)
            {
                List<RandomiserIngredient> ingredients = randomiseMe.Ingredients;
                int depth = randomiseMe.Depth;
                LogHandler.Debug("Randomising recipe for " + randomiseMe.TechType.AsString());

                for (int i = 0; i < ingredients.Count; i++)
                {
                    LogHandler.Debug("  Found ingredient " + (ingredients[i].techType).AsString());

                    // Find the Recipe object that matches the TechType of the
                    // ingredient we aim to randomise. With the Recipe, we have
                    // access to much more complete data like the item's category.
                    RandomiserRecipe matchRecipe = _materials.GetAll().Find(x => x.TechType.Equals(ingredients[i].techType));

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
                            match = _materials.GetAll().FindAll(x => (x.Category.Equals(matchRecipe.Category) || x.Category.Equals(ETechTypeCategory.Fish) || x.Category.Equals(ETechTypeCategory.Seeds)) && x.Depth <= randomiseMe.Depth);
                        if (useFish && !useSeeds)
                            match = _materials.GetAll().FindAll(x => (x.Category.Equals(matchRecipe.Category) || x.Category.Equals(ETechTypeCategory.Fish)) && x.Depth <= randomiseMe.Depth);
                        if (!useFish && useSeeds)
                            match = _materials.GetAll().FindAll(x => (x.Category.Equals(matchRecipe.Category) || x.Category.Equals(ETechTypeCategory.Seeds)) && x.Depth <= randomiseMe.Depth);
                    }
                    else
                    {
                        match = _materials.GetAll().FindAll(x => x.Category.Equals(matchRecipe.Category) && x.Depth <= randomiseMe.Depth);
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

                RandomiserLogic.ApplyRandomisedRecipe(masterDict, randomiseMe);
            }
            LogHandler.Info("Finished randomising.");

            return;
        }
    }
}
