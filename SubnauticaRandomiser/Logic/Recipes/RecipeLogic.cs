using System.Collections.Generic;
using System.Linq;
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
                  || (entity.CheckBlueprintFulfilled(_logic, reachableDepth) && entity.CheckPrerequisitesFulfilled(_logic))))
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
            if (essentialItems.Count > 0)
            {
                TechType type = essentialItems.Find(x => !_masterDict.RecipeDict.ContainsKey(x));
                if (!type.Equals(TechType.None))
                {
                    entity = _materials.Find(type);
                    LogHandler.Debug("Prioritising essential item " + entity + " for depth " + depth);
                }
            }

            // Similarly, if all essential items are done, grab one from among the elective items and leave the rest
            // up to chance.
            if (entity is null && electiveItems.Count > 0)
            {
                TechType[] types = electiveItems.Find(arr => arr.All(x => !_masterDict.RecipeDict.ContainsKey(x)));
                
                if (types?.Length > 0)
                {
                    TechType nextType = _logic.GetRandom(new List<TechType>(types));
                    entity = _materials.Find(nextType);
                    LogHandler.Debug("Prioritising elective item " + entity + " for depth " + depth);
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
        /// Apply a randomised recipe to the in-game craft data, and store a copy in the master dictionary.
        /// </summary>
        /// <param name="recipe">The recipe to change.</param>
        internal void ApplyRandomisedRecipe(Recipe recipe)
        {
            _masterDict.AddRecipe(recipe.TechType, recipe);
        }

        /// <summary>
        /// Apply all recipe changes stored in the masterDict to the game.
        /// </summary>
        /// <param name="masterDict">The master dictionary.</param>
        internal static void ApplyMasterDict(EntitySerializer masterDict)
        {
            Dictionary<TechType, Recipe>.KeyCollection keys = masterDict.RecipeDict.Keys;

            foreach (TechType key in keys)
            {
                CraftDataHandler.SetTechData(key, masterDict.RecipeDict[key]);
            }

            // TODO Once scrap metal is working, un-commenting this will apply the change on every startup.
            //ChangeScrapMetalResult(masterDict.DictionaryInstance[TechType.Titanium]);
        }
    }
}