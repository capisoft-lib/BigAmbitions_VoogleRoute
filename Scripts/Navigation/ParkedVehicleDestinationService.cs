namespace VoogleRoute.Navigation
{
    internal static class ParkedVehicleDestinationService
    {
        internal static bool TryNavigateToParkedVehicle()
        {
            if (!ParkedVehicleStore.HasParkedPosition)
                return false;

            WorldDestinationService.SetParkedVehicleDestination(ParkedVehicleStore.ParkedPosition);
            return true;
        }
    }
}
