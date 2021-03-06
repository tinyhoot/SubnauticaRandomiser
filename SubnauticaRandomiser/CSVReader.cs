using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using SMLHelper.V2.Crafting;
using SubnauticaRandomiser.RandomiserObjects;
using UnityEngine;

namespace SubnauticaRandomiser
{
    internal static class CSVReader
    {
        internal static List<LogicEntity> s_csvRecipeList;
        internal static List<BiomeCollection> s_csvBiomeList;
        internal static List<Databox> s_csvDataboxList;
        internal static string s_recipeCSVMD5;

        private static readonly int s_expectedColumns = 8;
        private static readonly int s_expectedRows = 245;
        private static readonly int s_expectedWreckColumns = 6;

        internal static List<LogicEntity> ParseRecipeFile(string fileName)
        {
            // First, try to find and grab the file containing recipe information.
            string[] csvLines;
            string path = Path.Combine(InitMod.s_modDirectory, fileName);
            LogHandler.Debug("Looking for recipe CSV as " + path);

            try
            {
                csvLines = File.ReadAllLines(path);
            }
            catch (Exception ex)
            {
                LogHandler.MainMenuMessage("Failed to read recipe CSV! Aborting.");
                LogHandler.Error(ex.Message);
                return null;
            }

            // If the CSV does not contain the expected amount of rows, it is
            // likely that the user added custom items to it.
            // If the lines are the same, but the MD5 is not, some values of
            // existing entries must have been modified.
            s_recipeCSVMD5 = CalculateMD5(path);
            if (csvLines.Length != s_expectedRows)
            {
                LogHandler.Info("Recipe CSV seems to contain custom entries.");
            }
            else if (!s_recipeCSVMD5.Equals(InitMod.s_expectedRecipeMD5))
            {
                LogHandler.Info("Recipe CSV seems to have been modified.");
            }

            // Second, read each line and try to parse that into a list of
            // LogicEntity objects, for later use.
            s_csvRecipeList = new List<LogicEntity>();

            int lineCounter = 0;
            foreach (string line in csvLines)
            {
                lineCounter++;
                if (line.StartsWith("TechType", StringComparison.InvariantCulture))
                {
                    // This is the header line. Skip.
                    continue;
                }

                // ParseRecipeFileLine fails upwards, so this ensures all errors
                // are caught in one central location.
                try
                {
                    s_csvRecipeList.Add(ParseRecipeFileLine(line));
                }
                catch (Exception ex)
                {
                    LogHandler.Error("Failed to parse information from recipe CSV on line "+lineCounter);
                    LogHandler.Error(ex.Message);
                }
            }

            return s_csvRecipeList;
        }

