using System;
using BaPlayerLocation.Subscriber;

namespace VoogleRoute.Navigation
{
    /// <summary>
    /// Sole VoogleRoute bridge to <c>LIB_BaPlayerLocation</c>.
    /// No other VoogleRoute code may call <see cref="PlayerLocationSubscriber"/> or probe player pose.
    /// </summary>
    internal static class PlayerLocationSession
    {
        private static IDisposable _subscription;

        internal static PlayerLocationSnapshot Snapshot { get; private set; }

        internal static bool IsAvailable => Snapshot.IsAvailable;

        /// <summary>True after the LIB_BaPlayerLocation mod has initialized in the city.</summary>
        internal static bool IsLibraryActive => PlayerLocationSubscriber.IsActive;

        internal static event Action<PlayerLocationSnapshot> Changed;

        internal static void Initialize()
        {
            Shutdown();

            if (!IsLibraryActive)
            {
                ModLog.Info(
                    "[WARN] LIB_BaPlayerLocation is not active. " +
                    "Enable the library mod in Mods — Voogle Route will not navigate without it.");
            }

            _subscription = PlayerLocationSubscriber.SubscribeWhenActive(Apply);
            ModLog.Info("Subscribed to LIB_BaPlayerLocation (SubscribeWhenActive).");
            ModLog.Debug("LIB assembly binding OK | IsActive=" + IsLibraryActive);
        }

        internal static void Shutdown()
        {
            _subscription?.Dispose();
            _subscription = null;
            Snapshot = default;
            MovementModeDetector.Reset();
        }

        private static void Apply(PlayerLocationSnapshot snapshot)
        {
            Snapshot = snapshot;
            MovementModeDetector.Apply(snapshot);
            ModLog.Debug(
                "LIB pose | available=" + snapshot.IsAvailable +
                " kind=" + snapshot.MovementKind +
                " pos=" + snapshot.Position +
                " heading=" + snapshot.HeadingDeg.ToString("F1"));
            Changed?.Invoke(snapshot);
        }
    }
}
