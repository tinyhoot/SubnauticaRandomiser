using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using JetBrains.Annotations;
using SubnauticaRandomiser.RandomiserObjects;
using SubnauticaRandomiser.RandomiserObjects.Enums;
using UnityEngine;
using ILogHandler = SubnauticaRandomiser.Interfaces.ILogHandler;

namespace SubnauticaRandomiser
{
    internal class CSVReader
    {
        internal Dictionary<EBiomeType, List<float[]>> _csvAlternateStarts;
        internal List<BiomeCollection> _csvBiomeList;
        internal List<Databox> _csvDataboxList;
        internal List<LogicEntity> _csvRecipeList;
        private readonly ILogHandler _log;
        internal static string s_recipeCSVMD5;

        private const int _ExpectedColumns = 8;
        private const int _ExpectedRows = 245;
        private const int _ExpectedWreckColumns = 6;

        public CSVReader(ILogHandler logger)
        {
            _csvBiomeList = new List<BiomeCollection>();
            _csvDataboxList = new List<Databox>();
            _csvRecipeList = new List<LogicEntity>();
            _log = logger;
        }

        /// <summary>
        /// Attempt to parse a csv file containing information on alternate starts.
        /// </summary>
        /// <param name="fileName">The .csv file to parse.</param>
        /// <returns>The parsed Dictionary if successful, or null otherwise.</returns>
        internal Dictionary<EBiomeType, List<float[]>> ParseAlternateStartFile(string fileName)
        {
            // First, try to find and grab the file containing recipe information.
            string[] csvLines;
            string path = GetDataPath(fileName);
            _log.Debug("Looking for alternate start CSV as " + path);

            try
            {
                csvLines = File.ReadAllLines(path);
            }
            catch (Exception ex)
            {
                _log.MainMenuMessage("Failed to read alternate start CSV!");
                _log.Error(ex.Message);
                return null;
            }

            _csvAlternateStarts = new Dictionary<EBiomeType, List<float[]>>();

            int lineCounter = 0;
            foreach (string line in csvLines)
            {
                lineCounter++;
                if (line.StartsWith("Biome", StringComparison.InvariantCulture))
                {
                    // This is the header line. Skip.
                    continue;
                }

                // ParseRecipeFileLine fails upwards, so this ensures all errors are caught in one central location.
                try
                {
                    ParseAlternateStartLine(line);
                }
                catch (Exception ex)
                {
                    _log.Error("Failed to parse information from alternate start CSV on line "+lineCounter);
                    _log.Error(ex.Message);
                }
            }

            return _csvAlternateStarts;
        }

        /// <summary>
        /// Attempt to parse one content line of the alternate starts csv.
        /// </summary>
        /// <param name="line">The line to parse.</param>
        private void ParseAlternateStartLine(string line)
        {
            string[] cells = line.Split(',');
            if (cells.Length < 2)
                throw new FormatException("Unexpected number of columns: " + cells.Length);

            EBiomeType biome = StringToEBiomeType(cells[0]);
            List<float[]> starts = new List<float[]>();
            foreach (string cell in cells.Skip(1))
            {
                if (String.IsNullOrEmpty(cell))
                    continue;
                
                string[] rawCoords = cell.Split('/');
                if (rawCoords.Length != 4)
                    throw new FormatException("Invalid number of coordinates: " + rawCoords.Length);
                
                float[] parsedCoords = new float[4];
                for (int i = 0; i < 4; i++)
                {
                    parsedCoords[i] = float.Parse(rawCoords[i]);
                }
                starts.Add(parsedCoords);
            }
            
            _log.Debug("Registering alternate starts for biome " + biome);
            _csvAlternateStarts.Add(biome, starts);
        }

