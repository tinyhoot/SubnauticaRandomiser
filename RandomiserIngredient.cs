using System;
namespace SubnauticaRandomiser
{
    //Stupid hack.. but the Enum Serialization stuff is .NET 3.5+, and I think Subnautica isn't or something? There was a runtime error finding the dll.
    // This class makes sure TechType isn't serialized, instead an int is.
    [Serializable]
    public class RandomiserIngredient
    {
        public int TechType;
        public int Amount;

        public RandomiserIngredient(int techType, int amount)
        {
            TechType = techType;
            Amount = amount;
        }

        public RecipeIngredient ToIIngredient()
        {
            return new RecipeIngredient
            {
                techType = (TechType)TechType,
                amount = Amount
            };
        }
    }

    public class RecipeIngredient : IIngredient
    {
        public TechType techType { get; set; }

        public int amount { get; set; }
    }
}
