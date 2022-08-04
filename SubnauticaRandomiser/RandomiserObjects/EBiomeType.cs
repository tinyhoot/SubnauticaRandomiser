namespace SubnauticaRandomiser.RandomiserObjects
{
    public enum EBiomeType
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
        public static int GetAccessibleDepth(this EBiomeType biomeType)
        {
            switch (biomeType)
            {
                case (EBiomeType.ActiveLavaZone):
                    return 1400;
                case (EBiomeType.BloodKelp):
                    return 250;
                case (EBiomeType.BonesField):
                    return 650;
                case (EBiomeType.Canyon):
                    return 600;
                case (EBiomeType.CragField):
                    return 200;
                case (EBiomeType.CrashZone):
                    return 50;
                case (EBiomeType.DeepGrandReef):
                    return 500;
                case (EBiomeType.Dunes):
                    return 200;
                case (EBiomeType.FloatingIsland):
                    return 0;
                case (EBiomeType.GhostTree):
                    return 900;
                case (EBiomeType.GrandReef):
                    return 300;
                case (EBiomeType.GrassyPlateaus):
                    return 100;
                case (EBiomeType.InactiveLavaZone):
                    return 1000;
                case (EBiomeType.JellyshroomCaves):
                    return 220;
                case (EBiomeType.Kelp):
                    return 50;
                case (EBiomeType.KooshZone):
                    return 250;
                case (EBiomeType.LostRiverCorridor):
                    return 600;
                case (EBiomeType.LostRiverJunction):
                    return 800;
                case (EBiomeType.Mesas):
                    return 300;
                case (EBiomeType.Mountains):
                    return 200;
                case (EBiomeType.MushroomForest):
                    return 150;
                case (EBiomeType.PrisonAquarium):
                    return 1700;
                case (EBiomeType.SafeShallows):
                    return 25;
                case (EBiomeType.SeaTreaderPath):
                    return 200;
                case (EBiomeType.SkeletonCave):
                    return 650;
                case (EBiomeType.SparseReef):
                    return 100;
                case (EBiomeType.TreeCove):
                    return 900;
                case (EBiomeType.UnderwaterIslands):
                    return 150;
                default:
                    return 0;
            }
        }
    }
}
