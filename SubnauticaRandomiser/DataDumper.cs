namespace SubnauticaRandomiser
{
    /// <summary>
    /// One class capable of dumping internal data to log files for an easy overview of what's happening.
    /// </summary>
    internal static class DataDumper
    {
        public static void LogKnownTech()
        {
            foreach (var tech in KnownTech.compoundTech)
            {
                InitMod._Log.Debug($"Compound: {tech.techType}, {tech.dependencies}");
            }

            foreach (var tech in KnownTech.analysisTech)
            {
                InitMod._Log.Debug($"Scanning {tech.techType} unlocks:");
                foreach (var unlock in tech.unlockTechTypes)
                {
                    InitMod._Log.Debug($"-- {unlock}");
                }
            }
        }

        public static void LogPrefabs()
        {
            // Cache the ids, otherwise this logs nothing.
            _ = CraftData.GetClassIdForTechType(TechType.Titanium);
            var keys = UWE.PrefabDatabase.prefabFiles.Keys;
            foreach (string classId in keys)
            {
                InitMod._Log.Debug($"classId: {classId}, prefab: {UWE.PrefabDatabase.prefabFiles[classId]}");
            }
        }
    }
}