namespace SubnauticaRandomiser.Objects.Enums
{
    /// <summary>
    /// Defines the rough regions in the game as any player might recognise them. Only loosely related to how the game
    /// handles biomes.
    /// </summary>
    public enum BiomeRegion
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

    public static class BiomeRegionExtensions
    {
        /// <summary>
        /// Returns a hardcoded, rough approximation of the depth at which the biome in general becomes broadly
        /// accessible, i.e. comfortably explorable.
        /// </summary>
        /// <returns>The accessible depth.</returns>
        public static int GetAccessibleDepth(this BiomeRegion biomeRegion)
        {
            switch (biomeRegion)
            {
                case (BiomeRegion.ActiveLavaZone):
                    return 1400;
                case (BiomeRegion.BloodKelp):
                    return 250;
                case (BiomeRegion.BonesField):
                    return 650;
                case (BiomeRegion.Canyon):
                    return 600;
                case (BiomeRegion.CragField):
                    return 200;
                case (BiomeRegion.CrashZone):
                    return 50;
                case (BiomeRegion.DeepGrandReef):
                    return 500;
                case (BiomeRegion.Dunes):
                    return 200;
                case (BiomeRegion.FloatingIsland):
                    return 0;
                case (BiomeRegion.GhostTree):
                    return 900;
                case (BiomeRegion.GrandReef):
                    return 300;
                case (BiomeRegion.GrassyPlateaus):
                    return 100;
                case (BiomeRegion.InactiveLavaZone):
                    return 1000;
                case (BiomeRegion.JellyshroomCaves):
                    return 220;
                case (BiomeRegion.Kelp):
                    return 50;
                case (BiomeRegion.KooshZone):
                    return 250;
                case (BiomeRegion.LostRiverCorridor):
                    return 600;
                case (BiomeRegion.LostRiverJunction):
                    return 800;
                case (BiomeRegion.Mesas):
                    return 300;
                case (BiomeRegion.Mountains):
                    return 200;
                case (BiomeRegion.MushroomForest):
                    return 150;
                case (BiomeRegion.PrisonAquarium):
                    return 1700;
                case (BiomeRegion.SafeShallows):
                    return 0;
                case (BiomeRegion.SeaTreaderPath):
                    return 200;
                case (BiomeRegion.SkeletonCave):
                    return 650;
                case (BiomeRegion.SparseReef):
                    return 100;
                case (BiomeRegion.TreeCove):
                    return 900;
                case (BiomeRegion.UnderwaterIslands):
                    return 150;
                default:
                    return 0;
            }
        }
    }
}
