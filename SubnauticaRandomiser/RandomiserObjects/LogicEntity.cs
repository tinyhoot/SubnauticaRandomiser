using System.Collections.Generic;

namespace SubnauticaRandomiser.RandomiserObjects
{
    /// <summary>
    /// This class acts an abstract representation of anything that could or should be considered while randomising.
    /// The Randomiser will pass over every one of these entities and only consider itself done once each of them has
    /// the InLogic flag - meaning that it is considered accessible within the game.
    /// </summary>
    public class LogicEntity
    {
        public readonly TechType TechType;
        public readonly ETechTypeCategory Category;
        public Blueprint Blueprint;             // For making it show up in the PDA
        public Recipe Recipe;                   // For actually crafting it
        public SpawnData SpawnData;             // For spawning it naturally in the world
        public List<TechType> Prerequisites;    // What is absolutely mandatory before getting this?
        public bool InLogic;                    // Is this available for randomising other entities?
        public int AccessibleDepth;             // How deep down must you reach to get to this?

        public int Value;                       // Rough value/rarity in relation to other entities
        public int MaxUsesPerGame;              // How often can this get used in recipes?
        internal int _usedInRecipes;            // How often did this get used in recipes?

        public bool HasPrerequisites => !(Prerequisites is null) && Prerequisites.Count > 0;
        public bool HasRecipe => !(Recipe is null);
        public bool HasSpawnData => !(SpawnData is null);
        public bool IsFragment => Category.Equals(ETechTypeCategory.Fragments);

        public LogicEntity(TechType type, ETechTypeCategory category, Blueprint blueprint = null, Recipe recipe = null, SpawnData spawnData = null, List<TechType> prerequisites = null, bool inLogic = false, int value = 0)
        {
            TechType = type;
            Category = category;
            Blueprint = blueprint;
            Recipe = recipe;
            SpawnData = spawnData;
            Prerequisites = prerequisites;
            InLogic = inLogic;
            AccessibleDepth = 0;

            Value = value;
            MaxUsesPerGame = 0;
            _usedInRecipes = 0;
        }
        
        /// <summary>
        /// Check whether this entity can act as an ingredient in crafting. Base pieces and vehicles are obviously
        /// excluded.
        /// </summary>
        /// <returns>True if it can act as an ingredient, false if not.</returns>
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
                                        ETechTypeCategory.Vehicles,
                                        ETechTypeCategory.Fragments};

            foreach (ETechTypeCategory cat in bad)
            {
                if (cat.Equals(Category))
                    return false;
            }

            return true;
        }
        
        /// <summary>
        /// Get the number of slots this entity occupies in an inventory.
        /// </summary>
        /// <returns>The number of slots, or 0 if the entity cannot exist in the inventory.</returns>
        public int GetItemSize()
        {
            int size = 0;

            size = CraftData.GetItemSize(TechType).x * CraftData.GetItemSize(TechType).y;

            return size;
        }
        
        /// <summary>
        /// Checks whether this entity can still be used in the recipe for a different entity,
        /// </summary>
        /// <returns>True if it can be used, false if not.</returns>
        public bool HasUsesLeft()
        {
            if (MaxUsesPerGame <= 0)
                return true;

            if (_usedInRecipes < MaxUsesPerGame)
                return true;

            return false;
        }

        public override string ToString()
        {
            return TechType.AsString();
        }
    }
}
