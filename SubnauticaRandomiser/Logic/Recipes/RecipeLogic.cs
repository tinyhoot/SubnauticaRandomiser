using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using SMLHelper.V2.Handlers;
using SubnauticaRandomiser.RandomiserObjects;

namespace SubnauticaRandomiser.Logic.Recipes
{
    /// <summary>
    /// Handles everything related to randomising recipes.
    /// </summary>
    internal class RecipeLogic
    {
        private readonly CoreLogic _logic;
        private readonly Mode _mode;

        private RandomiserConfig _config => _logic._config;
        private EntitySerializer _masterDict => _logic._masterDict;
        private Materials _materials => _logic._materials;
        private ProgressionTree _tree => _logic._tree;

        public RecipeLogic(CoreLogic coreLogic)
        {
            _logic = coreLogic;
            _mode = null;
            
            // Init the mode that will be used.
            switch (_config.iRandomiserMode)
            {
                case (0):
                    _mode = new ModeBalanced(_logic);
                    break;
                case (1):
                    _mode = new ModeRandom(_logic);
                    break;
                default:
                    LogHandler.Error("Invalid recipe mode: " + _config.iRandomiserMode);
                    break;
            }
        }

        /// <summary>
        /// Handle everything related to actually randomising the recipe itself, and ensure all special cases are covered.
        /// </summary>
        /// <param name="entity">The recipe to randomise.</param>
        /// <param name="unlockedProgressionItems">The available materials to use as potential ingredients.</param>
        /// <param name="reachableDepth">The currently reachable depth.</param>
        /// <returns>True if the recipe was randomised, false otherwise.</returns>
        internal bool RandomiseRecipe(LogicEntity entity, Dictionary<TechType, bool> unlockedProgressionItems, int reachableDepth)
        {
            // Does this recipe have all of its prerequisites fulfilled? Skip this check if the recipe is a priority.
            if (!(_tree.IsPriorityEntity(entity)
                  || (CheckRecipeForBlueprint(entity, reachableDepth) && CheckRecipeForPrerequisites(entity))))
            {
                LogHandler.Debug("--- Recipe [" + entity.TechType.AsString() + "] did not fulfill requirements, skipping.");
                return false;
            }
            
            entity = _mode.RandomiseIngredients(entity);
            ApplyRandomisedRecipe(entity.Recipe);

            // Only add this entity to the materials list if it can be an ingredient.
            if (entity.CanFunctionAsIngredient())
                _materials.AddReachable(entity);

            // Knives are a special case that open up a lot of new materials.
            if ((entity.TechType.Equals(TechType.Knife) || entity.TechType.Equals(TechType.HeatBlade))
                && !unlockedProgressionItems.ContainsKey(TechType.Knife))
                unlockedProgressionItems.Add(TechType.Knife, true);

            // Similarly, Alien Containment is a special case for eggs.
            if (entity.TechType.Equals(TechType.BaseWaterPark) && _config.bUseEggs)
                unlockedProgressionItems.Add(TechType.BaseWaterPark, true);

            // If it is a central depth progression item, consider it unlocked.
            if (_tree.DepthProgressionItems.ContainsKey(entity.TechType) && !unlockedProgressionItems.ContainsKey(entity.TechType))
            {
                unlockedProgressionItems.Add(entity.TechType, true);
                _logic._spoilerLog.AddProgressionEntry(entity.TechType, 0);

                LogHandler.Debug("[+] Added " + entity.TechType.AsString() + " to progression items.");
            }

            entity.InLogic = true;
            LogHandler.Debug("[+] Randomised recipe for [" + entity.TechType.AsString() + "].");

            return true;
        }

