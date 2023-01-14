using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using SubnauticaRandomiser.Interfaces;
using SubnauticaRandomiser.Logic.Recipes;
using SubnauticaRandomiser.Objects;
using SubnauticaRandomiser.Objects.Enums;
using UnityEngine;
using ILogHandler = SubnauticaRandomiser.Interfaces.ILogHandler;

namespace SubnauticaRandomiser.Logic
{
    /// <summary>
    /// Acts as the core for handling all randomising logic in the mod, and turning modules on/off as needed.
    /// </summary>
    internal class CoreLogic : MonoBehaviour
    {
        internal RandomiserConfig _Config { get; private set; }
        internal ILogHandler _Log { get; private set; }
        internal EntitySerializer _Serializer { get; private set; }
        internal readonly Materials _Materials;
        internal IRandomHandler _Random { get; private set; }
        internal readonly SpoilerLog _SpoilerLog;
        internal readonly ProgressionTree _Tree;

        private readonly AlternateStartLogic _altStartLogic;
        private readonly AuroraLogic _auroraLogic;
        private readonly DataboxLogic _databoxLogic;
        internal readonly FragmentLogic _fragmentLogic;
        private RecipeLogic _recipeLogic;

        private Dictionary<EntityType, ILogicModule> _entityHandlers;
        
        /// <summary>
        /// Invoked during the setup stage, before the main loop begins.
        /// </summary>
        public event EventHandler OnSetup;
        
        /// <summary>
        /// Invoked once the next entity to be randomised has been determined.
        /// </summary>
        public event EventHandler OnEntityChosen;

        /// <summary>
        /// Invoked whenever an entity has been successfully randomised and added to the logic.
        /// </summary>
        public event EventHandler OnEntityRandomised;

        /// <summary>
        /// Invoked once the main loop has successfully completed.
        /// </summary>
        public event EventHandler OnMainLoopComplete;
        
        public void Awake()
        {
            _entityHandlers = new Dictionary<EntityType, ILogicModule>();
            
            _Config = Initialiser._Config;
            _Log = Initialiser._Log;
            _Serializer = new EntitySerializer(_Log);
            _Random = new RandomHandler(_Config.iSeed);
        }

        public void Start()
        {
            
        }

        public CoreLogic(IRandomHandler random, RandomiserConfig config, ILogHandler logger, List<LogicEntity> allMaterials,
            Dictionary<EBiomeType, List<float[]>> alternateStarts, List<BiomeCollection> biomes = null,
            List<Databox> databoxes = null)
        {
            _Config = config;
            _Log = logger;
            _Serializer = new EntitySerializer(logger);
            _Materials = new Materials(allMaterials, logger);
            _Random = random;
            _SpoilerLog = new SpoilerLog(config, logger, _Serializer);
            
            if (!_Config.sSpawnPoint.StartsWith("Vanilla"))
                _altStartLogic = new AlternateStartLogic(alternateStarts, config, logger, random);
            _auroraLogic = new AuroraLogic(this);
            if (_Config.bRandomiseDataboxes)
                _databoxLogic = new DataboxLogic(this, databoxes);
            if (_Config.bRandomiseFragments || _Config.bRandomiseNumFragments || _Config.bRandomiseDuplicateScans)
                _fragmentLogic = new FragmentLogic(this, biomes);
            if (_Config.bRandomiseRecipes)
                _recipeLogic = new RecipeLogic(this);
            _Tree = new ProgressionTree();
        }

