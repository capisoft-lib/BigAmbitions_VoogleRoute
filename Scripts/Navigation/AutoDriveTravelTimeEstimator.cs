using UnityEngine;

namespace VoogleRoute.Navigation
{
    /// <summary>Taxi travel time with configurable base multiplier (vanilla: distance × 0.04 min/m).</summary>
    internal static class AutoDriveTravelTimeEstimator
    {
        private const float TaxiMinutePerMeter = 0.04f;
        private const float MinTravelMinutes = 1f;

        internal static float EstimateMinutes(float distanceMeters)
        {
            var minutes = distanceMeters * TaxiMinutePerMeter * ModConfig.BaseTaxiMultiplier;
            minutes *= ResolveGameSpeed();
            return Mathf.Max(MinTravelMinutes, minutes);
        }

        private static float ResolveGameSpeed()
        {
            try
            {
                return PlayerPrefSettings.GameSpeed;
            }
            catch
            {
                return 1f;
            }
        }
    }
}
