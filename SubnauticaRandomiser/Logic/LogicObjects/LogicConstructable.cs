namespace SubnauticaRandomiser.Logic.LogicObjects
{
    /// <summary>
    /// Represents buildable structures, vehicles, and rocket stages. Anything that has a recipe but *cannot* be held
    /// in the inventory.
    /// </summary>
    internal class LogicConstructable : LogicEntity
    {
        /// <summary>
        /// Buildables are generally not free and require ingredients to build.
        /// </summary>
        public LogicRecipe Recipe { get; private set; }
        
        public void AddRecipe(LogicRecipe recipe)
        {
            Recipe = recipe;
            Dependencies.Add(recipe);
        }
    }
}