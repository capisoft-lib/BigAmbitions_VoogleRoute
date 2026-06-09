using UnityEngine;
using VoogleRoute;

namespace VoogleRoute.Navigation
{
    
    internal static class NavigationTargetTracker
    {
        internal const string MapSource = "map.customDestination";
    
        internal static bool HasTarget { get; private set; }
        internal static bool HasMapGpsTarget => HasTarget;
        internal static Vector3 ActiveTarget { get; private set; }
        internal static float LastChangeTime { get; private set; }
        internal static string LastSource { get; private set; } = "";
    
        internal static void SetMapGpsTarget(Vector3 target)
        {
            if (HasTarget && (ActiveTarget - target).sqrMagnitude < 0.25f)
                return;

            CityMapHelper.CloseIfOpen();

            ActiveTarget = target;
            HasTarget = true;
            LastSource = MapSource;
            LastChangeTime = Time.unscaledTime;
            ModLog.Info("Map GPS target set: " + target);
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
