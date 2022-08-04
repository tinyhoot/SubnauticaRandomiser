using System;
namespace SubnauticaRandomiser.RandomiserObjects
{
    /// <summary>
    /// A wrapper for the game's Ingredient class to make it serializable.
    /// </summary>
    [Serializable]
    public class RandomiserIngredient : IIngredient
    {
        public TechType techType { get; set; }
        public int amount { get; set; }

        public RandomiserIngredient(TechType techType, int amount)
        {
            this.techType = techType;
            this.amount = amount;
        }
    }
}