        // Parse one line of a CSV file and attempt to create a LogicEntity.
        private static LogicEntity ParseRecipeFileLine(string line)
        {
            LogicEntity entity = null;

            TechType type = TechType.None;
            ETechTypeCategory category = ETechTypeCategory.None;
            int depth = 0;
            Recipe recipe = null;
            List<TechType> prereqList = new List<TechType>();
            int value = 0;
            int maxUses = 0;

            Blueprint blueprint = null;
            List<TechType> blueprintUnlockConditions = new List<TechType>();
            List<TechType> blueprintFragments = new List<TechType>();
            bool blueprintDatabox = false;
            int blueprintUnlockDepth = 0;

            string[] cells = line.Split(',');

            if (cells.Length != s_expectedColumns)
            {
                throw new InvalidDataException("Unexpected number of columns: " + cells.Length + " instead of " + s_expectedColumns);
            }
            // While ugly, this makes it much easier to react to changes in the
            // structure of the CSV. Also less prone to accidental oversights.
            string cellsTechType = cells[0];
            string cellsCategory = cells[1];
            string cellsDepth = cells[2];
            string cellsPrereqs = cells[3];
            string cellsValue = cells[4];
            string cellsMaxUses = cells[5];
            string cellsBPUnlock = cells[6];
            string cellsBPDepth = cells[7];
            
            // Now to convert the data in each cell to an object we can use.
            // Column 1: TechType
            if (string.IsNullOrEmpty(cellsTechType))
            {
                throw new ArgumentException("TechType is null or empty, but is a required field.");
            }
            type = StringToEnum<TechType>(cellsTechType);

            // Column 2: Category
            if (string.IsNullOrEmpty(cellsCategory))
            {
                throw new ArgumentException("Category is null or empty, but is a required field.");
            }
            category = StringToEnum<ETechTypeCategory>(cellsCategory);

            // Column 3: Depth Difficulty
            if (!string.IsNullOrEmpty(cellsDepth))
            {
                depth = StringToInt(cellsDepth, "Depth");
            }

            // Column 4: Prerequisites
            if (!string.IsNullOrEmpty(cellsPrereqs))
            {
                prereqList = ProcessMultipleTechTypes(cellsPrereqs.Split(';'));
            }

            // Column 5: Value
            if (string.IsNullOrEmpty(cellsValue))
            {
                throw new ArgumentException("Value is null or empty, but is a required field.");
            }
            value = StringToInt(cellsValue, "Value");

            // Column 6: Max Uses Per Game
            if (!string.IsNullOrEmpty(cellsMaxUses))
            {
                maxUses = StringToInt(cellsMaxUses, "Max Uses");
            }

            // Column 7: Blueprint Unlock Conditions
            if (!string.IsNullOrEmpty(cellsBPUnlock))
            {
                string[] conditions = cellsBPUnlock.Split(';');

                foreach (string str in conditions)
                {
                    if (str.ToLower().Contains("fragment"))
                    {
                        blueprintFragments.Add(StringToEnum<TechType>(str));
                    } 
                    else if (str.ToLower().Contains("databox"))
                    {
                        blueprintDatabox = true;
                    }
                    else
                    {
                        blueprintUnlockConditions.Add(StringToEnum<TechType>(str));
                    }
                }
            }

            // Column 8: Blueprint Unlock Depth
            if (!string.IsNullOrEmpty(cellsBPDepth))
            {
                blueprintUnlockDepth = StringToInt(cellsBPDepth, "Blueprint Unlock Depth");
            }
            
            // Only if any of the blueprint components yielded anything,
            // ship the entity with a blueprint.
            if ((blueprintUnlockConditions != null && blueprintUnlockConditions.Count > 0) || blueprintUnlockDepth != 0 || !blueprintDatabox || blueprintFragments.Count > 0)
            {
                blueprint = new Blueprint(type, blueprintUnlockConditions, blueprintFragments, blueprintDatabox, blueprintUnlockDepth);
            }

            // Only if the category corresponds to a techtype commonly associated
            // with a craftable thing, ship the entity with a recipe.
            if (category.CanHaveRecipe())
            {
                recipe = new Recipe(type);
            }

            LogHandler.Debug("Registering entity: " + type.AsString() + ", " + category.ToString() + ", " + depth + ", "+ prereqList.Count + " prerequisites, " + value + ", " + maxUses + ", ...");

            entity = new LogicEntity(type, category, blueprint, recipe, null, prereqList, false, value);
            entity.AccessibleDepth = depth;
            entity.MaxUsesPerGame = maxUses;
            return entity;
        }

        // This handles everything related to the biome CSV.
        internal static List<BiomeCollection> ParseBiomeFile(string fileName)
        {
            // Try and grab the file containing biome information.
            string[] csvLines;
            string path = Path.Combine(InitMod.s_modDirectory, fileName);
            LogHandler.Debug("Looking for biome CSV as " + path);

            try
            {
                csvLines = File.ReadAllLines(path);
            }
            catch (Exception ex)
            {
                LogHandler.MainMenuMessage("Failed to read biome CSV! Aborting.");
                LogHandler.Error(ex.Message);
                return null;
            }

            s_csvBiomeList = new List<BiomeCollection>();

            int lineCounter = 0;
            foreach (string line in csvLines)
            {
                lineCounter++;
                if (line.StartsWith("biomeType", StringComparison.InvariantCulture))
                {
                    // This is the header line. Skip.
                    continue;
                }

                // ParseBiomeFileLine fails upwards, so this ensures all errors
                // are caught in one central location.
                try
                {
                    Biome biome = ParseBiomeFileLine(line);
                    BiomeCollection collection = s_csvBiomeList.Find(x => x.BiomeType.Equals(biome.BiomeType));

                    // Initiate a BiomeCollection if it does not alread exist.
                    if (collection is null)
                    {
                        collection = new BiomeCollection(biome.BiomeType);
                        s_csvBiomeList.Add(collection);
                    }

                    collection.Add(biome);
                }
                catch (Exception ex)
                {
                    LogHandler.Error("Failed to parse information from biome CSV on line " + lineCounter);
                    LogHandler.Error(ex.Message);
                }
            }

            return s_csvBiomeList;
        }

