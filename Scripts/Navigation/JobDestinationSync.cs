using Player.PlayerMissions;
using Streets;
using Helpers;
using UI.Guiders;
using UnityEngine;
using Vehicles.DeliveryDriverJob;

namespace VoogleRoute.Navigation
{
    /// <summary>Follows vanilla job guider targets (delivery missions) when no map GPS is set.</summary>
    internal static class JobDestinationSync
    {
        private static Address _lastAddress;
        private static bool _hasLastAddress;

        internal static bool TryGetActiveJobAddress(out Address address)
        {
            if (_hasLastAddress && _lastAddress != null)
            {
                address = _lastAddress;
                return true;
            }

            address = null;
            return false;
        }

        internal static void Poll()
        {
            if (!GameState.IsWorldReady())
                return;

            if (!TryResolveActiveJob(out var address, out var worldPos))
            {
                if (NavigationTargetTracker.LastSource == NavigationTargetTracker.JobSource &&
                    NavigationTargetTracker.HasMapGpsTarget)
                {
                    ModLog.Info("Job destination cleared.");
                    NavigationTargetTracker.ClearMapGpsTarget("job destination cleared");
                }

                _hasLastAddress = false;
                _lastAddress = null;
                return;
            }

            if (_hasLastAddress &&
                DestinationResolver.AddressesEqual(_lastAddress, address) &&
                NavigationTargetTracker.HasMapGpsTarget &&
                NavigationTargetTracker.LastSource == NavigationTargetTracker.JobSource)
                return;

            _lastAddress = address;
            _hasLastAddress = address != null;

            ModLog.Info("Job destination synced: " + (address?.ToFormattedString() ?? worldPos.ToString()));
            NavigationTargetTracker.SetJobTarget(worldPos);
        }

        private static bool TryResolveActiveJob(out Address address, out Vector3 worldPos)
        {
            address = null;
            worldPos = default;

            if (TryGetDeliveryMissionTarget(out address, out worldPos))
                return true;

            return TryGetJobGuiderTarget(out address, out worldPos);
        }

        private static bool TryGetDeliveryMissionTarget(out Address address, out Vector3 worldPos)
        {
            address = null;
            worldPos = default;

            try
            {
                if (SaveGameManager.Current?.currentPlayerMission is not DeliveryDriverMission mission)
                    return false;

                if (!mission.IsOngoing())
                    return false;

                address = mission.pinnedAddress;
                if (address == null && mission.destinations != null)
                {
                    for (var i = 0; i < mission.destinations.Count; i++)
                    {
                        var stop = mission.destinations[i];
                        if (stop != null && !stop.IsCompleted())
                        {
                            address = stop.address;
                            break;
                        }
                    }

                    if (address == null && mission.destinations.Count > 0)
                        address = mission.destinations[0]?.address;
                }

                if (address == null)
                    return false;

                return DestinationResolver.TryResolveWorldPosition(address, out worldPos);
            }
            catch
            {
                return false;
            }
        }

        private static bool TryGetJobGuiderTarget(out Address address, out Vector3 worldPos)
        {
            address = null;
            worldPos = default;

            try
            {
                if (!InstanceBehavior<GuidersManager>.IsInitialized)
                    return false;

                var guider = InstanceBehavior<GuidersManager>.Instance?.jobDestinationGuider;
                if (guider == null || guider.target == null)
                    return false;

                address = guider.CurrentAddress;
                worldPos = guider.target.position;

                if (address != null && !address.IsUndefined() &&
                    DestinationResolver.TryResolveWorldPosition(address, out var resolved))
                {
                    worldPos = resolved;
                    return true;
                }

                return worldPos.sqrMagnitude > 0.01f;
            }
            catch
            {
                return false;
            }
        }
    }
}
