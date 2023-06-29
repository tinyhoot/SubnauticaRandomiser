using System.Collections.Generic;
using System.Linq;
using SubnauticaRandomiser.Logic;
using SubnauticaRandomiser.Objects.Enums;

namespace SubnauticaRandomiser.Objects
{
    /// <summary>
    /// This class acts an abstract representation of anything that could or should be considered while randomising.
    /// The Randomiser will pass over every one of these entities and only consider itself done once each of them has
    /// the InLogic flag - meaning that it is considered accessible within the game.
    /// </summary>
    internal class LogicEntity
    {
        public readonly EntityType EntityType;
        public readonly TechType TechType;
        public readonly TechTypeCategory Category;
        public Blueprint Blueprint;             // For making it show up in the PDA
        public Recipe Recipe;                   // For actually crafting it
        public List<SpawnData> SpawnData;       // For spawning it naturally in the world
        public List<TechType> Prerequisites;    // What is absolutely mandatory before getting this?
        public bool InLogic;                    // Has this already been randomised?
        public int AccessibleDepth;             // How deep down must you reach to get to this?
        public int MaxUsesPerGame;              // How often can this get used in recipes?
        public int UsedInRecipes;               // How often did this get used in recipes?
        public int Value;                       // Rough value/rarity in relation to other entities

        public bool HasPrerequisites => Prerequisites?.Count > 0;
        public bool HasRecipe => !(Recipe is null);
        public bool HasSpawnData => !(SpawnData is null);

        public LogicEntity(EntityType entityType, TechType techType, TechTypeCategory category, Blueprint blueprint = null, Recipe recipe = null,
            List<SpawnData> spawnData = null, List<TechType> prerequisites = null, bool inLogic = false, int value = 0)
        {
            EntityType = entityType;
            TechType = techType;
            Category = category;
            Blueprint = blueprint;
            Recipe = recipe;
            SpawnData = spawnData;
            Prerequisites = prerequisites;
            InLogic = inLogic;
            AccessibleDepth = 0;
            MaxUsesPerGame = 0;
            UsedInRecipes = 0;
            Value = value;
        }

        /// <summary>
        /// Add the given TechType as a prerequisite for this entity.
        /// </summary>
        public void AddPrerequisite(TechType techType)
        {
            Prerequisites ??= new List<TechType>();
            if (!Prerequisites.Contains(techType))
                Prerequisites.Add(techType);
        }
        
        /// <summary>
        /// Check whether this entity can act as an ingredient in crafting. Base pieces and vehicles are obviously
        /// excluded.
        /// </summary>
        /// <returns>True if it can act as an ingredient, false if not.</returns>
        public bool CanFunctionAsIngredient()
        {
            return Category.IsIngredient();
        }

        /// <summary>
        /// Check whether this entity is ready for being randomised and entered into the logic.
        /// </summary>
        /// <param name="logic">An instance of the core logic.</param>
        /// <param name="depth">The maximum depth to consider.</param>
        public bool CheckReady(CoreLogic logic, int depth)
        {
            return CheckPrerequisitesFulfilled(logic) && CheckBlueprintFulfilled(logic, depth);
        }
        
         /// <summary>
        /// Check if this entity fulfills all conditions to have its blueprint be unlocked.
        /// </summary>
        /// <param name="logic">An instance of the core logic.</param>
        /// <param name="depth">The maximum depth to consider.</param>
        /// <returns>True if the entity has no blueprint or fulfills all conditions, false otherwise.</returns>
        public bool CheckBlueprintFulfilled(CoreLogic logic, int depth)
        {
            if (Blueprint is null || (Blueprint.UnlockConditions is null && Blueprint.UnlockDepth == 0))
                return true;

            foreach (TechType condition in Blueprint.UnlockConditions ?? Enumerable.Empty<TechType>())
            {
                LogicEntity conditionEntity = logic.EntityHandler.GetEntity(condition);
                if (conditionEntity is null)
                    continue;

                // Without this piece, the Air bladder will hang if fish are not enabled for the logic, as it
                // fruitlessly searches for a bladderfish which never enters its algorithm.
                // Eggs and seeds are never problematic in vanilla, but are covered in case users add their own
                // modded items with those.
                if ((!logic._Config.UseFish.Value && conditionEntity.Category.Equals(TechTypeCategory.Fish))
                    || (!logic._Config.UseEggs.Value && conditionEntity.Category.Equals(TechTypeCategory.Eggs))
                    || (!logic._Config.UseSeeds.Value && conditionEntity.Category.Equals(TechTypeCategory.Seeds)))
                    continue;

                if (logic.EntityHandler.IsInLogic(conditionEntity))
                    continue;
                
                return false;
            }

            // Ensure that necessary fragments have already been randomised.
            if (Blueprint.Fragments?.Count > 0)
            {
                bool fragmentsOkay = Blueprint.Fragments.All(f => logic.EntityHandler.IsInLogic(f));
                if (logic._Config.EnableFragmentModule.Value && logic._Config.RandomiseFragments.Value)
                    return fragmentsOkay;
                // If the fragment module is not enabled BUT fragments are all in logic anyway this is probably a
                // priority entity being randomised. In that case, skip all other checks.
                if (fragmentsOkay)
                    return true;
            }

            return depth >= Blueprint.UnlockDepth;
        }
         
         /// <summary>
         /// Check whether all prerequisites for this entity have already been randomised.
         /// </summary>
         /// <param name="logic">The core logic.</param>
         /// <returns>True if all conditions are fulfilled, false otherwise.</returns>
         public bool CheckPrerequisitesFulfilled(CoreLogic logic)
         {
             if (Prerequisites is null || Prerequisites.Count == 0)
                 return true;

             return Prerequisites.All(type => logic.EntityHandler.IsInLogic(type));
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

            if (UsedInRecipes < MaxUsesPerGame)
                return true;

            return false;
        }

        public override string ToString()
        {
            return TechType.AsString();
        }
    }
}
