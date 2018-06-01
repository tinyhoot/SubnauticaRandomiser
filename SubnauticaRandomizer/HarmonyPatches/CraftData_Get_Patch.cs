using Harmony;
using System;
using System.Reflection;

namespace SubnauticaRandomizer.HarmonyPatches
{
    [HarmonyPatch(typeof(CraftData))]
    [HarmonyPatch("Get")]
    class CraftData_Get_Patch
    {
        public static readonly Type TechDataType = typeof(CraftData).GetNestedType("TechData", BindingFlags.NonPublic);

        public static void Postfix(ref ITechData __result, ref TechType techType)
        {
            if (Settings.Instance.Recipes.RecipesByType.ContainsKey((int)techType))
            {
                __result = Settings.Instance.Recipes.RecipesByType[(int)techType];
            }
        }
    }
}