        /// <summary>
        /// Attempt to parse the given file into a list of entities representing recipes.
        /// </summary>
        /// <param name="fileName">The file to parse.</param>
        /// <returns>A list of LogicEntities if successful, null otherwise.</returns>
        [CanBeNull]
        internal List<LogicEntity> ParseRecipeFile(string fileName)
        {
            // First, try to find and grab the file containing recipe information.
            string[] csvLines;
            string path = GetDataPath(fileName);
            _log.Debug("Looking for recipe CSV as " + path);

            try
            {
                csvLines = File.ReadAllLines(path);
            }
            catch (Exception ex)
            {
                _log.MainMenuMessage("Failed to read recipe CSV! Aborting.");
                _log.Error(ex.Message);
                return null;
            }

            // If the CSV does not contain the expected amount of rows, it is likely that the user added custom items
            // to it. If the lines are the same, but the MD5 is not, some values of existing entries must have been
            // modified.
            s_recipeCSVMD5 = CalculateMD5(path);
            if (csvLines.Length != _ExpectedRows)
            {
                _log.Info("Recipe CSV seems to contain custom entries.");
            }
            else if (!s_recipeCSVMD5.Equals(InitMod._ExpectedRecipeMD5))
            {
                _log.Info("Recipe CSV seems to have been modified.");
            }

            // Second, read each line and try to parse that into a list of LogicEntity objects, for later use.
            _csvRecipeList = new List<LogicEntity>();

            int lineCounter = 0;
            foreach (string line in csvLines)
            {
                lineCounter++;
                if (line.StartsWith("TechType", StringComparison.InvariantCulture))
                {
                    // This is the header line. Skip.
                    continue;
                }

                // ParseRecipeFileLine fails upwards, so this ensures all errors are caught in one central location.
                try
                {
                    _csvRecipeList.Add(ParseRecipeFileLine(line));
                }
                catch (Exception ex)
                {
                    _log.Error("Failed to parse information from recipe CSV on line "+lineCounter);
                    _log.Error(ex.Message);
                }
            }

            return _csvRecipeList;
        }
        
        /// <summary>
        /// Parse one line of a CSV file and attempt to create a LogicEntity.
        /// </summary>
        /// <param name="line">A string to parse.</param>
        /// <returns>The fully processed LogicEntity.</returns>
        /// <exception cref="InvalidDataException">If the format of the data is wrong.</exception>
        /// <exception cref="ArgumentException">If a required column is missing or invalid.</exception>
        private LogicEntity ParseRecipeFileLine(string line)
        {
            TechType type;
            ETechTypeCategory category;
            int depth = 0;
            Recipe recipe = null;
            List<TechType> prereqList = new List<TechType>();
            int value;
            int maxUses = 0;

            Blueprint blueprint = null;
            List<TechType> blueprintUnlockConditions = new List<TechType>();
            List<TechType> blueprintFragments = new List<TechType>();
            bool blueprintDatabox = false;
            int blueprintUnlockDepth = 0;

            string[] cells = line.Split(',');

            if (cells.Length != _ExpectedColumns)
                throw new InvalidDataException("Unexpected number of columns: " + cells.Length + " instead of "
                                               + _ExpectedColumns);
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
                throw new ArgumentException("TechType is null or empty, but is a required field.");
            type = StringToEnum<TechType>(cellsTechType);

            // Column 2: Category
            if (string.IsNullOrEmpty(cellsCategory))
                throw new ArgumentException("Category is null or empty, but is a required field.");
            category = StringToEnum<ETechTypeCategory>(cellsCategory);

            // Column 3: Depth Difficulty
            if (!string.IsNullOrEmpty(cellsDepth))
                depth = StringToInt(cellsDepth, "Depth");

            // Column 4: Prerequisites
            if (!string.IsNullOrEmpty(cellsPrereqs))
                prereqList = ProcessMultipleTechTypes(cellsPrereqs.Split(';'));

            // Column 5: Value
            if (string.IsNullOrEmpty(cellsValue))
                throw new ArgumentException("Value is null or empty, but is a required field.");
            value = StringToInt(cellsValue, "Value");

            // Column 6: Max Uses Per Game
            if (!string.IsNullOrEmpty(cellsMaxUses))
                maxUses = StringToInt(cellsMaxUses, "Max Uses");

            // Column 7: Blueprint Unlock Conditions
            if (!string.IsNullOrEmpty(cellsBPUnlock))
            {
                string[] conditions = cellsBPUnlock.Split(';');

                foreach (string str in conditions)
                {
                    if (str.ToLower().Contains("fragment"))
                        blueprintFragments.Add(StringToEnum<TechType>(str));
                    else if (str.ToLower().Contains("databox"))
                        blueprintDatabox = true;
                    else
                        blueprintUnlockConditions.Add(StringToEnum<TechType>(str));
                }
            }

            // Column 8: Blueprint Unlock Depth
            if (!string.IsNullOrEmpty(cellsBPDepth))
                blueprintUnlockDepth = StringToInt(cellsBPDepth, "Blueprint Unlock Depth");

            // Only if any of the blueprint components yielded anything, ship the entity with a blueprint.
            if ((blueprintUnlockConditions.Count > 0) || blueprintUnlockDepth != 0 
                                                      || !blueprintDatabox 
                                                      || blueprintFragments.Count > 0)
            {
                blueprint = new Blueprint(type, blueprintUnlockConditions, blueprintFragments, blueprintDatabox, 
                                    blueprintUnlockDepth);
            }

            // Only if the category corresponds to a techtype commonly associated with a craftable thing, ship the
            // entity with a recipe.
            if (category.CanHaveRecipe())
                recipe = new Recipe(type);

            _log.Debug($"Registering entity: {type.AsString()}, {category}, {depth}, {prereqList.Count}"
                       + $" prerequisites, {value}, {maxUses}, ...");

            var entity = new LogicEntity(type, category, blueprint, recipe, null, prereqList, false, value)
                {
                    AccessibleDepth = depth,
                    MaxUsesPerGame = maxUses
                };
            return entity;
        }