        /// <summary>
        /// Get an essential or elective entity for the currently reachable depth, prioritising essential ones.
        /// </summary>
        /// <param name="depth">The maximum depth to consider.</param>
        /// <returns>A LogicEntity, or null if all have been processed already.</returns>
        [CanBeNull]
        internal LogicEntity GetPriorityEntity(int depth)
        {
            List<TechType> essentialItems = _tree.GetEssentialItems(depth);
            List<TechType[]> electiveItems = _tree.GetElectiveItems(depth);
            LogicEntity entity = null;

            // Always get one of the essential items first, if available.
            if (essentialItems != null && essentialItems.Count > 0)
            {
                entity = _materials.GetAll().Find(x => x.TechType.Equals(essentialItems[0]));
                essentialItems.RemoveAt(0);
                LogHandler.Debug("Prioritising essential item " + entity.TechType.AsString() + " for depth " + depth);

                // If this has already been randomised, all the better.
                if (_masterDict.RecipeDict.ContainsKey(entity.TechType))
                {
                    entity = null;
                    LogHandler.Debug("Priority item was already randomised, skipping.");
                }
            }

            // Similarly, if all essential items are done, grab one from among
            // the elective items and leave the rest up to chance.
            if (entity is null && electiveItems != null && electiveItems.Count > 0)
            {
                TechType[] electiveTypes = electiveItems[0];
                electiveItems.RemoveAt(0);

                if (_logic.ContainsAny(_masterDict, electiveTypes))
                {
                    LogHandler.Debug("Priority elective containing " + electiveTypes[0].AsString() + " was already randomised, skipping.");
                }
                else
                {
                    TechType nextType = _logic.GetRandom(new List<TechType>(electiveTypes));
                    entity = _materials.GetAll().Find(x => x.TechType.Equals(nextType));
                    LogHandler.Debug("Prioritising elective item " + entity.TechType.AsString() + " for depth " + depth);
                }
            }

            return entity;
        }
        
        /// <summary>
        /// Add all reachable materials to the list, taking into account depth and any config options.
        /// </summary>
        /// <param name="depth">The maximum depth to consider.</param>
        internal void UpdateReachableMaterials(int depth)
        {
            if (_masterDict.ContainsKnife())
                _materials.AddReachable(ETechTypeCategory.RawMaterials, depth);
            else
                _materials.AddReachableWithPrereqs(ETechTypeCategory.RawMaterials, depth, TechType.Knife, true);

            if (_config.bUseFish)
                _materials.AddReachable(ETechTypeCategory.Fish, depth);
            if (_config.bUseSeeds && _masterDict.ContainsKnife())
                _materials.AddReachable(ETechTypeCategory.Seeds, depth);
            if (_config.bUseEggs && _masterDict.RecipeDict.ContainsKey(TechType.BaseWaterPark))
                _materials.AddReachable(ETechTypeCategory.Eggs, depth);
        }

