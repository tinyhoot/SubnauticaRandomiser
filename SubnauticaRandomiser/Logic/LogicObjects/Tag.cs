namespace SubnauticaRandomiser.Logic.LogicObjects
{
    /// <summary>
    /// Keeps track of some of the tags used throughout some modules.
    /// <br />
    /// This is not an enum because basing tags on strings allows for the easy addition of extra tags on the fly by
    /// e.g. a module.
    /// </summary>
    internal static class Tag
    {
        public const string BasePiece = "BasePiece";
        public const string Creature = "Creature";
        public const string Egg = "Egg";
        public const string EggCreature = "EggCreature";
        public const string Equipment = "Equipment";
        public const string Seed = "Seed";
        public const string Tool = "Tool";
        public const string Upgrade = "Upgrade";
    }
}