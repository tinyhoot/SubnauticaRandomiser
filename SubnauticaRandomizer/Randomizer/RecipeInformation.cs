using FileHelpers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SubnauticaRandomizer.Randomizer
{
    public class RecipeInformation
    {
        public TechType Type;
        public List<TechType> RequiredIngredients;
        public int DepthDifficulty;
        public string Category;
        public List<TechType> RestrictedTools;
        public List<int> RandomizeDifficulty;
        public int? Quantity;

        private static TechType ParseType(string type)
        {
            try
            {
                var techType = (TechType)Enum.Parse(typeof(TechType), type);
                if (Enum.IsDefined(typeof(TechType), techType))
                {
                    return techType;
                }
            }
            catch { }

            return TechType.None;
        }

        private static IEnumerable<TechType> ParseTypes(string types)
        {
            foreach(var type in types.Split(','))
            {
                yield return ParseType(type);
            }
        }

        public static RecipeInformation FromCsv(RecipeFromCSV csv)
        {
            var techType = ParseType(csv.Type);

            if (techType == TechType.None)
            {
                return null;
            }

            var difficulties = string.IsNullOrEmpty(csv.RandomizeDifficulty) ? new List<int>() : csv.RandomizeDifficulty.Split(',').Select(s => s.Trim()).Select(Int32.Parse);

            try
            {
                return new RecipeInformation
                {
                    Type = techType,
                    RequiredIngredients = ParseTypes(csv.RequiredIngredients).Where(tt => tt != TechType.None).ToList(),
                    DepthDifficulty = csv.DepthDifficulty,
                    Category = csv.Category,
                    RestrictedTools = ParseTypes(csv.RestrictedTools).Where(tt => tt != TechType.None).ToList(),
                    RandomizeDifficulty = difficulties.ToList(),
                    Quantity = csv.Quantity
                };
            }
            catch { }

            return null;
        }

        public static IEnumerable<RecipeInformation> ParseFromCSV(string filename)
        {
            return new FileHelperEngine<RecipeFromCSV>().ReadFile(filename).Select(FromCsv).Where(ri => ri != null);
        }
    }

    [DelimitedRecord(","), IgnoreFirst(1)]
    public class RecipeFromCSV
    {
        [FieldQuoted(QuoteMode.OptionalForRead)]
        public string Type;
        [FieldQuoted(QuoteMode.OptionalForRead)]
        public string RequiredIngredients;
        public int DepthDifficulty;
        [FieldQuoted(QuoteMode.OptionalForRead)]
        public string Category;
        [FieldQuoted(QuoteMode.OptionalForRead)]
        public string RestrictedTools;
        [FieldQuoted(QuoteMode.OptionalForRead)]
        public string RandomizeDifficulty;
        public int? Quantity;
    }
}
