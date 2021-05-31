using System;
namespace SubnauticaRandomiser
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
