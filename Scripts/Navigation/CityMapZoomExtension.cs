using UnityEngine;

namespace VoogleRoute.Navigation
{
    /// <summary>
    /// Vanilla CityMapCam clamps zoom-out at minMaxDistance.y (350). Raise the cap so the city map (M) can show more area.
    /// </summary>
    internal static class CityMapZoomExtension
    {
        private const float ExtendedMaxZoomDistance = 1000f;

        private static bool _applied;

        internal static void Reset() => _applied = false;

        internal static void EnsureApplied()
        {
            try
            {
                if (!CityManager.IsInitialized)
                    return;

                var cam = CityManager.Instance?.cityMap?.cityMapCam;
                if (cam == null)
                    return;

                if (cam.minMaxDistance.y >= ExtendedMaxZoomDistance)
                {
                    _applied = true;
                    return;
                }

                cam.minMaxDistance = new Vector2(cam.minMaxDistance.x, ExtendedMaxZoomDistance);
                if (!_applied)
                    ModLog.Info("City map max zoom distance extended to " + ExtendedMaxZoomDistance + " (vanilla 350).");
                _applied = true;
            }
            catch (System.Exception ex)
            {
                ModLog.Error("Failed to extend city map zoom", ex);
            }
        }
    }
}
