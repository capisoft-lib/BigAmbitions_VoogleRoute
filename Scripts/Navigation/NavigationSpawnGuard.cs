using Helpers;
using UnityEngine;

namespace VoogleRoute.Navigation
{
    /// <summary>
    /// Prevents outdoor/indoor auto-walk from firing during save load while the player spawns inside a building.
    /// </summary>
    internal static class NavigationSpawnGuard
    {
        private const float SuppressAutoWalkSeconds = 4f;
        private const int OutdoorStableFramesRequired = 5;

        private static float _suppressAutoWalkUntil = -1f;
        private static bool _appliedIndoorSpawnReset;
        private static int _outdoorStableFrames;

        internal static void OnSaveRebound()
        {
            _appliedIndoorSpawnReset = false;
            _outdoorStableFrames = 0;
            _suppressAutoWalkUntil = Time.unscaledTime + SuppressAutoWalkSeconds;
        }

        internal static bool IsAutoWalkSuppressed => Time.unscaledTime < _suppressAutoWalkUntil;

        internal static void Tick()
        {
            if (!GameState.IsWorldReady())
                return;

            if (GameState.IsIndoorNavigationContext())
            {
                _outdoorStableFrames = 0;

                if (_appliedIndoorSpawnReset)
                    return;

                _appliedIndoorSpawnReset = true;
                ApplyIndoorSpawnReset();
                _suppressAutoWalkUntil = Time.unscaledTime + 1f;
                return;
            }

            _outdoorStableFrames++;
            if (_outdoorStableFrames >= OutdoorStableFramesRequired)
                _suppressAutoWalkUntil = -1f;
        }

        internal static void Reset()
        {
            _appliedIndoorSpawnReset = false;
            _outdoorStableFrames = 0;
            _suppressAutoWalkUntil = -1f;
        }

        private static void ApplyIndoorSpawnReset()
        {
            AutoWalkService.Reset();
            IndoorAutoWalkService.Reset();
            PlayerNavigationRelease.Release();

            if (ModConfig.AutoWalkEnabled)
                ModConfig.SetAutoWalkEnabled(false, persist: false);

            if (ModConfig.IndoorAutoWalkEnabled)
                ModConfig.SetIndoorAutoWalkEnabled(false, persist: false);

            ModLog.Info("Suppressed auto-walk after spawning inside a building.");
        }
    }
}
