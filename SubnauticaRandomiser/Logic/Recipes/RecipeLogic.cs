using System.Collections.Generic;
using SMLHelper.V2.Handlers;
using SubnauticaRandomiser.Interfaces;
using SubnauticaRandomiser.Objects;
using SubnauticaRandomiser.Objects.Enums;

namespace SubnauticaRandomiser.Logic.Recipes
{
    /// <summary>
    /// Handles everything related to randomising recipes.
    /// </summary>
    internal class RecipeLogic
    {
        private readonly CoreLogic _logic;
        private readonly Mode _mode;

        private RandomiserConfig _config => _logic._Config;
        private ILogHandler _log => _logic._Log;
        private EntitySerializer _serializer => _logic._Serializer;
        private Materials _materials => _logic._Materials;
        private ProgressionTree _tree => _logic._Tree;

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
                    _log.Error("Invalid recipe mode: " + _config.iRandomiserMode);
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
            if (!(_tree.IsPriorityEntity(entity, reachableDepth)
                  || (entity.CheckBlueprintFulfilled(_logic, reachableDepth) && entity.CheckPrerequisitesFulfilled(_logic))))
            {
                _log.Debug($"[R] --- Recipe [{entity}] did not fulfill requirements, skipping.");
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
            
            // If fragment randomisation is enabled, laser cutters open up new options there.
            if (entity.TechType.Equals(TechType.LaserCutter))
                _logic._fragmentLogic?.AddLaserCutterBiomes();

            // If it is a central depth progression item, consider it unlocked.
            if (_tree.DepthProgressionItems.ContainsKey(entity.TechType) && !unlockedProgressionItems.ContainsKey(entity.TechType))
            {
                unlockedProgressionItems.Add(entity.TechType, true);
                _logic._SpoilerLog.AddProgressionEntry(entity.TechType, 0);

                _log.Debug($"[R][+] Added {entity} to progression items.");
            }

            entity.InLogic = true;
            _log.Debug($"[R][+] Randomised recipe for [{entity}].");

            return true;
        }

        /// <summary>
        /// Add all reachable materials to the list, taking into account depth and any config options.
        /// </summary>
        /// <param name="depth">The maximum depth to consider.</param>
        internal void UpdateReachableMaterials(int depth)
        {
            if (_serializer.ContainsKnife())
                _materials.AddReachable(ETechTypeCategory.RawMaterials, depth);
            else
                _materials.AddReachableWithPrereqs(ETechTypeCategory.RawMaterials, depth, TechType.Knife, true);

            if (_config.bUseFish)
                _materials.AddReachable(ETechTypeCategory.Fish, depth);
            if (_config.bUseSeeds && _serializer.ContainsKnife())
                _materials.AddReachable(ETechTypeCategory.Seeds, depth);
            if (_config.bUseEggs && _serializer.RecipeDict.ContainsKey(TechType.BaseWaterPark))
                _materials.AddReachable(ETechTypeCategory.Eggs, depth);
        }

        /// <summary>
        /// Apply a randomised recipe to the in-game craft data, and store a copy in the master dictionary.
        /// </summary>
        /// <param name="recipe">The recipe to change.</param>
        internal void ApplyRandomisedRecipe(Recipe recipe)
        {
            _serializer.AddRecipe(recipe.TechType, recipe);
        }

        /// <summary>
        /// Apply all recipe changes stored in the masterDict to the game.
        /// </summary>
        /// <param name="serializer">The master dictionary.</param>
        internal static void ApplyMasterDict(EntitySerializer serializer)
        {
            Dictionary<TechType, Recipe>.KeyCollection keys = serializer.RecipeDict.Keys;

            foreach (TechType key in keys)
            {
                CraftDataHandler.SetTechData(key, serializer.RecipeDict[key]);
            }

            // TODO Once scrap metal is working, un-commenting this will apply the change on every startup.
            //ChangeScrapMetalResult(masterDict.DictionaryInstance[TechType.Titanium]);
        }
    }
}