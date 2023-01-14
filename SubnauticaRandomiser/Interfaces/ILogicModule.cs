using HarmonyLib;
using SubnauticaRandomiser.Objects;

namespace SubnauticaRandomiser.Interfaces
{
    internal interface ILogicModule
    {
        /// <summary>
        /// Randomise anything which does not require use of the main loop. This method is called before the main loop
        /// is run.
        /// </summary>
        public void Randomise();
        
        /// <summary>
        /// Randomise the given entity. The implementing class will only receive entities of the type(s) for which it
        /// registered itself as handler. If no handler was registered, this method is never called.
        /// </summary>
        public LogicEntity RandomiseEntity(LogicEntity entity);
        
        /// <summary>
        /// If the module needs to register any patches with Harmony, do it in this method.
        /// </summary>
        /// <param name="harmony">The main harmony instance of this mod.</param>
        public void SetupHarmonyPatches(Harmony harmony);
    }
}