        /// <summary>
        /// Set up all the necessary structures for later.
        /// </summary>
        private void Setup(List<LogicEntity> notRandomised)
        {
            // Init the progression tree.
            _Tree.SetupVanillaTree();
            _altStartLogic?.Randomise(_Serializer);
            if (_Config.bRandomiseDoorCodes)
                _auroraLogic.RandomiseDoorCodes();
            if (_Config.bRandomiseSupplyBoxes)
                _auroraLogic.RandomiseSupplyBoxes();

            if (_databoxLogic != null)
            {
                // Just randomise those flat out for now, instead of including them in the core loop.
                _databoxLogic.RandomiseDataboxes();
                _databoxLogic.UpdateBlueprints(_Materials.GetAll());
                _databoxLogic.LinkCyclopsHullModules(_Materials);
            }

            if (_fragmentLogic != null)
            {
                if (_Config.bRandomiseFragments)
                {
                    _Tree.SetupFragments();
                    // Initialise the fragment cache and remove vanilla spawns.
                    FragmentLogic.Init();
                    // Queue up all fragments to be randomised.
                    notRandomised.AddRange(_Materials.GetAllFragments());
                }
                
                // Randomise the number of fragment scans required per blueprint.
                if (_Config.bRandomiseNumFragments)
                    _fragmentLogic.RandomiseNumFragments(_Materials.GetAllFragments());
                
                // Randomise duplicate scan rewards.
                if (_Config.bRandomiseDuplicateScans)
                    _fragmentLogic.CreateDuplicateScanYieldDict();
            }
            
            if (_recipeLogic != null)
            {
                _recipeLogic.UpdateReachableMaterials(0);
                // Queue up all craftables to be randomised.
                notRandomised.AddRange(_Materials.GetAllCraftables());
                
                // Update the progression tree with recipes.
                _Tree.SetupRecipes(_Config.bVanillaUpgradeChains);
                if (_Config.bVanillaUpgradeChains)
                    _Tree.ApplyUpgradeChainToPrerequisites(_Materials.GetAll());
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
            _Log.Info("Randomising using logic-based system...");
            
            List<LogicEntity> notRandomised = new List<LogicEntity>();
            Dictionary<TechType, bool> unlockedProgressionItems = new Dictionary<TechType, bool>();

            // Set up basic structures.
            Setup(notRandomised);

            int circuitbreaker = 0;
            int currentDepth = 0;
            int numProgressionItems = -1; // This forces a depth calculation on the first loop.
            while (notRandomised.Count > 0)
            {
                circuitbreaker++;
                if (circuitbreaker > 3000)
                {
                    _Log.InGameMessage("Failed to randomise items: stuck in infinite loop!");
                    _Log.Fatal("Encountered infinite loop, aborting!");
                    throw new TimeoutException("Encountered infinite loop while randomising!");
                }
                
                // Update depth and reachable materials.
                currentDepth = UpdateReachableDepth(currentDepth, unlockedProgressionItems, numProgressionItems);
                numProgressionItems = unlockedProgressionItems.Count;

                LogicEntity nextEntity = ChooseNextEntity(notRandomised, currentDepth);

                // Choose a logic appropriate to the entity.
                bool? success = null;
                if (nextEntity.IsFragment)
                    success = _fragmentLogic.RandomiseFragment(nextEntity, unlockedProgressionItems, currentDepth);
                else if (nextEntity.HasRecipe)
                    success = _recipeLogic.RandomiseRecipe(nextEntity, unlockedProgressionItems, currentDepth);
                
                if (success == true)
                {
                    notRandomised.Remove(nextEntity);
                    nextEntity.InLogic = true;
                }

                if (success is null)
                    _Log.Warn("Unsupported entity in loop: " + nextEntity);
            }

            _Log.Info($"Finished randomising within {circuitbreaker} cycles!");
            _SpoilerLog.WriteLog();

            return _Serializer;
        }

        /// <summary>
        /// Get the next entity to be randomised, prioritising essential or elective ones.
        /// </summary>
        /// <returns>The next entity.</returns>
        [NotNull]
        private LogicEntity ChooseNextEntity(List<LogicEntity> notRandomised, int depth)
        {
            // Make sure the list of absolutely essential items is done first, for each depth level. This guarantees
            // certain recipes are done by a certain depth, e.g. waterparks by 500m.
            // Automatically fails if recipes do not get randomised.
            LogicEntity next = GetPriorityEntity(depth);
            next ??= _Random.Choice(notRandomised);

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

            _Log.Debug("===== Recalculating reachable depth =====");

            // Get the deepest depth that can be reached by vehicle.
            foreach (EProgressionNode node in EProgressionNodeExtensions.AllDepthNodes)
            {
                foreach (TechType[] path in _Tree?.GetProgressionPath(node)?.Pathways ?? Enumerable.Empty<TechType[]>())
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
            
            _Log.Debug("===== New reachable depth: " + totalDepth + " =====");

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
                return vehicleDepth + Math.Min(soloDepthRaw, _Config.iMaxDepthWithoutVehicle);

            return vehicleDepth + Math.Min(CalculateSoloDepth(vehicleDepth, soloDepthRaw), _Config.iMaxDepthWithoutVehicle);
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
            
            int newDepth = CalculateReachableDepth(progressionItems, _Config.iDepthSearchTime);
            _SpoilerLog.UpdateLastProgressionEntry(newDepth);
            currentDepth = Math.Max(currentDepth, newDepth);
            _recipeLogic?.UpdateReachableMaterials(currentDepth);

            return currentDepth;
        }

        /// <summary>
        /// Get an essential or elective entity for the currently reachable depth, prioritising essential ones.
        /// </summary>
        /// <param name="depth">The maximum depth to consider.</param>
        /// <returns>A LogicEntity, or null if all have been processed already.</returns>
        [CanBeNull]
        private LogicEntity GetPriorityEntity(int depth)
        {
            List<TechType> essentialItems = _Tree.GetEssentialItems(depth);
            List<TechType[]> electiveItems = _Tree.GetElectiveItems(depth);
            LogicEntity entity = null;

            // Always get one of the essential items first, if available.
            if (essentialItems.Count > 0)
            {
                TechType type = essentialItems.Find(x =>
                    !_Serializer.RecipeDict.ContainsKey(x) && !_Serializer.SpawnDataDict.ContainsKey(x));
                
                if (!type.Equals(TechType.None))
                {
                    entity = _Materials.Find(type);
                    _Log.Debug($"Prioritising essential entity {entity} for depth {depth}");
                }
            }

            // Similarly, if all essential items are done, grab one from among the elective items and leave the rest
            // up to chance.
            if (entity is null && electiveItems.Count > 0)
            {
                TechType[] types = electiveItems.Find(arr => arr.All(x =>
                    !_Serializer.RecipeDict.ContainsKey(x) && !_Serializer.SpawnDataDict.ContainsKey(x)));
                
                if (types?.Length > 0)
                {
                    TechType nextType = _Random.Choice(types);
                    entity = _Materials.Find(nextType);
                    _Log.Debug($"Prioritising elective entity {entity} for depth {depth}");
                }
            }

            return entity;
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

        public void RegisterEntityHandler(EntityType type, ILogicModule module)
        {
            if (_entityHandlers.ContainsKey(type))
                throw new ArgumentException($"A handler for entity type '{type}' already exists: "
                                            + $"{_entityHandlers[type].GetType()}");
            
            _entityHandlers.Add(type, module);
        }

        /// <summary>
        /// Register a component module for use with the randomiser.
        /// </summary>
        /// <returns>The instantiated component.</returns>
        public T RegisterModule<T>() where T : MonoBehaviour, ILogicModule
        {
            return gameObject.EnsureComponent<T>();
        }
    }
}
