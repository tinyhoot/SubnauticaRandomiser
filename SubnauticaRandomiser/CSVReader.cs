using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using JetBrains.Annotations;
using SubnauticaRandomiser.Handlers;
using SubnauticaRandomiser.Objects;
using SubnauticaRandomiser.Objects.Enums;
using SubnauticaRandomiser.Objects.Exceptions;
using UnityEngine;
using ILogHandler = SubnauticaRandomiser.Interfaces.ILogHandler;

namespace SubnauticaRandomiser
{
    internal static class CSVReader
    {
        private static readonly CultureInfo _culture = CultureInfo.InvariantCulture;
        private static ILogHandler _log => Initialiser._Log;

        /// <summary>
        /// Wow I wish this could be an async enumerator but NO WE'RE STUCK ON 4.7.2
        /// </summary>
        private static async Task<List<string[]>> ReadCsvAsync(string fileName)
        {
            List<string[]> lines = new List<string[]>();

            _log.Debug($"Looking for csv: {fileName}");
            try
            {
                using StreamReader reader = new StreamReader(GetDataPath(fileName));
                while (!reader.EndOfStream)
                {
                    string line = await reader.ReadLineAsync();
                    string[] cells = line.Split(',');
                    lines.Add(cells);
                }
            }
            catch (Exception ex)
            {
                _log.InGameMessage($"Failed to read datafile: {fileName}");
                _log.Error(ex.Message);
                return null;
            }

            return lines;
        }

        /// <summary>
        /// Attempt to parse a csv file containing information on alternate starts.
        /// </summary>
        /// <param name="fileName">The .csv file to parse.</param>
        /// <returns>The parsed Dictionary if successful, or null otherwise.</returns>
        public static async Task<Dictionary<Objects.Enums.BiomeRegion, List<float[]>>> ParseAlternateStartAsync(string fileName)
        {
            List<string[]> lines = await ReadCsvAsync(fileName);
            if (lines is null)
                throw new ParsingException();

            Dictionary<Objects.Enums.BiomeRegion, List<float[]>> parsedStarts = new Dictionary<Objects.Enums.BiomeRegion, List<float[]>>();
            for (int i = 1; i < lines.Count; i++)
            {
                string[] cells = lines[i];
                try
                {
                    Objects.Enums.BiomeRegion biome = EnumHandler.Parse<Objects.Enums.BiomeRegion>(cells[0]);
                    var starts = ParseAlternateStartLine(cells);
                    parsedStarts.Add(biome, starts);
                    _log.Debug($"Registered alternate starts for biome {biome}");
                }
                catch (Exception ex)
                {
                    _log.Error($"Failed to parse information from alternate start CSV on line {i}");
                    _log.Error(ex.Message);
                }
            }

            return parsedStarts;
        }


        /// <summary>
        /// Attempt to parse one content line of the alternate starts csv.
        /// </summary>
        private static List<float[]> ParseAlternateStartLine(string[] cells)
        {
            if (cells.Length < 2)
                throw new FormatException("Unexpected number of columns: " + cells.Length);

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
                    parsedCoords[i] = float.Parse(rawCoords[i], _culture);
                }

                starts.Add(parsedCoords);
            }

