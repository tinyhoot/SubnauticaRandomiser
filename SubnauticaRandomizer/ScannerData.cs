using System.Collections.Generic;

namespace SubnauticaRandomizer
{
    class ScannerData
    {
        public Dictionary<TechType, int> Required = new Dictionary<TechType, int>();

        public ScannerData(Dictionary<string, int> config)
        {
            foreach (var kvp in config)
            {
                var techType = kvp.Key.ToTechType();
                Required[techType] = kvp.Value;
            }
        }
    }
}
