using System;
using System.Collections.Generic;
using SubnauticaRandomiser.Objects;

namespace SubnauticaRandomiser.Logic.LogicObjects
{
    /// <summary>
    /// Represents a craftable recipe. This entity is made specifically to check whether an item is <em>craftable</em>,
    /// i.e. whether all components are accessible. Responsibility for checking whether an item's craft node has been
    /// unlocked lies with <see cref="Blueprint"/>.
    /// </summary>
    internal class LogicRecipe : LogicEntity
    {
        /// <summary>
        /// The recipe that is registered into the game.
        /// </summary>
        public Recipe Recipe { get; private set; }
        
        /// <summary>
        /// The entity required to unlock the recipe in the PDA and make it available for crafting. Can be null, in
        /// which case the recipe is unlocked from the start. It may still lack its ingredients though!
        /// </summary>
        public LogicBlueprint Blueprint { get; private set; }

        public void CreateRecipe(IEnumerable<LogicEntity> ingredients, int craftAmount = 1)
        {
            Recipe = new Recipe(TechType);
            // Needs figuring out how exactly ingredients are done, and migrating of the Recipe class
            throw new NotImplementedException();
        }

        public void AddBlueprint(LogicBlueprint blueprint)
        {
            Blueprint = blueprint;
            Dependencies.Add(blueprint);
        }
    }
}