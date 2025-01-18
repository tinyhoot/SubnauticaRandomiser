using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using SubnauticaRandomiser.Interfaces;
using SubnauticaRandomiser.Objects;

namespace SubnauticaRandomiser.Logic.Modules.Recipes
{
    /// <summary>
    /// A mode for recipe randomisation with few to no checks in place. Unpredictable.
    /// </summary>
    internal class ModeRandom : Mode
    {
        internal ModeRandom(CoreLogic coreLogic, RecipeLogic recipeLogic, IRandomHandler rng) : base(coreLogic, recipeLogic, rng) { }
        
        protected override IEnumerable<(LogicEntity, int)> YieldRandomIngredients(LogicEntity entity,
            ReadOnlyCollection<RandomiserIngredient> ingredients, Func<TechType, bool> isDuplicate)
        {
            int number = _rng.Next(1, _config.MaxIngredientsPerRecipe.Value + 1, _distribution);

            for (int i = 1; i <= number; i++)
            {
                LogicEntity ingredientEntity = GetRandom(_recipeLogic.ValidIngredients);

                // Prevent duplicates.
                if (isDuplicate(ingredientEntity.TechType))
                {
                    i--;
                    continue;
                }

                int max = FindMaximum(ingredientEntity);
                yield return (ingredientEntity, _rng.Next(1, max + 1, _distribution));
            }
        }

        protected override int GetBaseThemeIngredientNumber(LogicEntity baseTheme)
        {
            return _rng.Next(1, FindMaximum(baseTheme) + 1, _distribution);
        }

        public override TechType GetScrapMetalReplacement()
        {
            var options = _entityHandler.GetAllRawMaterials();
            return _rng.Choice(options).TechType;
        }
    }
}
