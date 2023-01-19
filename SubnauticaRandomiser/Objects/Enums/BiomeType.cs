namespace SubnauticaRandomiser.Objects.Enums
{
    public enum BiomeType
    {
        None,
        ActiveLavaZone,
        BloodKelp,
        BonesField,
        Canyon,
        CragField,
        CrashZone,
        DeepGrandReef,
        Dunes,
        FloatingIsland,
        GhostTree,
        GrandReef,
        GrassyPlateaus,
        InactiveLavaZone,
        JellyshroomCaves,
        Kelp,
        KooshZone,
        LostRiverCorridor,
        LostRiverJunction,
        Mesas,
        Mountains,
        MushroomForest,
        PrisonAquarium,
        SafeShallows,
        SeaTreaderPath,
        ShipInterior,
        ShipSpecial,
        SkeletonCave,
        SparseReef,
        TreeCove,
        UnderwaterIslands
    }

    public static class BiomeTypeExtensions
    {
        /// <summary>
        /// Returns a hardcoded, rough approximation of the depth at which the biome in general becomes broadly
        /// accessible, i.e. comfortably explorable.
        /// </summary>
        /// <returns>The accessible depth.</returns>
        public static int GetAccessibleDepth(this BiomeType biomeType)
        {
            switch (biomeType)
            {
                case (BiomeType.ActiveLavaZone):
                    return 1400;
                case (BiomeType.BloodKelp):
                    return 250;
                case (BiomeType.BonesField):
                    return 650;
                case (BiomeType.Canyon):
                    return 600;
                case (BiomeType.CragField):
                    return 200;
                case (BiomeType.CrashZone):
                    return 50;
                case (BiomeType.DeepGrandReef):
                    return 500;
                case (BiomeType.Dunes):
                    return 200;
                case (BiomeType.FloatingIsland):
                    return 0;
                case (BiomeType.GhostTree):
                    return 900;
                case (BiomeType.GrandReef):
                    return 300;
                case (BiomeType.GrassyPlateaus):
                    return 100;
                case (BiomeType.InactiveLavaZone):
                    return 1000;
                case (BiomeType.JellyshroomCaves):
                    return 220;
                case (BiomeType.Kelp):
                    return 50;
                case (BiomeType.KooshZone):
                    return 250;
                case (BiomeType.LostRiverCorridor):
                    return 600;
                case (BiomeType.LostRiverJunction):
                    return 800;
                case (BiomeType.Mesas):
                    return 300;
                case (BiomeType.Mountains):
                    return 200;
                case (BiomeType.MushroomForest):
                    return 150;
                case (BiomeType.PrisonAquarium):
                    return 1700;
                case (BiomeType.SafeShallows):
                    return 0;
                case (BiomeType.SeaTreaderPath):
                    return 200;
                case (BiomeType.SkeletonCave):
                    return 650;
                case (BiomeType.SparseReef):
                    return 100;
                case (BiomeType.TreeCove):
                    return 900;
                case (BiomeType.UnderwaterIslands):
                    return 150;
                default:
                    return 0;
            }
        }
    }
}
