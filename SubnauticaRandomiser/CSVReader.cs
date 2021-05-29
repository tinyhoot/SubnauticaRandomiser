using System;
using System.Collections.Generic;
using System.IO;
using SMLHelper.V2.Crafting;

namespace SubnauticaRandomiser
{
    public static class CSVReader
    {
        private static string[] s_csvLines;
        internal static List<Recipe> s_csvParsedList;

        private static readonly int s_expectedColumns = 8;

        internal static List<Recipe> ParseFile(string fileName)
        {
            // First, try to find and grab the file containing recipe information
            string path = InitMod.s_modDirectory + "\\" + fileName;
            LogHandler.Debug("Looking for recipe CSV as " + path);

            try
            {
                s_csvLines = File.ReadAllLines(path);
            }
            catch (Exception ex)
            {
                LogHandler.MainMenuMessage("Failed to read recipe CSV!");
                LogHandler.Error(ex.Message);
                return null;
            }

            // Second, read each line and try to parse that into a list of
            // RandomiserRecipe objects, for later use.
            // For now, this system is not robust and expects the CSV to be read
            // pretty much as it was distributed with the mod.
            s_csvParsedList = new List<Recipe>();

            foreach (string line in s_csvLines)
            {
                if (line.StartsWith("TechType", StringComparison.InvariantCulture))
                {
                    // This is the header line. Skip.
                    continue;
                }

                // This might very well fail if the user messed with the CSV
                try
                {
                    s_csvParsedList.Add(ParseLine(line));
                }
                catch (Exception ex)
                {
                    LogHandler.Error("Failed to parse information from CSV!");
                    LogHandler.Error(ex.Message);
                }
            }

            return s_csvParsedList;
        }

        // Parse one line of a CSV file and attempt to create a RandomiserRecipe
        private static Recipe ParseLine(string line)
        {
            Recipe recipe = null;

            TechType type = TechType.None;
            List<Ingredient> ingredientList = new List<Ingredient>();
            ETechTypeCategory category = ETechTypeCategory.None;
            int depth = 0;
            List<TechType> prereqList = new List<TechType>();
            int value = 0;

            Blueprint blueprint = null;
            List<TechType> blueprintUnlockConditions = new List<TechType>();
            TechType blueprintFragment = TechType.None;
            bool blueprintDatabox = false;
            int blueprintUnlockDepth = 0;

            string[] cells = line.Split(',');

            if (cells.Length != s_expectedColumns)
            {
                throw new InvalidDataException("Unexpected number of columns: " + cells.Length);
            }
            
            // Now to convert the data in each cell to an object we can use
            // Column 1: TechType
            type = StringToTechType(cells[0]);

            // Column 2: Ingredients
            // These need some special attention as they represent a complex object
            // Disabled for now, as it seems this information could just be pulled
            // from the game instead. Whoops.
            //if (!String.IsNullOrEmpty(cells[1]))
            //{
            //    string[] ingredients = cells[1].Split(';', ':');

            //    if (ingredients.Length % 2 != 0)
            //    {
            //        throw new InvalidDataException("Unexpected data in Ingredients field: "+ingredients);
            //    }

            //    for (int i = 0; i < ingredients.Length-1; i = i + 2)
            //    {
            //        TechType t = StringToTechType(ingredients[i]);
            //        int amount = int.Parse(ingredients[i + 1]);

            //        Ingredient ing = new Ingredient(t, amount);
            //        ingredientList.Add(ing);
            //        // LogHandler.Debug(type+": "+ing.techType.AsString()+":"+ing.amount);
            //    }
            //}

            // Column 3: Category
            category = StringToETechTypeCategory(cells[2]);

            // Column 4: Depth Difficulty
            if (!String.IsNullOrEmpty(cells[3]))
            {
                depth = int.Parse(cells[3]);
            }

            // Column 5: Prerequisites
            if (!String.IsNullOrEmpty(cells[4]))
            {
                prereqList = ProcessMultipleTechTypes(cells[4].Split(';'));
            }

            // Column 6: Craft Amount
            if (!String.IsNullOrEmpty(cells[5]))
            {
                value = int.Parse(cells[5]);
            }

            // Column 7: Blueprint Unlock Conditions
            if (!String.IsNullOrEmpty(cells[6]))
            {
                //blueprintUnlockConditions = ProcessMultipleTechTypes(cells[6].Split(';'));
                string[] conditions = cells[6].Split(';');

                foreach (string str in conditions)
                {
                    if (str.ToLower().Contains("fragment"))
                    {
                        // HACK This code as-is will not handle the Cyclops properly
                        // but I feel like that one needs special care anyways.
                        blueprintFragment = StringToTechType(str);
                    } 
                    else if (str.ToLower().Contains("databox"))
                    {
                        blueprintDatabox = true;
                    }
                    else
                    {
                        blueprintUnlockConditions.Add(StringToTechType(str));
                    }
                }
            }

            // Column 8: Blueprint Unlock Depth
            if (!String.IsNullOrEmpty(cells[7]))
            {
                blueprintUnlockDepth = int.Parse(cells[7]);
            }
            
            // Only if any of the blueprint components yielded anything,
            // ship the recipe with a blueprint.
            if (blueprintUnlockConditions != null || blueprintUnlockDepth != 0 || !blueprintDatabox || !blueprintFragment.Equals(TechType.None))
            {
                blueprint = new Blueprint(type, blueprintUnlockConditions, blueprintFragment, blueprintDatabox, blueprintUnlockDepth);
            }
            
            LogHandler.Debug("Registering recipe: " + type.AsString() +" "+ category.ToString() +" "+ depth +" ... "+ value);
            recipe = new Recipe(type, category, depth, prereqList, value, blueprint);
            return recipe;
        }

        internal static List<TechType> ProcessMultipleTechTypes(string[] str)
        {
            List<TechType> output = new List<TechType>();

            foreach (string s in str)
            {
                if (!String.IsNullOrEmpty(s))
                {
                    TechType t = StringToTechType(s);
                    output.Add(t);
                }
            }

            return output;
        }

        internal static TechType StringToTechType(string str)
        {
            TechType type;

            try
            {
                type = (TechType)Enum.Parse(typeof(TechType), str, true);
            }
            catch (Exception ex)
            {
                LogHandler.Error("Failed to parse string to TechType: " + str);
                LogHandler.Error(ex.Message);
                type = TechType.None;
            }

            return type;
        }

        internal static ETechTypeCategory StringToETechTypeCategory(string str)
        {
            ETechTypeCategory type;

            try
            {
                type = (ETechTypeCategory)Enum.Parse(typeof(ETechTypeCategory), str, true);
            }
            catch (Exception ex)
            {
                LogHandler.Error("Failed to parse string to ETechTypeCategory: " + str);
                LogHandler.Error(ex.Message);
                type = ETechTypeCategory.None;
            }

            return type;
        }

        internal static EProgressionNode StringToEProgressionNode(string str)
        {
            EProgressionNode node;

            try
            {
                node = (EProgressionNode)Enum.Parse(typeof(EProgressionNode), str, true);
            }
            catch (Exception ex)
            {
                LogHandler.Error("Failed to parse string to EProgressionNode: " + str);
                LogHandler.Error(ex.Message);
                node = EProgressionNode.None;
            }

            return node;
        }
    }
}
