namespace SubnauticaRandomizer
{
    public class RecipeRandomizer
    {
        public static Recipes Randomize()
        {
            var recipes = new Recipes();
            recipes.RecipesByType[(int)TechType.Fins] = new Recipe
            {
                CraftAmount = 1,
                Ingredients = new System.Collections.Generic.List<GeneratedRecipeIngredient>()
                {
                    new GeneratedRecipeIngredient
                    {
                        TechType = (int)TechType.Quartz,
                        Amount = 2
                    }
                }
            };

            return recipes;
        }
    }
}
