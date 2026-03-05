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
        public event Action<StartingState> StartingStateCreated;

        /// <summary>
        /// Triggered after an entity was successfully randomised.
        /// </summary>
        public event Action<LogicEntity> EntityRandomised;

        /// <summary>
        /// Triggered whenever a new <see cref="Sphere"/> is created during the main loop.
        /// </summary>
        public event Action<Sphere> SphereCreated;

        internal void TriggerStartingStateCreated(StartingState ctx)
        {
            StartingStateCreated?.Invoke(ctx);
        }

        internal void TriggerEntityRandomised(LogicEntity entity)
        {
            EntityRandomised?.Invoke(entity);
        }

        internal void TriggerSphereCreated(Sphere sphere)
        {
            SphereCreated?.Invoke(sphere);
        }
    }
}