using System.Collections.Generic;

namespace SubnauticaRandomiser
{
    public class RandomiserRecipe : Recipe
    {
        public ETechTypeCategory Category;
        public int Depth;
        public List<TechType> Prerequisites;
        public int Value;
        public int MaxUsesPerGame;
        internal int _usedInRecipes;
        public Blueprint Blueprint;

        public RandomiserRecipe(TechType type, ETechTypeCategory category, int depth = 0, List<TechType> prereqs = null, int value = 0, int maxUses = 0, Blueprint blueprint = null) : base(type)
        {
            Depth = depth;
            Value = value;
            MaxUsesPerGame = maxUses;
            _usedInRecipes = 0;

            Category = category;
            Prerequisites = prereqs;
            Blueprint = blueprint;
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

        public bool HasUsesLeft()
        {
            if (MaxUsesPerGame <= 0)
                return true;

            if (_usedInRecipes < MaxUsesPerGame)
                return true;

            return false;
        }
    }
}