using System;
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
    }
}