        /// <summary>
        /// Attempt to parse the given file into a list of biomes and their stats.
        /// </summary>
        /// <param name="fileName">The file to parse.</param>
        /// <returns>A list of BiomeCollection if successful, null otherwise.</returns>
        [CanBeNull]
        internal List<BiomeCollection> ParseBiomeFile(string fileName)
        {
            // Try and grab the file containing biome information.
            string[] csvLines;
            string path = GetDataPath(fileName);
            _log.Debug("Looking for biome CSV as " + path);

            try
            {
                csvLines = File.ReadAllLines(path);
            }
            catch (Exception ex)
            {
                _log.MainMenuMessage("Failed to read biome CSV! Aborting.");
                _log.Error(ex.Message);
                return null;
            }

            _csvBiomeList = new List<BiomeCollection>();

            int lineCounter = 0;
            foreach (string line in csvLines)
            {
                lineCounter++;
                if (line.StartsWith("biomeType", StringComparison.InvariantCulture))
                {
                    // This is the header line. Skip.
                    continue;
                }

                // ParseBiomeFileLine fails upwards, so this ensures all errors are caught in one central location.
                try
                {
                    Biome biome = ParseBiomeFileLine(line);
                    BiomeCollection collection = _csvBiomeList.Find(x => x.BiomeType.Equals(biome.Region));

                    // Initiate a BiomeCollection if it does not already exist.
                    if (collection is null)
                    {
                        collection = new BiomeCollection(biome.Region);
                        _csvBiomeList.Add(collection);
                    }

                    collection.Add(biome);
                }
                catch (Exception ex)
                {
                    _log.Error("Failed to parse information from biome CSV on line " + lineCounter);
                    _log.Error(ex.Message);
                }
            }

            return _csvBiomeList;
        }

        /// <summary>
        /// Parse one line of a CSV file and attempt to create a single Biome.
        /// </summary>
        /// <param name="line">A string to parse.</param>
        /// <returns>The fully processed Biome.</returns>
        /// <exception cref="ArgumentException">If a required column is empty, missing or invalid.</exception>
        private Biome ParseBiomeFileLine(string line)
        {
            Biome biome;
            int smallCount;
            int mediumCount;
            int creatureCount;
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
            _log.Debug($"Registering biome: {name}, {biomeType}, {creatureCount}, {mediumCount}, {smallCount}");

            return biome;
        }
        
        /// <summary>
        /// Attempt to parse the given CSV file for wreckage information and extract stats on Databoxes.
        /// </summary>
        /// <param name="fileName">The file to parse.</param>
        /// <returns>A list of Databoxes if successful, null otherwise.</returns>
        [CanBeNull]
        internal List<Databox> ParseWreckageFile(string fileName)
        {
            string[] csvLines;
            string path = GetDataPath(fileName);
            _log.Debug("Looking for wreckage CSV as " + path);

            try
            {
                csvLines = File.ReadAllLines(path);
            }
            catch (Exception ex)
            {
                _log.MainMenuMessage("Failed to read wreckage CSV!");
                _log.Error(ex.Message);
                return null;
            }

            _csvDataboxList = new List<Databox>();
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
                        _csvDataboxList.Add(databox);
                }
                catch (Exception ex)
                {
                    _log.Error("Failed to parse information from wreckage CSV on line " + lineCounter);
                    _log.Error(ex.Message);
                }
            }