            return starts;
        }

        public static async Task<List<T>> ParseDataFileAsync<T>(string fileName, Func<string[], T> lineParser)
        {
            List<string[]> lines = await ReadCsvAsync(fileName);
            if (lines is null)
                throw new ParsingException();

            List<T> parsedLines = new List<T>();
            // Always skip the header line.
            for (int i = 1; i < lines.Count; i++)
            {
                string[] cells = lines[i];
                try
                {
                    // Parse each line using the provided callback method.
                    T parsedLine = lineParser(cells);
                    if (parsedLine is null)
                        continue;
                    parsedLines.Add(parsedLine);
                }
                catch (Exception ex)
                {
                    _log.Error($"Failed to parse information from csv {fileName} on line {i}");
                    _log.Error(ex.Message);
                }
            }

            return parsedLines;
        }

        /// <summary>
        /// Parse one line of a CSV file and attempt to create a LogicEntity.
        /// </summary>
        /// <param name="cells">An array representing the cells on one line of the file.</param>
        /// <returns>The fully processed LogicEntity.</returns>
        /// <exception cref="InvalidDataException">If the format of the data is wrong.</exception>
        /// <exception cref="ArgumentException">If a required column is missing or invalid.</exception>
        public static LogicEntity ParseRecipeLine(string[] cells)
        {
            EntityType entityType;
            TechType techType;
            TechTypeCategory category;
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
            techType = EnumHandler.Parse<TechType>(cellsTechType);

            // Column 2: Category
            if (string.IsNullOrEmpty(cellsCategory))
                throw new ArgumentException("Category is null or empty, but is a required field.");
            category = EnumHandler.Parse<TechTypeCategory>(cellsCategory);
            entityType = category.Equals(TechTypeCategory.Fragments) ? EntityType.Fragment : EntityType.Recipe;

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
                        blueprintFragments.Add(EnumHandler.Parse<TechType>(str));
                    else if (str.ToLower().Contains("databox"))
                        blueprintDatabox = true;
                    else
                        blueprintUnlockConditions.Add(EnumHandler.Parse<TechType>(str));
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
                blueprint = new Blueprint(techType, blueprintUnlockConditions, blueprintFragments, blueprintDatabox,
                    blueprintUnlockDepth);
            }

            // Only if the category corresponds to a techtype commonly associated with a craftable thing, ship the
            // entity with a recipe.
            if (category.IsCraftable())
                recipe = new Recipe(techType);

            _log.Debug($"Registering entity: {techType.AsString()}, {category}, {depth}, {prereqList.Count}"
                       + $" prerequisites, {value}, {maxUses}, ...");

            var entity = new LogicEntity(entityType, techType, category, blueprint, recipe, null, prereqList, false,
                value)
            {
                AccessibleDepth = depth,
                MaxUsesPerGame = maxUses
            };
            return entity;
        }


        /// <summary>
        /// Parse one line of a CSV file and attempt to create a single Biome.
        /// </summary>
        /// <param name="cells">An array representing the cells on one line of the file.</param>
        /// <returns>The fully processed Biome.</returns>
        /// <exception cref="ArgumentException">If a required column is empty, missing or invalid.</exception>
        public static Biome ParseBiomeLine(string[] cells)
        {
            Biome biome;
            int smallCount;
            int mediumCount;
            int creatureCount;
            float? fragmentRate = null;

            string name = cells[0];
            string cellsSmallCount = cells[1];
            string cellsMediumCount = cells[2];
            string cellsCreatureCount = cells[3];
            string cellsFragmentRate = cells[4];

            // Column 1: The internal name of the biome. Does not have to be
            // processed at all, the string itself is good enough.
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("BiomeType is null or empty, but is a required field.");
            Objects.Enums.BiomeRegion biomeRegion = EnumHandler.Parse<Objects.Enums.BiomeRegion>(name);

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

            biome = new Biome(name, biomeRegion, creatureCount, mediumCount, smallCount, fragmentRate);
            _log.Debug($"Registering biome: {name}, {biomeRegion}, {creatureCount}, {mediumCount}, {smallCount}");

            return biome;
        }

        /// <summary>
        /// Parse one line of a CSV file and attempt to create a single Databox.
        /// </summary>
        /// <param name="cells">An array representing the cells on one line of the file.</param>
        /// <returns>The fully processed Databox.</returns>
        /// <exception cref="ArgumentException">If a required column is empty, missing or invalid.</exception>
        public static Databox ParseWreckageLine(string[] cells)
        {
            TechType type;
            Vector3 coordinates;
            Wreckage wreck = Wreckage.None;
            bool isDatabox;
            bool laserCutter = false;
            bool propulsionCannon = false;

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
            type = EnumHandler.Parse<TechType>(cellsTechType);

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
                wreck = EnumHandler.Parse<Wreckage>(cellsEWreckage);

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
        public static string CalculateMD5(string path)
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
            string dataFolder = Path.Combine(Initialiser.GetModDirectory(), "DataFiles");
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
                    TechType t = EnumHandler.Parse<TechType>(s);
                    output.Add(t);
                }
            }

            return output;
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
                inputInt = int.Parse(input, _culture);
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
                output = float.Parse(input, _culture);
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
                output = int.Parse(input, _culture);
            }
            catch (Exception)
            {
                throw new FormatException(column + " is not an integer: " + input);
            }

            return output;
        }
    }
}