        private static Biome ParseBiomeFileLine(string line)
        {
            Biome biome = null;
            int smallCount = 0;
            int mediumCount = 0;
            int creatureCount = 0;
            float? fragmentRate = null;

            string[] cells = line.Split(',');

            string name = cells[0];
            string cellsSmallCount = cells[1];
            string cellsMediumCount = cells[2];
            string cellsCreatureCount = cells[3];
            string cellsFragmentRate = cells[4];

            // Column 1: The internal name of the biome. Does not have to be
            // processed at all, the string itself is good enough.
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("BiomeType is null or empty, but is a required field.");
            EBiomeType biomeType = StringToEBiomeType(name);

            // Column 2: The number of small slots.
            if (string.IsNullOrEmpty(cellsSmallCount))
                throw new ArgumentException("smallCount is null or empty, but is a required field.");
            smallCount = StringToInt(cellsSmallCount, "smallCount");
            smallCount = (int)Math.Ceiling((double)smallCount / 100);

            // Column 3: The number of medium slots.
            if (string.IsNullOrEmpty(cellsMediumCount))
                throw new ArgumentException("mediumCount is null or empty, but is a required field.");
            mediumCount = StringToInt(cellsMediumCount, "mediumCount");
            mediumCount = (int)Math.Ceiling((double)mediumCount / 100);

            // Column 4: The number of creature slots.
            if (string.IsNullOrEmpty(cellsCreatureCount))
                throw new ArgumentException("creatureCount is null or empty, but is a required field.");
            creatureCount = StringToInt(cellsCreatureCount, "creatureCount");
            creatureCount = (int)Math.Ceiling((double)creatureCount / 100);

            // Column 5: The total chance of fragments in vanilla Subnautica.
            if (!string.IsNullOrEmpty(cellsFragmentRate))
                fragmentRate = StringToFloat(cellsFragmentRate, "fragmentRate");

            biome = new Biome(name, biomeType, creatureCount, mediumCount, smallCount, fragmentRate);
            LogHandler.Debug("Registering biome: " + name + ", " + biomeType.ToString() + ", " + creatureCount + ", " + mediumCount + ", " + smallCount);

            return biome;
        }

        // This handles everything related to the wreckage CSV and databoxes.
        // Similar in structure to the recipe CSV parser above.
        internal static List<Databox> ParseWreckageFile(string fileName)
        {
            string[] csvLines;
            string path = Path.Combine(InitMod.s_modDirectory, fileName);
            LogHandler.Debug("Looking for wreckage CSV as " + path);

            try
            {
                csvLines = File.ReadAllLines(path);
            }
            catch (Exception ex)
            {
                LogHandler.MainMenuMessage("Failed to read wreckage CSV!");
                LogHandler.Error(ex.Message);
                return null;
            }

            s_csvDataboxList = new List<Databox>();
            int lineCounter = 0;

            foreach (string line in csvLines)
            {
                lineCounter++;
                if (line.StartsWith("TechType", StringComparison.InvariantCulture))
                {
                    // This is the header line. Skip.
                    continue;
                }

                // For now, this only handles databoxes and ignores everything else.
                try
                {
                    Databox databox = ParseWreckageFileLine(line);
                    if (databox != null)
                        s_csvDataboxList.Add(databox);
                }
                catch (Exception ex)
                {
                    LogHandler.Error("Failed to parse information from wreckage CSV on line " + lineCounter);
                    LogHandler.Error(ex.Message);
                }
            }

            return s_csvDataboxList;
        }

