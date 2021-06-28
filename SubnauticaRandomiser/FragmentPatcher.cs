using System;
using System.Collections.Generic;
using SMLHelper.V2.Handlers;
using static LootDistributionData;

namespace SubnauticaRandomiser
{
    public class FragmentPatcher
    {
        public FragmentPatcher()
        {
        }

        internal static void EditLootDistribution()
        {
            // This spawns a bunch of laser cutters, but fragments do not seem affected
            LogHandler.Debug("1, " + CraftData.GetClassIdForTechType(TechType.LaserCutter));
            //LootDistributionHandler.EditLootDistributionData(CraftData.GetClassIdForTechType(TechType.WorkbenchFragment), BiomeType.SafeShallows_Grass, (float)0.6, 20);
            LogHandler.Debug("2");
            LootDistributionHandler.EditLootDistributionData(CraftData.GetClassIdForTechType(TechType.LaserCutter), BiomeType.SafeShallows_ShellTunnel, (float)0.9, 20);
            LogHandler.Debug("3");
            // Does not seem to work. Wrong classId?
            // LootDistributionData.PrefabData p = new LootDistributionData.PrefabData();
            //List<BiomeData> dist = new List<BiomeData>();
            //BiomeData d = new BiomeData();
            //d.biome = BiomeType.SafeShallows_CaveFloor;
            //d.count = 20;
            //d.probability = (float)0.95;
            //dist.Add(d);

            //SrcData src = new SrcData();

            //UWE.WorldEntityInfo info = new UWE.WorldEntityInfo();
            //UWE.WorldEntityDatabase.TryGetInfo(CraftData.GetClassIdForTechType(TechType.LaserCutterFragment), out info);
            //LootDistributionHandler.AddLootDistributionData(CraftData.GetClassIdForTechType(TechType.WorkbenchFragment), dist, info,);


            // Look at dnSpy, TechFragment class. Seems like there's one overarching
            // class with an easy to change techType field? Intercept a function
            // and overwrite that before the prefab gets loaded?
            // TechFragment.GetRandom() seems like a good target?

            // BiomeType enum inexplicably has fragments in it?

            // *** HOW THINGS SEEM TO WORK ***
            /* When the game first loads a cell, it decides to populate it with
             * things, or not. This is when the loot distributor comes into play.
             * Most fragments have like a 4-10% spawn chance in very specific biomes,
             * like the sandy parts of the grassy plateaus. Afterwards, unless a
             * mod is used to change that, the game no longer spawns anything within
             * that cell. This is how the world can slowly become devoid of life
             * and resources as you keep hoovering up everything. 
             * 
             * Many fragments can spawn in many different biomes, and have different
             * distributions for that. However, those are mostly the random fragments
             * somewhere on the seafloor.
             * In addition, there's also biomes that are specifically named after
             * the fragment they support. This seems to be the very specific locations
             * where a specific fragment will ALWAYS spawn, such as within wrecks.
             *             
             * Setting the distribution of fragments in their vanilla locations to 0
             * works, and is an effective way to prevent unwanted spawns. However,
             * this has to be done for every single specific fragment prefab (of which
             * e.g. the moonpool has six).
             * 
             * The CustomizeYourSpawns mod has exposed all the spawn and biome IDs
             * in handy json format in its mod folder. Now how to convert those prefabs
             * to class ids which the function actually takes? No clue. But that mod
             * can do it.
             * 
             * As an aside, this same spawning system could also be used to mess with
             * the spawns of fish. Ghost leviathan in the shallows? Suddenly possible.            
             * 
             */
        }
    }
}

