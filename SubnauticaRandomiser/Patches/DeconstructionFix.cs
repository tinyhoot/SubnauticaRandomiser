using System.Collections.Generic;
using HarmonyLib;

namespace SubnauticaRandomiser.Patches
{
    // [HarmonyPatch]
    internal static class DeconstructionFix
    {
        private static readonly Dictionary<string, TechType> _corridors = new Dictionary<string, TechType>()
        {
            { "BaseCorridorIShape(Clone)", TechType.BaseCorridorI },
            { "BaseCorridorLShape(Clone)", TechType.BaseCorridorL },
            { "BaseCorridorTShape(Clone)", TechType.BaseCorridorT },
            { "BaseCorridorXShape(Clone)", TechType.BaseCorridorX },
            { "BaseCorridorIShapeGlass(Clone)", TechType.BaseCorridorGlassI },
            { "BaseCorridorLShapeGlass(Clone)", TechType.BaseCorridorGlassL }
        };

        /// <summary>
        /// Shaped corridors are falsely associated with their straight counterparts. Because their recipes can differ
        /// wildly, the difference can be crushing. This updates the recipe to what it should be on deconstruction.
        /// </summary>
        /// <param name="__instance">The base part that is being deconstructed.</param>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(BaseDeconstructable), nameof(BaseDeconstructable.Deconstruct))]
        internal static void FixCorridors(ref BaseDeconstructable __instance)
        {
            if (_corridors.ContainsKey(__instance.name))
                __instance.recipe = _corridors[__instance.name];
        }
    }
}