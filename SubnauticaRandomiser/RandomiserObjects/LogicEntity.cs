using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SubnauticaRandomiser.Logic;
using SubnauticaRandomiser.Logic.Recipes;

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
        public List<SpawnData> SpawnData;       // For spawning it naturally in the world
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

        public LogicEntity(TechType type, ETechTypeCategory category, Blueprint blueprint = null, Recipe recipe = null,
            List<SpawnData> spawnData = null, List<TechType> prerequisites = null, bool inLogic = false, int value = 0)
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
        /// Check if this recipe fulfills all conditions to have its blueprint be unlocked.
        /// </summary>
        /// <param name="logic">An instance of the core logic.</param>
        /// <param name="depth">The maximum depth to consider.</param>
        /// <returns>True if the recipe has no blueprint or fulfills all conditions, false otherwise.</returns>
        public bool CheckBlueprintFulfilled(CoreLogic logic, int depth)
        {
            if (Blueprint is null || (Blueprint.UnlockConditions is null && Blueprint.UnlockDepth == 0))
                return true;

            // If the databox was randomised, do work to account for new locations.
            if (logic._config.bRandomiseDataboxes && Blueprint.NeedsDatabox && !Blueprint.WasUpdated && logic._databoxes?.Count > 0)
                Blueprint.UpdateDataboxUnlocks(logic);

            foreach (TechType condition in Blueprint.UnlockConditions ?? Enumerable.Empty<TechType>())
            {
                LogicEntity conditionEntity = logic._materials.Find(condition);
                if (conditionEntity is null)
                    continue;

                // Without this piece, the Air bladder will hang if fish are not enabled for the logic, as it
                // fruitlessly searches for a bladderfish which never enters its algorithm.
                // Eggs and seeds are never problematic in vanilla, but are covered in case users add their own
                // modded items with those.
                if ((!logic._config.bUseFish && conditionEntity.Category.Equals(ETechTypeCategory.Fish))
                    || (!logic._config.bUseEggs && conditionEntity.Category.Equals(ETechTypeCategory.Eggs))
                    || (!logic._config.bUseSeeds && conditionEntity.Category.Equals(ETechTypeCategory.Seeds)))
                    continue;

                if (logic._masterDict.RecipeDict.ContainsKey(condition)
                    || logic._materials.GetReachable().Exists(x => x.TechType.Equals(condition)))
                    continue;
                
                return false;
            }

            // Ensure that necessary fragments have already been randomised.
            if (logic._config.bRandomiseFragments && Blueprint.Fragments?.Count > 0)
            {
                foreach (TechType fragment in Blueprint.Fragments)
                {
                    if (!logic._masterDict.SpawnDataDict.ContainsKey(fragment))
                    {
                        LogHandler.Debug("[B] Entity " + this + " missing fragment " + fragment.AsString());
                        return false;
                    }
                }

                return true;
            }

            return depth >= Blueprint.UnlockDepth;
        }
         
         /// <summary>
         /// Check whether all prerequisites for this recipe have already been randomised.
         /// </summary>
         /// <param name="logic">The core logic.</param>
         /// <returns>True if all conditions are fulfilled, false otherwise.</returns>
         public bool CheckPrerequisitesFulfilled(CoreLogic logic)
         {
             // The builder tool must always be randomised before any base pieces ever become accessible.
             if (Category.IsBasePiece() && !logic._masterDict.RecipeDict.ContainsKey(TechType.Builder))
                 return false;

             if (Prerequisites is null || Prerequisites.Count == 0)
                 return true;

             return Prerequisites.All(type => logic._masterDict.RecipeDict.ContainsKey(type));
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
