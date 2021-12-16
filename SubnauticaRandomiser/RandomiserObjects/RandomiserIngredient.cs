using System;
namespace SubnauticaRandomiser.RandomiserObjects
{
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
