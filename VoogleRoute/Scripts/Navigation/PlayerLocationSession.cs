using System;
using BaPlayerLocation.Subscriber;

namespace VoogleRoute.Navigation
{
    /// <summary>
    /// Single LIB_BaPlayerLocation subscription for Voogle Route.
    /// The library invokes the handler on subscribe (if a pose is already known)
    /// and only again after a significant change (movement, heading, mode, place).
    /// </summary>
    internal static class PlayerLocationSession
    {
        private static IDisposable _subscription;

        internal static PlayerLocationSnapshot Snapshot { get; private set; }

        internal static bool IsAvailable => Snapshot.IsAvailable;

        internal static event Action<PlayerLocationSnapshot> Changed;

        internal static void Initialize()
        {
            Shutdown();

            _subscription = PlayerLocationSubscriber.SubscribeWhenActive(Apply);
            ModLog.Info("Subscribed to LIB_BaPlayerLocation.");
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
            Changed?.Invoke(snapshot);
        }
    }
}
