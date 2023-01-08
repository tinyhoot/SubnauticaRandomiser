using System.Collections.Generic;
using SubnauticaRandomiser.Interfaces;

namespace SubnauticaRandomiser.Logic
{
    internal class AuroraLogic
    {
        private readonly CoreLogic _logic;

        private ILogHandler _log => _logic._Log;
        private IRandomHandler _random => _logic._Random;
        private EntitySerializer _serializer => _logic._Serializer;

        private readonly List<string> keypadPrefabClassIds = new List<string>
        {
            "38135f4d-5f31-4438-abce-2c8bbbc5c77c", // GenericLab
            "48a5564b-e632-4666-9e7c-f377fbc4fd23", // CargoRoom
            "3265d800-9ae0-478c-973c-ddf5351977c0", // LivingArea_Bedroom2
            "19feccc5-36a0-431c-ae97-16f87c21d5af", // CaptainsQuarters
        };

        public AuroraLogic(CoreLogic logic)
        {
            _logic = logic;
        }

        /// <summary>
        /// Randomise the access codes for all doors in the Aurora.
        /// </summary>
        public void RandomiseDoorCodes()
        {
            Dictionary<string, string> keyCodes = new Dictionary<string, string>();
            foreach (string classId in keypadPrefabClassIds)
            {
                int code = _random.Next(0, 9999);
                _log.Debug($"[AR] Assigning accessCode {code} to {classId}");
                keyCodes.Add(classId, code.ToString().PadLeft(4, '0'));
            }

            _serializer.DoorKeyCodes = keyCodes;
        }
    }
}