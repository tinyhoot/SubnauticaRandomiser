namespace SubnauticaRandomiser.Logic.LogicObjects
{
    /// <summary>
    /// Represents buildable structures, vehicles, and rocket stages. Anything that has a recipe but *cannot* be held
    /// in the inventory.
    /// </summary>
    internal class LogicBuildable : LogicEntity
    {
        /// <summary>
        /// If the buildable has a blueprint it cannot be accessed without being unlocked first (e.g. through scanning).
        /// </summary>
        public LogicBlueprint Blueprint { get; private set; }
        
        /// <summary>
        /// Buildables are generally not free and require ingredients to build.
        /// </summary>
        public LogicRecipe Recipe { get; private set; }

        public void AddBlueprint(LogicBlueprint blueprint)
        {
            Blueprint = blueprint;
            Dependencies.Add(blueprint);
        }
        
        public void AddRecipe(LogicRecipe recipe)
        {
            Recipe = recipe;
            Dependencies.Add(recipe);
        }
    }
}