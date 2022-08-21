using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SubnauticaRandomiser.Logic.Recipes;
using SubnauticaRandomiser.RandomiserObjects;

namespace SubnauticaRandomiser.Logic
{
    /// <summary>
    /// Acts as the core for handling all randomising logic in the mod, and turning modules on/off as needed.
    /// </summary>
    public class CoreLogic
    {
        internal readonly RandomiserConfig _config;
        internal readonly List<Databox> _databoxes;
        internal readonly EntitySerializer _masterDict;
        internal readonly Materials _materials;
        internal readonly System.Random _random;
        internal readonly SpoilerLog _spoilerLog;
        internal readonly ProgressionTree _tree;

        private readonly AlternateStartLogic _altStartLogic;
        private readonly DataboxLogic _databoxLogic;
        private readonly FragmentLogic _fragmentLogic;
        private readonly RecipeLogic _recipeLogic;

        public CoreLogic(System.Random random, RandomiserConfig config, List<LogicEntity> allMaterials,
            Dictionary<EBiomeType, List<float[]>> alternateStarts, List<BiomeCollection> biomes = null,
            List<Databox> databoxes = null)
        {
            _config = config;
            _databoxes = databoxes;
            _masterDict = new EntitySerializer();
            _materials = new Materials(allMaterials);
            _random = random;
            _spoilerLog = new SpoilerLog(config, _masterDict);
            
            if (!_config.sSpawnPoint.StartsWith("Vanilla"))
                _altStartLogic = new AlternateStartLogic(this, alternateStarts);
            if (_config.bRandomiseDataboxes)
                _databoxLogic = new DataboxLogic(this);
            if (_config.bRandomiseFragments || _config.bRandomiseNumFragments || _config.bRandomiseDuplicateScans)
                _fragmentLogic = new FragmentLogic(this, biomes);
            if (_config.bRandomiseRecipes)
                _recipeLogic = new RecipeLogic(this);
            _tree = new ProgressionTree();
        }

        /// <summary>
        /// Set up all the necessary structures for later.
        /// </summary>
        private void Setup(List<LogicEntity> notRandomised)
        {
            _altStartLogic?.Randomise();

            if (_databoxLogic != null)
            {
                // Just randomise those flat out for now, instead of including them in the core loop.
                _databoxLogic.RandomiseDataboxes();
                LinkCyclopsHullModules();
            }

            if (_fragmentLogic != null)
            {
                if (_config.bRandomiseFragments)
                {
                    // Initialise the fragment cache and remove vanilla spawns.
                    FragmentLogic.Init();
                    // Queue up all fragments to be randomised.
                    notRandomised.AddRange(_materials.GetAllFragments());
                }
                
                // Randomise the number of fragment scans required per blueprint.
                if (_config.bRandomiseNumFragments)
                    _fragmentLogic.RandomiseNumFragments(_materials.GetAllFragments());
                
                // Randomise duplicate scan rewards.
                if (_config.bRandomiseDuplicateScans)
                    _fragmentLogic.CreateDuplicateScanYieldDict();
            }
            
            if (_recipeLogic != null)
            {
                _recipeLogic.UpdateReachableMaterials(0);
                // Queue up all craftables to be randomised.
                notRandomised.AddRange(_materials.GetAllCraftables());
                
                // Init the progression tree.
                _tree.SetupVanillaTree();
                if (_config.bVanillaUpgradeChains)
                    _tree.ApplyUpgradeChainToPrerequisites(_materials.GetAll());
            }
        }
        
