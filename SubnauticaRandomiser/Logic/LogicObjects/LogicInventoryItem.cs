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