using System;
using System.Collections.Generic;
using SMLHelper.V2.Crafting;
using SMLHelper.V2.Handlers;

namespace SubnauticaRandomiser
{
    public class RandomiserRecipe : Recipe
    {
        public ETechTypeCategory Category;
        public int Depth;
        public List<TechType> Prerequisites;
        public int Value;
        public int MaxUsesPerGame;
        public Blueprint Blueprint;

        public RandomiserRecipe(TechType type, ETechTypeCategory category, int depth = 0, List<TechType> prereqs = null, int value = 0, int maxUses = 0, Blueprint blueprint = null) : base(type)
        {
            Depth = depth;
            Value = value;
            MaxUsesPerGame = maxUses;

            Category = category;
            Prerequisites = prereqs;
            Blueprint = blueprint;
        }

        public Recipe GetBasicRecipe()
        {
            return new Recipe(TechType, Ingredients, LinkedIngredients, CraftAmount);
        }
    }
}