using System;
namespace SubnauticaRandomiser.RandomiserObjects
{
    public class LogicEntity
    {
        public readonly TechType TechType;
        public readonly ETechTypeCategory Category;
        public Blueprint Blueprint;             // For making it show up in the PDA
        public RandomiserRecipe Recipe;         // For actually crafting it
        public SpawnData SpawnData;             // For spawning it naturally in the world
        public bool InLogic;                    // Is this available for randomising other entities?
        public int AccessibleDepth;             // How deep down must you reach to get to this?

        public int Value;                       // Rough value/rarity in relation to other entities
        public int MaxUsesPerGame;              // How often can this get used in recipes?
        internal int _usedInRecipes;            // How often did this get used in recipes?

        /* 
         * This class acts an abstract representation of anything that could or
         * should be considered while randomising.       
         * The Randomiser will pass over every one of these entities and only
         * consider itself done once each of them has the InLogic flag - meaning
         * that it is considered accessible within the game.
         */

        public LogicEntity(TechType type, ETechTypeCategory category, Blueprint blueprint = null, RandomiserRecipe recipe = null, SpawnData spawnData = null, bool inLogic = false, int value = 0)
        {
            TechType = type;
            Category = category;
            Blueprint = blueprint;
            Recipe = recipe;
            SpawnData = spawnData;
            InLogic = inLogic;
            AccessibleDepth = 0;

            Value = value;
            MaxUsesPerGame = 0;
            _usedInRecipes = 0;
        }

        // Can this entity still be used in the recipe for a different entity?
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
