using System.Collections.Generic;

namespace SubnauticaRandomiser.RandomiserObjects
{
    public class RandomiserRecipe : Recipe
    {
        public ETechTypeCategory Category;
        public int Depth;
        public List<TechType> Prerequisites;

        public RandomiserRecipe(TechType type, ETechTypeCategory category, int depth = 0, List<TechType> prereqs = null, int value = 0, int maxUses = 0) : base(type)
        {
            Depth = depth;

            Category = category;
            Prerequisites = prereqs;
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

        public int GetItemSize()
        {
            int size = 0;

            size = CraftData.GetItemSize(TechType).x * CraftData.GetItemSize(TechType).y;

            return size;
        }
    }
}