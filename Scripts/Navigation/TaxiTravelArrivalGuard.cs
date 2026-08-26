using UnityEngine;
using VoogleRoute;

namespace VoogleRoute.Navigation
{
    /// <summary>
    /// Prevents a taxi warp from being followed by an immediate building interaction.
    /// One Click Taxi opens the vanilla taxi map, so this covers both vanilla and mod-opened rides.
    /// </summary>
    internal static class TaxiTravelArrivalGuard
    {
        private const float PostTaxiSuppressionSeconds = 3f;

        private static bool _taxiTravelActive;
        private static float _suppressBuildingAutoEnterUntil = -1f;

        internal static void OnTimeMachineStarted()
        {
            _taxiTravelActive = IsVanillaTaxiTravelActive();
            if (_taxiTravelActive)
                ModLog.Info("Taxi travel started; building auto-enter will be guarded on arrival.");
        }

        internal static void OnTimeMachineEnded()
        {
            if (!_taxiTravelActive && !IsVanillaTaxiTravelActive())
                return;

            _taxiTravelActive = false;
            _suppressBuildingAutoEnterUntil = Time.unscaledTime + PostTaxiSuppressionSeconds;
            ModLog.Info("Taxi travel completed; building auto-enter temporarily suppressed.");
        }

        internal static bool ShouldSuppressBuildingAutoEnter() =>
            Time.unscaledTime <= _suppressBuildingAutoEnterUntil;

        internal static void Reset()
        {
            _taxiTravelActive = false;
            _suppressBuildingAutoEnterUntil = -1f;
        }

        private static bool IsVanillaTaxiTravelActive()
        {
            try
            {
                return BigAmbitionsCompatibility.IsTaxiTravelActive();
            }
            catch
            {
                return false;
            }
        }
    }
}
