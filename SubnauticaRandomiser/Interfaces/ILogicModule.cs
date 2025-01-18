using System.Collections.Generic;
using System.Threading.Tasks;
using HarmonyLib;
using SubnauticaRandomiser.Objects;
using SubnauticaRandomiser.Serialization;
using SubnauticaRandomiser.Serialization.Modules;

namespace SubnauticaRandomiser.Interfaces
{
    /// <summary>
    /// The baseline which every module besides the core must implement. 
    /// </summary>
    internal interface ILogicModule
    {
        /// <summary>
        /// If the module requires any kind of external file to be able to randomise, register a task responsible for
        /// loading this critical data. Randomising will only begin once all of these tasks have completed.
        /// </summary>
        public IEnumerable<Task> LoadFiles();
        
        /// <summary>
        /// If the module requires any kind of save data, initialise it here. If no data is required, simply return
        /// null instead.
        /// </summary>
        public BaseModuleSaveData SetupSaveData();
        
        /// <summary>
        /// If the module makes changes to the game which do <em>not</em> rely on Harmony but still require storing
        /// in the serializer (like recipe changes), do it here. Executed after either running through the main logic
        /// or loading a saved state.
        /// </summary>
        public void ApplySerializedChanges(SaveData saveData);

        /// <summary>
        /// After returning to the main menu the randomiser cleans the slate for the next save. If the module previously
        /// made any non-harmony changes to the game it must undo them here. This includes things like e.g. recipe
        /// changes or altered language lines.
        /// <br />
        /// </summary>
        /// <seealso cref="HootLib.Objects.NautilusShell"/>
        public void UndoSerializedChanges(SaveData saveData);

        /// <summary>
        /// Randomise anything which does not require use of the main loop. This method is called before the main loop
        /// is run.
        /// </summary>
        /// <param name="rng">The random number generator of this seed.</param>
        /// <param name="saveData">The save data used for this seed.</param>
        public void RandomiseOutOfLoop(IRandomHandler rng, SaveData saveData);
        
        /// <summary>
        /// Attempt to randomise the given entity. The implementing class will only receive entities of the type(s)
        /// for which it registered itself as handler. If no handler was registered, this method is never called.
        /// </summary>
        /// <param name="rng">The random number generator of this seed.</param>
        /// <param name="entity">The entity to be randomised.</param>
        /// <returns>True if successful, false if not.</returns>
        public bool RandomiseEntity(IRandomHandler rng, ref LogicEntity entity);
        
        /// <summary>
        /// If the module needs to register any patches with Harmony, do it in this method.
        /// </summary>
        /// <param name="harmony">The main harmony instance of this mod.</param>
        /// <param name="saveData">The existing, complete save data for this save/seed.</param>
        public void SetupHarmonyPatches(Harmony harmony, SaveData saveData);
    }
}