using System.Collections.Generic;
using SubnauticaRandomiser.Handlers;
using SubnauticaRandomiser.Objects;

namespace SubnauticaRandomiser.Serialization.Modules
{
    internal class RecipeSaveData : BaseModuleSaveData
    {
        public Dictionary<TechType, Recipe> RecipeDict = new Dictionary<TechType, Recipe>();
        public bool DiscoverEggs;
        public TechType ScrapMetalResult;
        
        public bool AddRecipe(TechType type, Recipe r)
        {
            if (RecipeDict.ContainsKey(type))
            {
                PrefixLogHandler.Get("[SaveData]").Warn($"Tried to add duplicate key {type.AsString()} to "
                                                        + $"Recipe master dictionary!");
                return false;
            }
            RecipeDict.Add(type, r);
            return true;
        }
    }
}