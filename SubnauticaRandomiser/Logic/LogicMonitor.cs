using System;
using SubnauticaRandomiser.Logic.LogicObjects;

namespace SubnauticaRandomiser.Logic
{
    /// <summary>
    /// Provides access to events that occur during randomisation.
    /// </summary>
    public class LogicMonitor
    {
        /// <summary>
        /// Triggered during the setup stage as every <see cref="LogicEntity"/> is randomly assigned its priority in
        /// the logic. This event can be used to override individual entities' priorities, e.g. to force certain QoL
        /// items to appear relatively early.
        /// </summary>
        public Action<LogicEntity> PrioritySetup;
    }
}