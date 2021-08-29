using HarmonyLib;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using xxHashSharp;
using Logger = QModManager.Utility.Logger;

namespace SubnauticaRandomizerContinued
{

    class SeedLockdown
    {
        public static string seedString;
        public static int seedIndex = 4; //Used for testing purposes so I could clearly see the seed changing when needed
        public static Dictionary<string, string> slotSeedsCache = new Dictionary<string, string>(20); //Storing seeds found in savegames

        // These locks control the seed lockdown so that we don't constantly re-initialize a seed when generating new things, but only when moving into new areas.
        public static bool isLocked = false;
        public static bool isEntitySlotLocked = false;

        /* 
         * It's not enough to just lock down the game's seed, because the order the player explores changes the number of calls to the generator
         * and subsequently changes future spawn results. In other words, even if two players are using "staticseed", they may not find moonpool
         * fragments at the same place, if one beelines it to that place and the other goes somewhere else first.
         * 
         * This function gives the rest of the code an easy way to combine the save's seed with information about the location being randomized. In this way,
         * the PRNG can be initialized consistently for any given region, despite the difference in order.
         */
        public static int GetSeed(params string[] inputs)
        {
            string combinedSeed = inputs.Join();
            byte[] seedBytes = Encoding.ASCII.GetBytes(combinedSeed);
            return (int)xxHash.CalculateHash(seedBytes);
        }

        /* 
         * I don't fully recall at this point what is what, but these three (EntitySlotsPlaceholder, PrefabPlaceholdersGroup, and SpawnVirtualEntities)
         * seemed to be all necessary at some point along the way in order to appropriate lock things down.
         * 
         * I think sometimes one spawns the other, and therefore you can't just settle on one.
         * 
         * Based on this code and a faint recollection triggered by it, I'm fairly certain that EntitySlots are occasionally hand-placed, while often
         * they're initialized by the Placeholders. That's why the EntitySlots might trigger a lockdown themselves - only when they AREN'T inside of
         * a Placeholder.
         */
        [HarmonyPatch(typeof(EntitySlotsPlaceholder))]
        [HarmonyPatch("Spawn")]
        internal class EntitySlotsPlaceholder_Spawn_Patch
        {
            [HarmonyPrefix]
            public static void Prefix(EntitySlotsPlaceholder __instance, out Random.State __state)
            {
                __state = Random.state;
                if (seedString == null || seedString.Length == 0) return;
                int seed = GetSeed(seedString, __instance.transform.position.ToString());
                Random.InitState(seed);
                // Once we start locking down based on the location of this placeholder, everything in it should go in the same order and therefore we don't want to keep resetting
                isLocked = true; 
                //Logger.Log(Logger.Level.Debug, "Replaced seed with location- " + __instance.transform.position + "-dependent value for spawning EntitySlotsPlaceholder.");
            }

            [HarmonyPostfix]
            public static void Postfix(Random.State __state)
            {
                if (seedString == null || seedString.Length == 0) return;
                Random.state = __state;
                // After the placeholder is initialized, we allow the RNG seed to be locked down by the next group.
                isLocked = false;
                //Logger.Log(Logger.Level.Debug, "Restored seed with original state after spawning EntitySlotsPlaceholder.");
            }
        }

        [HarmonyPatch(typeof(PrefabPlaceholdersGroup))]
        [HarmonyPatch("Spawn")]
        internal class PrefabPlaceholdersGroup_Spawn_Patch
        {
            [HarmonyPrefix]
            public static void Prefix(PrefabPlaceholdersGroup __instance, out Random.State __state)
            {
                __state = Random.state;
                if (seedString == null || seedString.Length == 0) return;
                int seed = GetSeed(seedString, __instance.transform.position.ToString());
                Random.InitState(seed);
                // Once we start locking down based on the location of this placeholder, everything in it should go in the same order and therefore we don't want to keep resetting
                isLocked = true;
                //Logger.Log(Logger.Level.Debug, "Replaced seed with location- " + __instance.transform.position + "-dependent value for spawning PrefabPlaceholdersGroup.");
            }

