using System;

namespace SubnauticaRandomiser
{
    /// <summary>
    /// One class capable of dumping internal data to log files for an easy overview of what's happening.
    /// </summary>
    internal static class DataDumper
    {
        public static void LogBiomes()
        {
            // Grab a copy of all vanilla BiomeData. This loads it fresh from disk
            // and will thus be unaffected by any existing randomisation.
            LootDistributionData loot = LootDistributionData.Load(LootDistributionData.dataPath);

            Initialiser._Log.Debug("---Dumping Biomes");
            BiomeType[] biomes = (BiomeType[])Enum.GetValues(typeof(BiomeType));
            foreach (BiomeType biome in biomes)
            {
                if (loot.GetBiomeLoot(biome, out LootDistributionData.DstData distributionData))
                {
                    int valid = 0;
                    int validFragments = 0;
                    float sum = 0f;
                    float sumFragments = 0f;
                    foreach (var prefab in distributionData.prefabs)
                    {
                        if (string.IsNullOrEmpty(prefab.classId) || prefab.classId.Equals("None"))
                            continue;

                        valid++;
                        sum += prefab.probability;

                        if (UWE.WorldEntityDatabase.TryGetInfo(prefab.classId, out UWE.WorldEntityInfo info)){
                            if (info != null && !info.techType.Equals(TechType.None) && info.techType.AsString().ToLower().Contains("fragment"))
                            {
                                validFragments++;
                                sumFragments += prefab.probability;
                            }
                        }
                    }
                    Initialiser._Log.Debug(
                        $"{biome.AsString()}\t{valid} entries\t{validFragments} fragments\t{sum} totalspawnrate\t{sumFragments} totalfragmentrate");
                }
                else
                {
                    Initialiser._Log.Debug($"{biome.AsString()}\tNONE\t\t");
                }
            }
        }
        
        public static void LogKnownTech()
        {
            foreach (var tech in KnownTech.compoundTech)
            {
                Initialiser._Log.Debug($"Compound: {tech.techType}, {tech.dependencies}");
            }

            foreach (var tech in KnownTech.analysisTech)
            {
                Initialiser._Log.Debug($"Scanning {tech.techType} unlocks:");
                foreach (var unlock in tech.unlockTechTypes)
                {
                    Initialiser._Log.Debug($"-- {unlock}");
                }
            }
        }

        public static void LogPDAEncyclopedia()
        {
            foreach (var kvpair in PDAEncyclopedia.mapping)
            {
                Initialiser._Log.Debug($"Key: {kvpair.Key}, Path: {kvpair.Value.path}");
                Initialiser._Log.Debug(Language.main.Get("EncyDesc_" + kvpair.Key));
            }
        }

        public static void LogPrefabs()
        {
            // Cache the ids, otherwise this logs nothing.
            _ = CraftData.GetClassIdForTechType(TechType.Titanium);
            var keys = UWE.PrefabDatabase.prefabFiles.Keys;
            foreach (string classId in keys)
            {
                Initialiser._Log.Debug($"classId: {classId}, prefab: {UWE.PrefabDatabase.prefabFiles[classId]}");
            }
        }
    }
}