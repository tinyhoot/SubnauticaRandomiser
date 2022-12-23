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
                InitMod._log.Debug($"Compound: {tech.techType}, {tech.dependencies}");
            }

            foreach (var tech in KnownTech.analysisTech)
            {
                InitMod._log.Debug($"Scanning {tech.techType} unlocks:");
                foreach (var unlock in tech.unlockTechTypes)
                {
                    InitMod._log.Debug($"-- {unlock}");
                }
            }
        }
    }
}