            [HarmonyPostfix]
            public static void Postfix(Random.State __state)
            {
                if (seedString == null || seedString.Length == 0) return;
                Random.state = __state;
                // After the placeholder is initialized, we allow the RNG seed to be locked down by the next group.
                isLocked = false;
                //Logger.Log(Logger.Level.Debug, "Restored seed with original state after spawning PrefabPlaceholdersGroup.");
            }
        }

        [HarmonyPatch(typeof(EntitySlot))]
        [HarmonyPatch("SpawnVirtualEntities")]
        internal class EntitySlot_SpawnVirtualEntities_Patch
        {
            [HarmonyPrefix]
            public static void Prefix(EntitySlot __instance, out Random.State __state)
            {
                __state = Random.state;
                if (seedString == null || seedString.Length == 0 || isLocked) return;
                int seed = GetSeed(seedString, __instance.transform.position.ToString());
                Random.InitState(seed);
                // Once we start locking down based on the location of this slot, everything in it should go in the same order and therefore we don't want to keep resetting
                isEntitySlotLocked = true;
                //Logger.Log(Logger.Level.Debug, "Replaced seed with location- " + __instance.transform.position + "-dependent value for spawning EntitySlot.");
            }

            [HarmonyPostfix]
            public static void Postfix(Random.State __state)
            {
                if (seedString == null || seedString.Length == 0 || !isEntitySlotLocked) return;
                Random.state = __state;
                // After the placeholder is initialized, we allow the RNG seed to be locked down by the next group.
                isEntitySlotLocked = false;
                //Logger.Log(Logger.Level.Debug, "Restored seed with original state after spawning entities via EntitySlot.");
            }
        }

        internal class SeededGameInfo : SaveLoadManager.GameInfo
        {
            public string seed;

            // Danger! This may have some risks if anything else is modifying the save file, not sure if that's easy to overcome.
            // I've lost the specific reason why I did this but I _believe_ I found it necessary to properly manage [de]serializing save data.
            public SeededGameInfo(SaveLoadManager.GameInfo info, string seed)
            {
                this.seed = seed;
                this.basePresent = info.basePresent;
                this.changeSet = info.changeSet;
                this.cyclopsPresent = info.cyclopsPresent;
                this.dateTicks = info.dateTicks;
                this.exosuitPresent = info.exosuitPresent;
                this.gameMode = info.gameMode;
                this.gameTime = info.gameTime;
                this.isFallback = info.isFallback;
                this.machineName = info.machineName;
                this.rocketPresent = info.rocketPresent;
                this.seamothPresent = info.seamothPresent;
                this.session = info.session;
                this.startTicks = info.startTicks;
                this.userName = info.userName;
                this.version = info.version;
            }
        }

        static Traverse SaveFile = new Traverse(typeof(SaveLoadManager.GameInfo)).Method("SaveFile", new[] { typeof(string), typeof(byte[]) });

        // Adjust the save data to keep the used seed. Safe to use and no effect if current game is not randomized.
        [HarmonyPatch(typeof(SaveLoadManager.GameInfo))]
        [HarmonyPatch("SaveIntoCurrentSlot")]
        internal class GameInfo_SaveIntoCurrentSlot_Patch
        {

            [HarmonyPostfix]
            public static void Postfix(SaveLoadManager.GameInfo info)
            {
                if (seedString == null || seedString.Length == 0) return;
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    SeededGameInfo seededInfo = new SeededGameInfo(info, seedString);
                    string value = JsonUtility.ToJson(seededInfo);
                    using (StreamWriter streamWriter = new StreamWriter(memoryStream, Encoding.UTF8))
                    {
                        streamWriter.WriteLine(value);
                    }
                    SaveFile.GetValue("gameinfo.json", memoryStream.ToArray());
                    //Logger.Log(Logger.Level.Debug, "Saved seed to GameInfo.");
                }
            }
        }

        // Read the seed used for a given saved game (safe to use and no effect on non-randomized games).
        [HarmonyPatch(typeof(SaveLoadManager))]
        [HarmonyPatch("RegisterSaveGame")]
        internal class GameInfo_LoadFromBytes_Patch
        {