        /// <summary>
        /// Start the randomisation process.
        /// </summary>
        /// <returns>A serialisation instance containing all changes made.</returns>
        /// <exception cref="TimeoutException">Raised to prevent infinite loops if the core loop takes too long to find
        /// a valid solution.</exception>
        internal EntitySerializer Randomise()
        {
            LogHandler.Info("Randomising using logic-based system...");
            
            List<LogicEntity> notRandomised = new List<LogicEntity>();
            Dictionary<TechType, bool> unlockedProgressionItems = new Dictionary<TechType, bool>();

            // Set up basic structures.
            Setup(notRandomised);

            int circuitbreaker = 0;
            int currentDepth = 0;
            int numProgressionItems = unlockedProgressionItems.Count;
            while (notRandomised.Count > 0)
            {
                circuitbreaker++;
                if (circuitbreaker > 3000)
                {
                    LogHandler.MainMenuMessage("Failed to randomise items: stuck in infinite loop!");
                    LogHandler.Fatal("Encountered infinite loop, aborting!");
                    throw new TimeoutException("Encountered infinite loop while randomising!");
                }
                
                // Update depth and reachable materials.
                currentDepth = UpdateReachableDepth(currentDepth, unlockedProgressionItems, numProgressionItems);
                numProgressionItems = unlockedProgressionItems.Count;

                LogicEntity nextEntity = ChooseNextEntity(notRandomised, currentDepth);

                // Choose a logic appropriate to the entity.
                if (nextEntity.IsFragment)
                {
                    if (_config.bRandomiseFragments && _fragmentLogic != null)
                        _fragmentLogic.RandomiseFragment(nextEntity, currentDepth);

                    notRandomised.Remove(nextEntity);
                    nextEntity.InLogic = true;
                    continue;
                }

                if (nextEntity.HasRecipe)
                {
                    bool success = _recipeLogic.RandomiseRecipe(nextEntity, unlockedProgressionItems, currentDepth);
                    if (success)
                    {
                        notRandomised.Remove(nextEntity);
                        nextEntity.InLogic = true;
                    }

                    continue;
                }
                
                LogHandler.Warn("Unsupported entity in loop: " + nextEntity);
            }

            LogHandler.Info($"Finished randomising within {circuitbreaker} cycles!");
            _spoilerLog.WriteLog();

            return _masterDict;
        }

        /// <summary>
        /// Get the next entity to be randomised, prioritising essential or elective ones.
        /// </summary>
        /// <returns>The next entity.</returns>
        private LogicEntity ChooseNextEntity(List<LogicEntity> notRandomised, int depth)
        {
            // Make sure the list of absolutely essential items is done first, for each depth level. This guarantees
            // certain recipes are done by a certain depth, e.g. waterparks by 500m.
            // Automatically fails if recipes do not get randomised.
            LogicEntity next = _recipeLogic?.GetPriorityEntity(depth);
            next ??= GetRandom(notRandomised);

            return next;
        }

        /// <summary>
        /// This function calculates the maximum reachable depth based on what vehicles the player has attained, as well
        /// as how much further they can go "on foot"
        /// </summary>
        /// <param name="progressionItems">A list of all currently reachable items relevant for progression.</param>
        /// <param name="depthTime">The minimum time that it must be possible to spend at the reachable depth before
        /// resurfacing.</param>
        /// <returns>The reachable depth.</returns>
        internal int CalculateReachableDepth(Dictionary<TechType, bool> progressionItems, int depthTime = 15)
        {
            const double swimmingSpeed = 4.7;  // Always assume that the player is holding a tool.
            const double seaglideSpeed = 11.0;
            bool seaglide = progressionItems.ContainsKey(TechType.Seaglide);
            double finSpeed = 0.0;
            int vehicleDepth = 0;
            Dictionary<TechType, double[]> tanks = new Dictionary<TechType, double[]>
            {
                { TechType.Tank, new[] { 75, 0.4 } },  // Tank type, oxygen, weight factor.
                { TechType.DoubleTank, new[] { 135, 0.47 } },
                { TechType.HighCapacityTank, new[] { 225, 0.6 } },
                { TechType.PlasteelTank, new[] { 135, 0.1 } }
            };

            LogHandler.Debug("===== Recalculating reachable depth =====");

            // Get the deepest depth that can be reached by vehicle.
            foreach (EProgressionNode node in EProgressionNodeExtensions.AllDepthNodes)
            {
                foreach (TechType[] path in _tree?.GetProgressionPath(node)?.Pathways ?? Enumerable.Empty<TechType[]>())
                {
                    if (CheckDictForAllTechTypes(progressionItems, path))
                        vehicleDepth = Math.Max(vehicleDepth, (int)node);
                }
            }

            if (progressionItems.ContainsKey(TechType.Fins))
                finSpeed = 1.41;
            if (progressionItems.ContainsKey(TechType.UltraGlideFins))
                finSpeed = 1.88;

            // How deep can the player go without any tanks?
            double soloDepthRaw = (45 - depthTime) * (seaglide ? seaglideSpeed : swimmingSpeed + finSpeed) / 2;

            // How deep can they go with tanks?
            foreach (var kv in tanks)
            {
                if (progressionItems.ContainsKey(kv.Key))
                {
                    // Value[0] is the oxygen granted by the tank, Value[1] its weight factor.
                    double depth = (kv.Value[0] - depthTime)
                        * (seaglide ? seaglideSpeed : swimmingSpeed + finSpeed - kv.Value[1]) / 2;
                    soloDepthRaw = Math.Max(soloDepthRaw, depth);
                }
            }

            // Given everything above, calculate the total.
            int totalDepth = CalculateTotalDepth(progressionItems, vehicleDepth, (int)soloDepthRaw);
            
            LogHandler.Debug("===== New reachable depth: " + totalDepth + " =====");

            return totalDepth;
        }

