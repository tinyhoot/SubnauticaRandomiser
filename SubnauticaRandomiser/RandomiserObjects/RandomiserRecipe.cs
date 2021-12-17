using System.Collections.Generic;

namespace SubnauticaRandomiser.RandomiserObjects
{
    public class RandomiserRecipe : Recipe
    {
        public ETechTypeCategory Category;
        public int Depth;

        public RandomiserRecipe(TechType type, ETechTypeCategory category, int depth = 0, int value = 0, int maxUses = 0) : base(type)
        {
            Depth = depth;

            Category = category;
        }

        public Recipe GetSerializableRecipe()
        {
            return new Recipe(TechType, Ingredients, LinkedIngredients, CraftAmount);
        }

        // Base pieces and vehicles obviously cannot act as ingredients for
        // recipes, so this function detects and filters them.
        public bool CanFunctionAsIngredient()
        {
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
                if (cat.Equals(Category))
                    return false;
            }

            return true;
        }
    }
}