            return _csvDataboxList;
        }

        /// <summary>
        /// Parse one line of a CSV file and attempt to create a single Databox.
        /// </summary>
        /// <param name="line">A string to parse.</param>
        /// <returns>The fully processed Databox.</returns>
        /// <exception cref="ArgumentException">If a required column is empty, missing or invalid.</exception>
        private Databox ParseWreckageFileLine(string line)
        {
            TechType type;
            Vector3 coordinates;
            EWreckage wreck = EWreckage.None;
            bool isDatabox;
            bool laserCutter = false;
            bool propulsionCannon = false;

            string[] cells = line.Split(',');

            if (cells.Length != _ExpectedWreckColumns)
                throw new InvalidDataException("Unexpected number of columns: " + cells.Length + " instead of "
                                               + _ExpectedWreckColumns);
            // As above, it's not the prettiest, but it's flexible.
            string cellsTechType = cells[0];
            string cellsCoordinates = cells[1];
            string cellsEWreckage = cells[2];
            string cellsIsDatabox = cells[3];
            string cellsLaserCutter = cells[4];
            string cellsPropulsionCannon = cells[5];

            // Column 1: TechType
            if (string.IsNullOrEmpty(cellsTechType))
                throw new ArgumentException("TechType is null or empty, but is a required field.");
            type = StringToEnum<TechType>(cellsTechType);

            // Column 2: Coordinates
            if (!string.IsNullOrEmpty(cellsCoordinates))
            {
                string[] str = cellsCoordinates.Split(';');
                if (str.Length != 3)
                    throw new ArgumentException("Coordinates are not in a valid format: " + cellsCoordinates);

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
                wreck = StringToEnum<EWreckage>(cellsEWreckage);

            // Column 4: Is it a databox?
            // Redundant until fragments are implemented, so this does nothing.
            if (!string.IsNullOrEmpty(cellsIsDatabox))
                isDatabox = StringToBool(cellsIsDatabox, "IsDatabox");

            // Column 5: Does it need a laser cutter?
            if (!string.IsNullOrEmpty(cellsLaserCutter))
                laserCutter = StringToBool(cellsLaserCutter, "NeedsLaserCutter");

            // Column 6: Does it need a propulsion cannon?
            if (!string.IsNullOrEmpty(cellsPropulsionCannon))
                propulsionCannon = StringToBool(cellsPropulsionCannon, "NeedsPropulsionCannon");

            _log.Debug($"Registering databox: {type.AsString()}, {coordinates}, {wreck}, {laserCutter}, "
                       + propulsionCannon);
            Databox databox = new Databox(type, coordinates, wreck, laserCutter, propulsionCannon);

            return databox;
        }

        /// <summary>
        /// Calculate the MD5 hash for a given file.
        /// </summary>
        /// <param name="path">The path to the file to hash.</param>
        /// <returns>The MD5 hash.</returns>
        internal static string CalculateMD5(string path)
        {
            using MD5 md5 = MD5.Create();
            using FileStream fileStream = File.OpenRead(path);
            var hash = md5.ComputeHash(fileStream);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

        /// <summary>
        /// Get the absolute path to a file in the mod's DataFiles folder.
        /// </summary>
        /// <param name="fileName">The file.</param>
        /// <returns>The absolute path.</returns>
        private static string GetDataPath(string fileName)
        {
            string dataFolder = Path.Combine(InitMod.s_modDirectory, "DataFiles");
            return Path.Combine(dataFolder, fileName);
        }

        /// <summary>
        /// Turn multiple strings into their TechType equivalents.
        /// </summary>
        /// <returns>A list containing all successfully parsed TechTypes.</returns>
        /// <exception cref="ArgumentException">Raised if the parsing fails.</exception>
        [NotNull]
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

        /// <summary>
        /// Attempt to parse a given string into an Enum.
        /// </summary>
        /// <returns>The parsed Enum.</returns>
        /// <exception cref="ArgumentException">Raised if the parsing fails.</exception>
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
                if (str.ToLower().Contains(type.ToLower()))
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

        /// <summary>
        /// Attempt to parse a string into a boolean value.
        /// </summary>
        /// <param name="input">The value.</param>
        /// <param name="column">The name of the column the value was in.</param>
        /// <returns>The parsed boolean value as appropriate.</returns>
        /// <exception cref="FormatException">Raised if the input value is unparseable.</exception>
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

        /// <summary>
        /// Attempt to parse a string into a floating point value.
        /// </summary>
        /// <param name="input">The value.</param>
        /// <param name="column">The name of the column the value was in.</param>
        /// <returns>The parsed float.</returns>
        /// <exception cref="FormatException">Raised if the input value is unparseable.</exception>
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

        /// <summary>
        /// Attempt to parse a string into an integer.
        /// </summary>
        /// <param name="input">The value.</param>
        /// <param name="column">The name of the column the value was in.</param>
        /// <returns>The parsed integer.</returns>
        /// <exception cref="FormatException">Raised if the input value is unparseable.</exception>
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
