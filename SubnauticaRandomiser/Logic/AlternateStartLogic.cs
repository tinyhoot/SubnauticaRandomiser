using System.Collections.Generic;
using SubnauticaRandomiser.RandomiserObjects;
using UnityEngine;
using Random = System.Random;

namespace SubnauticaRandomiser.Logic
{
    internal class AlternateStartLogic
    {
        private readonly Dictionary<EBiomeType, List<float[]>> _alternateStarts;
        private readonly CoreLogic _logic;

        private EntitySerializer _masterDict => _logic._masterDict;
        private Random _random => _logic._random;

        internal AlternateStartLogic(CoreLogic logic, Dictionary<EBiomeType, List<float[]>> alternateStarts)
        {
            _logic = logic;
            _alternateStarts = alternateStarts;
        }

        internal void Randomise()
        {
            _masterDict.StartPoint = GetRandomStart();
        }

        private RandomiserVector GetRandomStart()
        {
            EBiomeType biome = (EBiomeType)15; // TODO: Replace this with a config value.
            // TODO: Choose a random biome if the config demands it.

            if (!_alternateStarts.ContainsKey(biome))
            {
                LogHandler.Error("No information found on chosen starting biome " + biome);
                return new RandomiserVector(0, 0, 0);
            }

            // Choose one of the possible spawning boxes within the biome.
            int boxIdx = _random.Next(0, _alternateStarts[biome].Count);
            float[] box = _alternateStarts[biome][boxIdx];
            // Choose the specific spawn point within the box.
            int x = _random.Next((int)box[0], (int)box[2] + 1);
            int z = _random.Next((int)box[3], (int)box[1] + 1);

            LogHandler.Debug("Chosen new lifepod spawnpoint at x:" + x + " y:0" + " z:" + z);
            return new RandomiserVector(x, 0, z);
        }
    }
}