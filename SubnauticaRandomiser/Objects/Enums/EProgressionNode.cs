namespace SubnauticaRandomiser.Objects.Enums
{
    public enum EProgressionNode
    {
        None = 9999, 
        Aurora = 5,
        Depth0m = 0,
        Depth100m = 100,
        Depth200m = 200,
        Depth300m = 300,
        Depth500m = 500,
        Depth900m = 900,
        Depth1300m = 1300,
        Depth1700m = 1700
    }

    public static class EProgressionNodeExtensions
    {
        public static EProgressionNode[] AllDepthNodes => new[]
        {
            EProgressionNode.Depth0m,
            EProgressionNode.Depth100m,
            EProgressionNode.Depth200m,
            EProgressionNode.Depth300m,
            EProgressionNode.Depth500m,
            EProgressionNode.Depth900m,
            EProgressionNode.Depth1300m,
            EProgressionNode.Depth1700m
        };
    }
}
