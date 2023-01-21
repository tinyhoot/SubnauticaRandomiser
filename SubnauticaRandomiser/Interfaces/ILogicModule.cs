using HarmonyLib;
using SubnauticaRandomiser.Objects;

namespace SubnauticaRandomiser.Interfaces
{
    /// <summary>
    /// The baseline which every module besides the core must implement. 
    /// </summary>
    internal interface ILogicModule
    {
        /// <summary>
        /// If the module makes changes to the game which do <em>not</em> rely on Harmony but still require storing
        /// in the serializer (like recipe changes), do it here. Executed after either running through the main logic
        /// or loading a saved state.
        /// </summary>
        public void ApplySerializedChanges(EntitySerializer serializer);
        
        /// <summary>
        /// Randomise anything which does not require use of the main loop. This method is called before the main loop
        /// is run.
        /// </summary>
        /// <param name="serializer">The serialisation instance used for this seed.</param>
        public void RandomiseOutOfLoop(EntitySerializer serializer);
        
        /// <summary>
        /// Attempt to randomise the given entity. The implementing class will only receive entities of the type(s)
        /// for which it registered itself as handler. If no handler was registered, this method is never called.
        /// </summary>
        /// <returns>True if successful, false if not.</returns>
        public bool RandomiseEntity(ref LogicEntity entity);
        
        /// <summary>
        /// If the module needs to register any patches with Harmony, do it in this method.
        /// </summary>
        /// <param name="harmony">The main harmony instance of this mod.</param>
        public void SetupHarmonyPatches(Harmony harmony);
    }
}