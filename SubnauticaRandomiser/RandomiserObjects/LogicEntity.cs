using System;
namespace SubnauticaRandomiser.RandomiserObjects
{
    public class LogicEntity
    {
        public readonly TechType TechType;
        public readonly Blueprint Blueprint;    // For making it show up in the PDA
        public RandomiserRecipe Recipe;         // For actually crafting it
        public SpawnData SpawnData;             // For spawning it naturally in the world
        public bool InLogic;                    // Is this available for randomising other entities?

        /* 
         * This class acts an abstract representation of anything that could or
         * should be considered while randomising.       
         * The Randomiser will pass over every one of these entities and only
         * consider itself done once each of them has the InLogic flag - meaning
         * that it is considered accessible within the game.
         */        

        public LogicEntity(TechType type, Blueprint blueprint = null, RandomiserRecipe recipe = null, SpawnData spawnData = null, bool inLogic = false)
        {
            TechType = type;
            Blueprint = blueprint;
            Recipe = recipe;
            SpawnData = spawnData;
            InLogic = inLogic;
        }
    }
}
