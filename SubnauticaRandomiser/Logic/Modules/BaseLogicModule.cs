using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HarmonyLib;
using HootLib.Interfaces;
using SubnauticaRandomiser.Configuration;
using SubnauticaRandomiser.Interfaces;
using SubnauticaRandomiser.Logic.LogicObjects;
using SubnauticaRandomiser.Serialization;
using SubnauticaRandomiser.Serialization.Modules;

namespace SubnauticaRandomiser.Logic.Modules
{
    /// <summary>
    /// Provides a baseline for features that modify specific aspects of the game.
    /// </summary>
    internal abstract class BaseLogicModule
    {
        protected Config _config;
        protected ILogHandler _log;
        protected LogicMonitor _monitor;

        /// <summary>
        /// Specific which types of <see cref="LogicEntity"/> this module is equipped to handle.
        /// <see cref="RandomiseEntity"/> will only receive entities of the provided types.
        /// </summary>
        public abstract Type HandledEntityType { get; }

        public abstract string LogPrefix { get; }

        /// <summary>
        /// A parameterless constructor must exist for the reflection-based instantiation to be relatively painless.
        /// Treat this method as a setup function similar to Awake() in unity components.
        /// <br />
        /// Setup that is used solely during randomisation should be done during <see cref="PrepareRandomisation"/>
        /// instead.
        /// </summary>
        internal virtual void OnRegisterModule(Config config, ILogHandler logger, LogicMonitor monitor)
        {
            _config = config;
            _log = logger;
            _monitor = monitor;
        }

        /// <summary>
        /// If the module requires any kind of external file to be able to randomise, register a task responsible for
        /// loading this critical data. Randomising is guaranteed to wait until all of these tasks have completed.
        /// </summary>
        public virtual IEnumerable<Task> LoadFilesAsync()
        {
            return Enumerable.Empty<Task>();
        }

        /// <summary>
        /// If the module requires any kind of save data initialise it here. If no data is required simply return
        /// null instead.
        /// </summary>
        public virtual BaseModuleSaveData SetupSaveData()
        {
            return null;
        }

        /// <summary>
        /// Perform setup necessary for the module to function properly during randomisation. Only called if randomising
        /// is actually necessary, i.e. only during first load of a new game.
        /// </summary>
        public virtual void PrepareRandomisation(EntityManager manager)
        {
        }

        /// <summary>
        /// Randomise any non-entity data that does not require use of the main loop. Executed before the main loop
        /// starts, i.e. before any entities have been randomised.
        /// </summary>
        /// <param name="rng">The random number generator of this seed.</param>
        /// <param name="saveData">The save data used for this seed.</param>
        public virtual void PreEntityRandomisation(IRandomHandler rng, SaveData saveData)
        {
        }

        /// <summary>
        /// Randomise the provided entity. This module will only receive entities of a type that the module has
        /// registered itself as a handler for via <see cref="HandledEntityTypes"/>.
        /// </summary>
        /// <param name="rng">The random number generator of this seed.</param>
        /// <param name="saveData">The save data used for this seed.</param>
        /// <param name="entity">The entity to be randomised.</param>
        public virtual void RandomiseEntity(IRandomHandler rng, SaveData saveData, LogicEntity entity)
        {
        }

        /// <summary>
        /// Randomise any non-entity data that does not require use of the main loop. Executed after the main loop has
        /// completed and all entities are randomised.
        /// </summary>
        /// <param name="rng">The random number generator of this seed.</param>
        /// <param name="saveData">The save data used for this seed.</param>
        public virtual void PostEntityRandomisation(IRandomHandler rng, SaveData saveData)
        {
        }

        /// <summary>
        /// If the module needs to register any patches with Harmony do it in this method.
        /// </summary>
        /// <param name="harmony">The harmony instance to register patches with.</param>
        /// <param name="saveData">The complete save data for the current game.</param>
        public abstract void RegisterHarmonyPatches(Harmony harmony, SaveData saveData);

        /// <summary>
        /// If the module makes changes to the game which do <em>not</em> rely on Harmony but still require serialised
        /// data (like e.g. recipe changes) register them here. Keep track of any changes you make, as the module is
        /// also responsible for reversing them in <see cref="UndoSerializedState"/>.
        /// </summary>
        /// <seealso cref="HootLib.Objects.NautilusShell"/>
        public abstract void ApplySerializedState(SaveData saveData);

        /// <summary>
        /// On return to the main menu the randomiser cleans the slate for the next game. If the module previously made
        /// any changes to the game in <see cref="ApplySerializedState"/> it must undo them here. This does not include
        /// harmony patches, as those can be reversed very easily without additional input.
        /// </summary>
        /// <seealso cref="HootLib.Objects.NautilusShell"/>
        public abstract void UndoSerializedState(SaveData saveData);
    }
}