        private static Databox ParseWreckageFileLine(string line)
        {
            Databox databox = null;

            TechType type = TechType.None;
            Vector3 coordinates = Vector3.zero;
            EWreckage wreck = EWreckage.None;
            bool isDatabox = false;
            bool laserCutter = false;
            bool propulsionCannon = false;

            string[] cells = line.Split(',');

            if (cells.Length != s_expectedWreckColumns)
            {
                throw new InvalidDataException("Unexpected number of columns: " + cells.Length + " instead of " + s_expectedWreckColumns);
            }
            // As above, it's not the prettiest, but it's flexible.
            string cellsTechType = cells[0];
            string cellsCoordinates = cells[1];
            string cellsEWreckage = cells[2];
            string cellsIsDatabox = cells[3];
            string cellsLaserCutter = cells[4];
            string cellsPropulsionCannon = cells[5];

            // Column 1: TechType
            if (string.IsNullOrEmpty(cellsTechType))
            {
                throw new ArgumentException("TechType is null or empty, but is a required field.");
            }
            type = StringToEnum<TechType>(cellsTechType);

            // Column 2: Coordinates
            if (!string.IsNullOrEmpty(cellsCoordinates))
            {
                string[] str = cellsCoordinates.Split(';');
                if (str.Length != 3)
                {
                    throw new ArgumentException("Coordinates are not in a valid format: " + cellsCoordinates);
                }

                float x = StringToFloat(str[0], "Coordinates");
                float y = StringToFloat(str[1], "Coordinates");
                float z = StringToFloat(str[2], "Coordinates");
                coordinates = new Vector3(x, y, z);
            }
            else
            {
                // The only reason this should be empty is if it is a fragment.
                // For now, skip those.
                return null;
            }

            // Column 3: General location
            if (!string.IsNullOrEmpty(cellsEWreckage))
            {
                wreck = StringToEnum<EWreckage>(cellsEWreckage);
            }

            // Column 4: Is it a databox?
            // Redundant until fragments are implemented, so this does nothing.
            if (!string.IsNullOrEmpty(cellsIsDatabox))
            {
                isDatabox = StringToBool(cellsIsDatabox, "IsDatabox");
            }

            // Column 5: Does it need a laser cutter?
            if (!string.IsNullOrEmpty(cellsLaserCutter))
            {
                laserCutter = StringToBool(cellsLaserCutter, "NeedsLaserCutter");
            }

            // Column 6: Does it need a propulsion cannon?
            if (!string.IsNullOrEmpty(cellsPropulsionCannon))
            {
                propulsionCannon = StringToBool(cellsPropulsionCannon, "NeedsPropulsionCannon");
            }

            LogHandler.Debug("Registering databox: " + type + ", " + coordinates.ToString() + ", " + wreck.ToString() + ", " + laserCutter + ", " + propulsionCannon);
            databox = new Databox(type, coordinates, wreck, laserCutter, propulsionCannon);

            return databox;
        }

        internal static string CalculateMD5(string path)
        {
            using (MD5 md5 = MD5.Create())
            {
                using (FileStream fileStream = File.OpenRead(path))
                {
                    var hash = md5.ComputeHash(fileStream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }

        private static List<TechType> ProcessMultipleTechTypes(string[] str)
        {
            List<TechType> output = new List<TechType>();

            foreach (string s in str)
            {
                if (!String.IsNullOrEmpty(s))
                {
                    TechType t = StringToEnum<TechType>(s);
                    output.Add(t);
                }
            }

            return output;
        }

        private static TEnum StringToEnum<TEnum>(string str)
            where TEnum : struct
        {
            if (!Enum.TryParse(str, true, out TEnum result))
            {
                throw new ArgumentException("Failed to parse " + typeof(TEnum) + " from string: " + str);
            }

            return result;
        }

        private static EBiomeType StringToEBiomeType(string str)
        {
            foreach (string type in Enum.GetNames(typeof(EBiomeType)))
            {
                if (str.Contains(type))
                {
                    try
                    {
                        return (EBiomeType)Enum.Parse(typeof(EBiomeType), type);
                    }
                    catch (Exception)
                    {
                        throw new ArgumentException("Failed to parse EBiomeType from string: " + str);
                    }
                }
            }

            return EBiomeType.None;
        }

        private static bool StringToBool(string input, string column)
        {
            // If the string is "true" or "false", this just works.
            if (bool.TryParse(input, out bool output))
            {
                return output;
            }

            int inputInt;
            // Integers need a bit of extra handling.
            try
            {
                inputInt = int.Parse(input);
            }
            catch (Exception)
            {
                throw new FormatException(column + " is not a valid boolean value: " + input);
            }

            switch (inputInt)
            {
                case 0:
                    return false;
                case 1:
                    return true;
            }

            throw new FormatException(column + " is not a valid boolean value: " + input);
        }

        private static float StringToFloat(string input, string column)
        {
            float output;

            try
            {
                output = float.Parse(input);
            }
            catch (Exception)
            {
                throw new FormatException(column + " does not contain a floating point value: " + input);
            }

            return output;
        }

        private static int StringToInt(string input, string column)
        {
            int output;

            try
            {
                output = int.Parse(input);
            }
            catch (Exception)
            {
                throw new FormatException(column + " is not an integer: " + input);
            }

            return output;
        }
    }
}
