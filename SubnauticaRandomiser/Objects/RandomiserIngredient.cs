﻿using System;

namespace SubnauticaRandomiser.Objects
{
    /// <summary>
    /// A wrapper for the game's Ingredient class to make it serializable.
    /// </summary>
    [Serializable]
    public class RandomiserIngredient
    {
        public TechType techType { get; set; }
        public int amount { get; set; }

        public RandomiserIngredient(TechType techType, int amount)
        {
            this.techType = techType;
            this.amount = amount;
        }

        public Ingredient ToGameIngredient()
        {
            return new Ingredient(techType, amount);
        }
    }
}
