using UI;
using UnityEngine;
using VoogleRoute.Navigation;

namespace VoogleRoute
{
    /// <summary>Recovers from a stuck black overlay when no travel fade is expected.</summary>
    internal static class NavigationScreenRecovery
    {
        private const float StuckFadeSeconds = 4f;

        private static float _stuckSince = -1f;
        private static float _nextRecoveryAttempt = -1f;

        internal static void Tick()
        {
            if (!UiFader.isFading)
            {
                _stuckSince = -1f;
                return;
            }

            if (AutoDriveSkipTravelService.IsInProgress)
            {
                _stuckSince = -1f;
                return;
            }

            try
            {
                if (SubwaySystem.IsRiding)
                {
                    _stuckSince = -1f;
                    return;
                }
            }
            catch
            {
                // ignore
            }

            var now = Time.unscaledTime;
            if (_stuckSince < 0f)
                _stuckSince = now;

            if (now - _stuckSince < StuckFadeSeconds || now < _nextRecoveryAttempt)
                return;

            var host = VoogleRouteDriver.Instance;
            if (host == null)
                return;

            _nextRecoveryAttempt = now + 10f;
            _stuckSince = -1f;
            ModLog.Info("Recovering stuck UiFader (possible black screen).");
            host.StartCoroutine(UiFader.UnFade());
        }
    }
}