        /// <summary>
        /// Calculate the depth that can be comfortably reached on foot.
        /// </summary>
        /// <param name="vehicleDepth">The depth reachable by vehicle.</param>
        /// <param name="soloDepthRaw">The raw depth reachable on foot given no depth restrictions.</param>
        /// <returns>The depth that can be covered on foot in addition to the depth reachable by vehicle.</returns>
        private int CalculateSoloDepth(int vehicleDepth, int soloDepthRaw)
        {
            // Ensure that a number stays between a lower and upper bound. (e.g. 0 < x < 100)
            double limit(double x, double upperBound) => Math.Max(0, Math.Min(x, upperBound));
            // Calculate how much of the 0-100m and 100-200m range is already covered by vehicles.
            double[] vehicleDepths = { limit(vehicleDepth, 100), limit(vehicleDepth - 100, 100) };
            double[] soloDepths =
            {
                limit(soloDepthRaw, 100 - vehicleDepths[0]),
                limit(soloDepthRaw + vehicleDepths[0] - 100, 100 - vehicleDepths[1]),
                limit(soloDepthRaw + vehicleDepths[1] - 200, 10000)
            };
            
            // Below 100 meters, air is consumed three times as fast.
            // Below 200 meters, it is consumed five times as fast.
            return (int)(soloDepths[0] + (soloDepths[1] / 3) + (soloDepths[2] / 5));
        }

        /// <summary>
        /// Calculate the total depth that can be reached given the available equipment.
        /// </summary>
        /// <param name="progressionItems">The unlocked progression items.</param>
        /// <param name="vehicleDepth">The depth reachable by vehicle.</param>
        /// <param name="soloDepthRaw">The raw depth reachable on foot given no oxygen restrictions.</param>
        /// <returns>The total depth coverable by extending vehicle depth with a solo journey.</returns>
        private int CalculateTotalDepth(Dictionary<TechType, bool> progressionItems, int vehicleDepth, int soloDepthRaw)
        {
            // If there is a rebreather, all the funky calculations are redundant.
            if (progressionItems.ContainsKey(TechType.Rebreather))
                return vehicleDepth + Math.Min(soloDepthRaw, _config.iMaxDepthWithoutVehicle);

            return vehicleDepth + Math.Min(CalculateSoloDepth(vehicleDepth, soloDepthRaw), _config.iMaxDepthWithoutVehicle);
        }

        /// <summary>
        /// Update the depth that can be reached and trigger any changes that need to happen if a new significant
        /// threshold has been passed.
        /// </summary>
        /// <param name="currentDepth">The previously reachable depth.</param>
        /// <param name="progressionItems">The unlocked progression items.</param>
        /// <param name="numItems">The number of progression items on the previous cycle.</param>
        /// <returns>The new maximum depth.</returns>
        private int UpdateReachableDepth(int currentDepth, Dictionary<TechType, bool> progressionItems, int numItems)
        {
            if (progressionItems.Count <= numItems)
                return currentDepth;
            
            int newDepth = CalculateReachableDepth(progressionItems, _config.iDepthSearchTime);
            _spoilerLog.UpdateLastProgressionEntry(newDepth);
            currentDepth = Math.Max(currentDepth, newDepth);
            _recipeLogic?.UpdateReachableMaterials(currentDepth);

            return currentDepth;
        }
        