            [HarmonyPostfix]
            public static void Postfix(string slotName, UserStorageUtils.LoadOperation loadOperation)
            {
                if (loadOperation.GetSuccessful())
                {
                    SeededGameInfo seededInfo;
                    byte[] rawData = null;
                    loadOperation.files.TryGetValue("gameinfo.json", out rawData);
                    if (rawData != null)
                    {
                        using (StreamReader streamReader = new StreamReader(new MemoryStream(rawData)))
                        {
                            seededInfo = JsonUtility.FromJson<SeededGameInfo>(streamReader.ReadToEnd());
                            if (seededInfo.seed != null && seededInfo.seed.Length > 0)
                            {
                                slotSeedsCache[slotName] = seededInfo.seed;
                                //Logger.Log(Logger.Level.Debug, "Found seed " + seededInfo.seed + " in GameInfo for " + slotName + ".");
                            }
                            else
                            {
                                //Logger.Log(Logger.Level.Debug, "No seed detected in GameInfo for " + slotName + ".");
                                return;
                            }
                        }
                    }
                }

            }
        }

        // Handle starting a new/loading into a saved game.
        [HarmonyPatch(typeof(uGUI_SceneLoading))]
        [HarmonyPatch("BeginAsyncSceneLoad")]
        internal class uGUI_SceneLoading_BeginAsyncSceneLoad_Patch
        {


            [HarmonyPostfix]
            public static void Prefix(string sceneName)
            {
                if (sceneName.Equals("Main")) // We're loading into gameplay
                {
                    if (!Utils.GetContinueMode()) // We're NOT picking up a save game
                    {
                        seedIndex += 1; // For testing, we increment the index on each new game so we can show that different seeds have different outcomes
                        // If we don't recompile the mod, then if we close SN and re-launch, the first new game should be identical to the prior launch's first new game
                        seedString = "staticseed" + seedIndex;
                        Random.InitState(GetSeed(seedString));
                        //Logger.Log(Logger.Level.Debug, "Applied seed " + seedString + " on start of new game.");
                    }
                    else // We are picking up a save game
                    {
                        string slotName = SaveLoadManager.main.GetCurrentSlot(); 
                        slotSeedsCache.TryGetValue(slotName, out string seed); // Check to see if we found our seed in the save data
                        if (seed != null) // We did, reapply our seed
                        {
                            seedString = seed;
                            Random.InitState(GetSeed(seedString));
                            //Logger.Log(Logger.Level.Debug, "Applied seed " + seedString + " upon loading Main scene for " + slotName + ".");
                        }
                        else // We didn't, make sure to clear out any seed string from a prior load
                        {
                            seedString = null;
                            //Logger.Log(Logger.Level.Debug, "No seed applied upon loading Main scene for " + slotName + ".");
                            return;
                        }
                    }
                }
            }
        }

        /* 
         * Frankly I'm not entirely sure why this needed to be explicitly handled, but I know that if we didn't account for this,
         * two players using the same seed would not be guaranteed to start in the same spot.
         * 
         * I call out the uncertainty because that implies there could be other things which won't be _identical_ for two players
         * on a same seed - things which happen prior to getting a random start point seem to be non-deterministic even with the
         * rest of this logic. However, it's equally possible that it's inconsequential stuff - maybe calculations for particle systems
         * or animations - that influence this.
         */
        [HarmonyPatch(typeof(RandomStart))]
        [HarmonyPatch(nameof(RandomStart.GetRandomStartPoint))]
        internal class RandomStart_GetRandomStartPoint_Patch
        {
            [HarmonyPrefix]
            public static void Prefix(out Random.State __state)
            {
                __state = Random.state;
                if (seedString == null || seedString.Length == 0) return;
                Random.InitState(GetSeed(seedString, "escapePod"));
                //Logger.Log(Logger.Level.Debug, "Replaced seed prior to determining lifepod start location.");
            }

            [HarmonyPostfix]
            public static void Postfix(Random.State __state)
            {
                if (seedString == null || seedString.Length == 0) return;
                Random.state = __state;
                //Logger.Log(Logger.Level.Debug, "Restored seed with original state after determining lifepod start location.");
            }
        }
    }
    
}
