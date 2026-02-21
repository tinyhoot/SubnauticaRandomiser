namespace SubnauticaRandomiser.Logic.LogicObjects
{
    /// <summary>
    /// Represents an item that can be picked up and transported in an inventory.
    /// </summary>
    internal class LogicInventoryItem : LogicEntity
    {
        /// <summary>
        /// If the item has a recipe, it is craftable and can be made in one of the fabricators.
        /// </summary>
        public LogicRecipe Recipe { get; private set; }
        
        /// <summary>
        /// If the item has a spawnable, it can occur naturally out in the world.
        /// </summary>
        public LogicSpawnable Spawnable { get; private set; }

        /// <summary>
        /// The maximum number of recipes this item can be assigned to as an ingredient.
        /// </summary>
        public int MaxRecipeUses = -1;

        /// <summary>
        /// The number of times this item has been used in recipes during randomisation.
        /// </summary>
        public int TimesUsedInRecipes = 0;

        /// <summary>
        /// The item's value in relation to other items or recipes.
        /// </summary>
        public int Value = -1;

        public void AddRecipe(LogicRecipe recipe)
        {
            Recipe = recipe;
            Dependencies.Add(recipe);
        }

        public void AddSpawnable(LogicSpawnable spawnable)
        {
            Spawnable = spawnable;
            Dependencies.Add(spawnable);
        }
    }
}