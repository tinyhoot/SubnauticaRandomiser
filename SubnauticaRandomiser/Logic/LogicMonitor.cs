using System;
using SubnauticaRandomiser.Logic.LogicObjects;

namespace SubnauticaRandomiser.Logic
{
    /// <summary>
    /// Provides access to events that occur during randomisation.
    /// </summary>
    internal class LogicMonitor
    {
        /// <summary>
        /// Triggered during the setup stage as the initial state for <see cref="Sphere"/> zero is determined. Can
        /// be used to modify said state.
        /// </summary>
        public event Action<RandomisationContext> ContextCreated;
        
        /// <summary>
        /// Triggered during the setup stage as every <see cref="LogicEntity"/> is randomly assigned its priority in
        /// the logic. This event can be used to override individual entities' priorities, e.g. to force certain QoL
        /// items to appear relatively early.
        /// </summary>
        public event Action<LogicEntity> PrioritySetup;

        internal void TriggerContextCreated(RandomisationContext ctx)
        {
            ContextCreated?.Invoke(ctx);
        }

        internal void TriggerPrioritySetup(LogicEntity entity)
        {
            PrioritySetup?.Invoke(entity);
        }
    }
}