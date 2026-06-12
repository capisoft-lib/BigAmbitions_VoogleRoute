using UnityEngine;

namespace VoogleRoute.Navigation
{
    internal static class AutoDriveDiagnostics
    {
        private static string _lastBlockReason = string.Empty;
        private static float _nextStatusLogAt;
        private static float _nextTickLogAt;
        private static bool _wasEnabled;

        internal static void OnEnabled()
        {
            _wasEnabled = true;
            _lastBlockReason = string.Empty;
            ModLog.Info("[AutoDrive] enabled by user");
        }

        internal static void OnDisabled(string reason)
        {
            if (!_wasEnabled && string.IsNullOrEmpty(reason))
                return;

            _wasEnabled = false;
            _lastBlockReason = string.Empty;
            ModLog.Info("[AutoDrive] disabled | reason=" + reason);
        }

        internal static void LogBlockedOnce(string reason)
        {
            if (reason == _lastBlockReason)
                return;

            _lastBlockReason = reason;
            ModLog.Error("[AutoDrive] blocked: " + reason);
        }

        internal static void ClearBlockReason() => _lastBlockReason = string.Empty;

        internal static void LogApplyThrottled(
            float speed,
            float throttle,
            float brakes,
            float steering,
            float crossTrack,
            float headingError,
            float distDest,
            bool offRoute,
            int waypointCount)
        {
            var now = Time.unscaledTime;
            if (now < _nextTickLogAt)
                return;

            _nextTickLogAt = now + 1f;
            ModLog.Info(
                "[AutoDrive] drive speed=" + speed.ToString("F1") +
                " thr=" + throttle.ToString("F2") +
                " brk=" + brakes.ToString("F2") +
                " steer=" + steering.ToString("F2") +
                " xtrack=" + crossTrack.ToString("F1") +
                " head=" + headingError.ToString("F0") +
                " dest=" + distDest.ToString("F0") +
                " offRoute=" + offRoute +
                " wpts=" + waypointCount);
        }

        internal static void LogStatusThrottled(string status)
        {
            var now = Time.unscaledTime;
            if (now < _nextStatusLogAt)
                return;

            _nextStatusLogAt = now + 3f;
            ModLog.Info("[AutoDrive] status | " + status);
        }

        internal static void LogInputBinding(bool autoSetInput, bool throttleOk, bool brakesOk, bool steeringOk)
        {
            ModLog.Info(
                "[AutoDrive] input binding autoSetInput=" + autoSetInput +
                " throttle=" + throttleOk +
                " brakes=" + brakesOk +
                " steering=" + steeringOk);
        }
    }
}
