using System.Linq;
using System.IO;
using System.Collections.Generic;
using System;
using Oculus.Newtonsoft.Json;

namespace SubnauticaRandomizer.Randomizer
{
    public class RecipeRandomizer
    {
        private static Random RandomSeed = new Random();
        private static double ChancesOfUsingRawMaterial = 0.7;

        private List<RecipeInformation> _recipeInformation;
        private ILookup<int, RecipeInformation> _rawMaterials;
        private Dictionary<TechType, bool> _materialsThatHaveBeenUsed;
        private Recipes _recipes;

        private RecipeInformation PickRawMaterialAtRandom(int depthDifficulty)
        {
            var rawMaterial = _rawMaterials[depthDifficulty].PickRandom();

            _materialsThatHaveBeenUsed[rawMaterial.Type] = true;

            return rawMaterial;
        }

        private void DecideOnAnIngredient(RecipeInformation recipeInformation, TechType forType)
        {
            var existingIngredient = _recipes.RecipesByType[(int)forType].Ingredients.Where(i => i.TechType == (int)recipeInformation.Type).FirstOrDefault();

            if (existingIngredient != null)
            {
                existingIngredient.Amount++;
            }
            else
            {
                _recipes.RecipesByType[(int)forType].Ingredients.Add(new GeneratedRecipeIngredient
                {
                    Amount = 1,
                    TechType = (int)recipeInformation.Type
                });
            }
        }

        private void RandomizeMaterials()
        {
            foreach (var material in _recipeInformation.Where(ri => ri.Category == "Materials"))
            {
                _recipes.RecipesByType[(int)material.Type] = new Recipe
                {
                    CraftAmount = material.Quantity ?? 1,
                    Ingredients = new List<GeneratedRecipeIngredient>()
                };

                foreach(var materialPicked in material.RandomizeDifficulty.Select(PickRawMaterialAtRandom))
                {
                    DecideOnAnIngredient(materialPicked, material.Type);
                }
            }
        }

        private void RandomizeEquipmentAndTools()
        {
            foreach (var equipment in _recipeInformation.Where(ri => ri.Category == "Equipment" || ri.Category == "Tools" || ri.Category == "Buildings" || ri.Category == "Vehicles"))
            {
                _recipes.RecipesByType[(int)equipment.Type] = new Recipe
                {
                    CraftAmount = equipment.Quantity ?? 1,
                    Ingredients = equipment.RequiredIngredients.Select( ri => new GeneratedRecipeIngredient
                    {
                        Amount = 1,
                        TechType = (int)ri
                    }).ToList()
                };

                var randomizedDifficulties = new List<int>(equipment.RandomizeDifficulty);

                while (randomizedDifficulties.Any())
                {
                    if (RandomSeed.NextDouble() <= ChancesOfUsingRawMaterial)
                    {
                        var difficulty = randomizedDifficulties.First();
                        randomizedDifficulties.RemoveAt(0);
                        var materialPicked = PickRawMaterialAtRandom(difficulty);
                        DecideOnAnIngredient(materialPicked, equipment.Type);
                    }
                    else
                    {
                        foreach(var material in _recipeInformation.Where(ri => ri.Category == "Material"))
                        {
                            var materialCanBeUsed = material.RandomizeDifficulty.Intersect(randomizedDifficulties).Count() == material.RandomizeDifficulty.Count();

                            if (materialCanBeUsed)
                            {
                                foreach(var depthDifficulty in material.RandomizeDifficulty)
                                {
                                    randomizedDifficulties.Remove(depthDifficulty);
                                }
                                DecideOnAnIngredient(material, equipment.Type);
                                break;
                            }
                        }
                    }
                }
            }
        }

        public static Recipes Randomize(string modDirectory)
        {
            try
            {
                var recipeRandomizer = new RecipeRandomizer();
                var serializedInformation = JsonConvert.DeserializeObject<SerializedRecipesInformation>(File.ReadAllText(Path.Combine(modDirectory, "recipeinformation.json")));
                recipeRandomizer._recipeInformation = serializedInformation.Recipes.Select(sri => sri.ConvertTo()).ToList();
                recipeRandomizer._rawMaterials = recipeRandomizer._recipeInformation.Where(ri => ri.Category == "RawMaterials").ToLookup(ri => ri.DepthDifficulty, ri => ri);
                recipeRandomizer._materialsThatHaveBeenUsed = recipeRandomizer._recipeInformation.Where(ri => ri.Category == "RawMaterials" || ri.Category == "Materials").ToDictionary(ri => ri.Type, ri => false);
                recipeRandomizer._recipes = new Recipes();

                recipeRandomizer.RandomizeMaterials();
                recipeRandomizer.RandomizeEquipmentAndTools();

                return recipeRandomizer._recipes;
            }
            catch(Exception ex)
            {
                QPatch.LogError(ex.ToString());
                return new Recipes();
            }
        }
    }
}
