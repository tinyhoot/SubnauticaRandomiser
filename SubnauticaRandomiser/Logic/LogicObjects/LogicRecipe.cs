using HootLib.Interfaces;
using SubnauticaRandomiser.Objects;

namespace SubnauticaRandomiser.Logic.LogicObjects
{
    /// <summary>
    /// Represents a craftable recipe. This entity is made specifically to check whether an item is <em>craftable</em>,
    /// i.e. whether all components are accessible. Responsibility for checking whether an item's craft node has been
    /// unlocked lies with <see cref="LogicBlueprint"/>.
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

        /// <summary>
        /// The recipe's value in relation to other items or recipes.
        /// </summary>
        public int Value;

        public void LinkVanillaRecipe(EntityManager manager, ILogHandler log)
        {
            Recipe = new Recipe(TechType);
            Recipe.CopyVanillaData();
            
            // Link recipe ingredients from the vanilla game.
            // If any other mods have modified the recipes this will be reflected here too.
            foreach (var ingredient in Recipe.Ingredients)
            {
                var iitem = manager.Find<LogicInventoryItem>(ingredient.techType);
                if (iitem is null)
                {
                    log.Warn($"Tried to link {TechType.AsString()} recipe ingredient " +
                             $"{ingredient.techType.AsString()} but no such InventoryItem exists!");
                    continue;
                }

                Dependencies.Add(iitem);
            }
        }

        public void AddBlueprint(LogicBlueprint blueprint)
        {
            Blueprint = blueprint;
            Dependencies.Add(blueprint);
        }
    }
}