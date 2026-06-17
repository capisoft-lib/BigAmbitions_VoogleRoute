using UnityEngine;
using VoogleRoute;

namespace VoogleRoute.Navigation
{
    
    internal static class NavigationTargetTracker
    {
        internal const string MapSource = "map.customDestination";
        internal const string JobSource = "job.destination";
        internal const string ParkedVehicleSource = "parked.vehicle";
        internal const string WorldPositionSource = "world.position";
    
        internal static bool HasTarget { get; private set; }
        internal static bool HasMapGpsTarget => HasTarget;
        internal static Vector3 ActiveTarget { get; private set; }
        internal static float LastChangeTime { get; private set; }
        internal static string LastSource { get; private set; } = "";
    
        internal static void SetMapGpsTarget(Vector3 target)
        {
            SetTarget(target, MapSource);
        }

        internal static void SetJobTarget(Vector3 target)
        {
            SetTarget(target, JobSource);
        }

        internal static void SetParkedVehicleTarget(Vector3 target)
        {
            SetWorldPositionTarget(target, ParkedVehicleSource);
        }

        internal static void SetWorldPositionTarget(Vector3 target, string source = WorldPositionSource)
        {
            SetTarget(target, source);
        }

        internal static bool IsModNavigationSource =>
            LastSource == MapSource ||
            LastSource == JobSource ||
            LastSource == ParkedVehicleSource ||
            LastSource == WorldPositionSource;

        private static void SetTarget(Vector3 target, string source)
        {
            if (HasTarget && (ActiveTarget - target).sqrMagnitude < 0.25f && LastSource == source)
                return;

            ActiveTarget = target;
            HasTarget = true;
            LastSource = source;
            LastChangeTime = Time.unscaledTime;
            NavigationAutoEnterService.NotifyTargetChanged();
            ModLog.Info("Navigation target set (" + source + "): " + target);
            PathFinderService.NotifyMapDestinationChanged();
        }
    
        internal static void ClearMapGpsTarget(string reason)
        {
            if (!HasTarget)
                return;
    
            HasTarget = false;
            LastSource = reason;
            LastChangeTime = Time.unscaledTime;
            ModLog.Info("Map GPS target cleared: " + reason);
        }
    }
}
