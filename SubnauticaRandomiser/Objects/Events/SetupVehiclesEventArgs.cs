using System.Collections.Generic;

namespace SubnauticaRandomiser.Objects.Events
{
    internal class SetupVehiclesEventArgs
    {
        public Dictionary<TechType[], int> VehicleDepths;

        public SetupVehiclesEventArgs()
        {
            VehicleDepths = new Dictionary<TechType[], int>();
        }

        public SetupVehiclesEventArgs(Dictionary<TechType[], int> vehicleDepths)
        {
            VehicleDepths = vehicleDepths;
        }
    }
}