        /// <summary>
        /// Cyclops hull modules are linked and unlock together once the blueprint for module1 is found. Do the work
        /// for module1 and synchronise them.
        /// </summary>
        /// <exception cref="InvalidDataException">If the LogicEntity or databox for one of the hull modules cannot be
        /// found.</exception>
        private void LinkCyclopsHullModules()
        {
            if (!(_databoxes?.Count > 0))
            {
                LogHandler.Debug("Skipped linking Cyclops Hull Modules: Databoxes not randomised.");
                return;
            }

            LogicEntity mod1 = _materials.Find(TechType.CyclopsHullModule1);
            LogicEntity mod2 = _materials.Find(TechType.CyclopsHullModule2);
            LogicEntity mod3 = _materials.Find(TechType.CyclopsHullModule3);

            if (mod1 is null || mod2 is null || mod3 is null)
                throw new InvalidDataException("Tried to link Cyclops Hull Modules, but found null.");
            
            int total = 0;
            int number = 0;
            int lasercutter = 0;
            int propulsioncannon = 0;

            foreach (Databox box in _databoxes.FindAll(x => x.TechType.Equals(mod1.TechType)))
            {
                total += (int)Math.Abs(box.Coordinates.y);
                number++;

                if (box.RequiresLaserCutter)
                    lasercutter++;
                if (box.RequiresPropulsionCannon)
                    propulsioncannon++;
            }
            
            if (number == 0)
                throw new InvalidDataException("Entity " + this + " requires a databox, but 0 were found!");

            mod1.Blueprint.UnlockDepth = total / number;
            mod2.Blueprint.UnlockDepth = total / number;
            mod3.Blueprint.UnlockDepth = total / number;

            if (lasercutter / number >= 0.5)
            {
                mod1.Blueprint.UnlockConditions.Add(TechType.LaserCutter);
                mod2.Blueprint.UnlockConditions.Add(TechType.LaserCutter);
                mod3.Blueprint.UnlockConditions.Add(TechType.LaserCutter);
            }

            if (propulsioncannon / number >= 0.5)
            {
                mod1.Blueprint.UnlockConditions.Add(TechType.PropulsionCannon);
                mod2.Blueprint.UnlockConditions.Add(TechType.PropulsionCannon);
                mod3.Blueprint.UnlockConditions.Add(TechType.PropulsionCannon);
            }

            mod1.Blueprint.WasUpdated = true;
            mod2.Blueprint.WasUpdated = true;
            mod3.Blueprint.WasUpdated = true;
            
            LogHandler.Debug("Linked Cyclops Hull Modules.");
        }

        /// <summary>
        /// Check whether all TechTypes given in the array are present in the given dictionary.
        /// </summary>
        /// <param name="dict">The dictionary to check.</param>
        /// <param name="types">The array of TechTypes.</param>
        /// <returns>True if all TechTypes are present in the dictionary, false otherwise.</returns>
        private static bool CheckDictForAllTechTypes(Dictionary<TechType, bool> dict, TechType[] types)
        {
            bool allItemsPresent = true;

            foreach (TechType t in types)
            {
                allItemsPresent &= dict.ContainsKey(t);
                if (!allItemsPresent)
                    break;
            }

            return allItemsPresent;
        }

        /// <summary>
        /// Check wether any of the given TechTypes have already been randomised.
        /// </summary>
        /// <param name="masterDict">The master dictionary.</param>
        /// <param name="types">The TechTypes.</param>
        /// <returns>True if any TechType in the array has been randomised, false otherwise.</returns>
        public bool ContainsAny(EntitySerializer masterDict, TechType[] types)
        {
            foreach (TechType type in types)
            {
                if (masterDict.RecipeDict.ContainsKey(type))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Get a random element from a list.
        /// </summary>
        public T GetRandom<T>(List<T> list)
        {
            if (list == null || list.Count == 0)
            {
                return default(T);
            }

            return list[_random.Next(0, list.Count)];
        }
    }
}