        /// <summary>
        /// Check if this recipe fulfills all conditions to have its blueprint be unlocked.
        /// </summary>
        /// <param name="masterDict">The master dictionary.</param>
        /// <param name="databoxes">The list of all databoxes.</param>
        /// <param name="entity">The recipe to check.</param>
        /// <param name="depth">The maximum depth to consider.</param>
        /// <returns>True if the recipe fulfills all conditions, false otherwise.</returns>
        private bool CheckRecipeForBlueprint(LogicEntity entity, int depth)
        {
            bool fulfilled = true;

            if (entity.Blueprint == null || (entity.Blueprint.UnlockConditions == null 
                                             && entity.Blueprint.UnlockDepth == 0))
                return true;

            // If the databox was randomised, do work to account for new locations.
            // Cyclops hull modules need extra special treatment.
            if (entity.Blueprint.NeedsDatabox && _logic._databoxes?.Count > 0 
                                              && !entity.TechType.Equals(TechType.CyclopsHullModule2) 
                                              && !entity.TechType.Equals(TechType.CyclopsHullModule3))
            {
                int total = 0;
                int number = 0;
                int lasercutter = 0;
                int propulsioncannon = 0;

                foreach (Databox box in _logic._databoxes.FindAll(x => x.TechType.Equals(entity.TechType)))
                {
                    total += (int)Math.Abs(box.Coordinates.y);
                    number++;

                    if (box.RequiresLaserCutter)
                        lasercutter++;
                    if (box.RequiresPropulsionCannon)
                        propulsioncannon++;
                }

                LogHandler.Debug("[B] Found " + number + " databoxes for " + entity.TechType.AsString());

                entity.Blueprint.UnlockDepth = total / number;
                if (entity.TechType.Equals(TechType.CyclopsHullModule1))
                {
                    _materials.GetAll().Find(x => x.TechType.Equals(TechType.CyclopsHullModule2))
                        .Blueprint.UnlockDepth = total / number;
                    _materials.GetAll().Find(x => x.TechType.Equals(TechType.CyclopsHullModule3))
                        .Blueprint.UnlockDepth = total / number;
                }

                // If more than half of all locations of this databox require a
                // tool to access the box, add it to the requirements for the recipe
                if (lasercutter / number >= 0.5)
                {
                    entity.Blueprint.UnlockConditions.Add(TechType.LaserCutter);
                    if (entity.TechType.Equals(TechType.CyclopsHullModule1))
                    {
                        _materials.GetAll().Find(x => x.TechType.Equals(TechType.CyclopsHullModule2))
                            .Blueprint.UnlockConditions.Add(TechType.LaserCutter);
                        _materials.GetAll().Find(x => x.TechType.Equals(TechType.CyclopsHullModule3))
                            .Blueprint.UnlockConditions.Add(TechType.LaserCutter);
                    }
                }

                if (propulsioncannon / number >= 0.5)
                {
                    entity.Blueprint.UnlockConditions.Add(TechType.PropulsionCannon);
                    if (entity.TechType.Equals(TechType.CyclopsHullModule1))
                    {
                        _materials.GetAll().Find(x => x.TechType.Equals(TechType.CyclopsHullModule2))
                            .Blueprint.UnlockConditions.Add(TechType.PropulsionCannon);
                        _materials.GetAll().Find(x => x.TechType.Equals(TechType.CyclopsHullModule3))
                            .Blueprint.UnlockConditions.Add(TechType.PropulsionCannon);
                    }
                }
            }

            foreach (TechType condition in entity.Blueprint.UnlockConditions)
            {
                LogicEntity conditionEntity = _materials.GetAll().Find(x => x.TechType.Equals(condition));

                // Without this piece, the Air bladder will hang if fish are not
                // enabled for the logic, as it fruitlessly searches for a bladderfish
                // which never enters its algorithm.
                // Eggs and seeds are never problematic in vanilla, but are covered
                // in case users add their own modded items with those.
                if (!_config.bUseFish && conditionEntity.Category.Equals(ETechTypeCategory.Fish))
                    continue;
                if (!_config.bUseEggs && conditionEntity.Category.Equals(ETechTypeCategory.Eggs))
                    continue;
                if (!_config.bUseSeeds && conditionEntity.Category.Equals(ETechTypeCategory.Seeds))
                    continue;

                fulfilled &= (_masterDict.RecipeDict.ContainsKey(condition) 
                              || _materials.GetReachable().Exists(x => x.TechType.Equals(condition)));

                if (!fulfilled)
                    return false;
            }

            // Ensure that necessary fragments have already been randomised.
            if (_config.bRandomiseFragments && entity.Blueprint.Fragments != null && entity.Blueprint.Fragments.Count > 0)
            {
                foreach (TechType fragment in entity.Blueprint.Fragments)
                {
                    if (!_masterDict.SpawnDataDict.ContainsKey(fragment))
                    {
                        LogHandler.Debug("[B] Entity " + entity.TechType.AsString() + " missing fragment " 
                                         + fragment.AsString());
                        return false;
                    }
                }
            }
            else if (entity.Blueprint.UnlockDepth > depth)
            {
                fulfilled = false;
            }

            return fulfilled;
        }

        /// <summary>
        /// Check whether all prerequisites for this recipe have already been randomised.
        /// </summary>
        /// <param name="entity">The recipe to check.</param>
        /// <returns>True if all conditions are fulfilled, false otherwise.</returns>
        private bool CheckRecipeForPrerequisites(LogicEntity entity)
        {
            bool fulfilled = true;

            // The builder tool must always be randomised before any base pieces
            // ever become accessible.
            if (entity.Category.IsBasePiece() && !_masterDict.RecipeDict.ContainsKey(TechType.Builder))
                return false;

            if (entity.Prerequisites == null)
                return true;

            foreach (TechType t in entity.Prerequisites)
            {
                fulfilled &= _masterDict.RecipeDict.ContainsKey(t);
                if (!fulfilled)
                    break;
            }

            return fulfilled;
        }

        /// <summary>
        /// Apply a randomised recipe to the in-game craft data, and store a copy in the master dictionary.
        /// </summary>
        /// <param name="recipe">The recipe to change.</param>
        internal void ApplyRandomisedRecipe(Recipe recipe)
        {
            CraftDataHandler.SetTechData(recipe.TechType, recipe);
            _masterDict.AddRecipe(recipe.TechType, recipe);
        }
    }
}