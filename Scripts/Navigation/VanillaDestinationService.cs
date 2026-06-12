using Streets;
using UI.Guiders;

namespace VoogleRoute.Navigation
{
    internal static class VanillaDestinationService
    {
        internal static void SetMapDestination(Address address)
        {
            if (address == null)
                return;

            GuidersManager.SetGuiderTarget(address, DirectionGuiderType.Destination);
            ModLog.Info("Vanilla map destination set: " + address.ToFormattedString());
        }
    }
}
