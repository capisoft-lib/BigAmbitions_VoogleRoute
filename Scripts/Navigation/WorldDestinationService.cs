using UI.Guiders;
using UnityEngine;
using VoogleRoute.UI;

namespace VoogleRoute.Navigation
{
    /// <summary>Sets GPS/route targets at exact world positions (not building entrances).</summary>
    internal static class WorldDestinationService
    {
        internal static bool TrySetFromBookmark(BookmarkEntry bookmark)
        {
            if (bookmark == null || !bookmark.TryGetNavigationTarget(out var position))
                return false;

            SetWorldDestination(position, bookmark.DisplayName, NavigationTargetTracker.WorldPositionSource);
            return true;
        }

        internal static void SetParkedVehicleDestination(Vector3 position)
        {
            SetWorldDestination(
                position,
                ModUiText.QuickBookmarkLastCar,
                NavigationTargetTracker.ParkedVehicleSource);
        }

        internal static void SetWorldDestination(Vector3 position, string label, string trackerSource)
        {
            if (position.sqrMagnitude < 0.01f)
                return;

            ClearVanillaAddressDestination();
            NavigationTargetTracker.SetWorldPositionTarget(position, trackerSource);
            TrySetWorldGuider(position, label);
            ModLog.Info("World destination set (" + trackerSource + "): " + position);
        }

        private static void ClearVanillaAddressDestination()
        {
            try
            {
                if (SaveGameManager.Current != null)
                    SaveGameManager.Current.customDestination = null;
            }
            catch
            {
                // ignore
            }
        }

        private static void TrySetWorldGuider(Vector3 position, string label)
        {
            try
            {
                var refs = InstanceBehavior<GlobalReferences>.Instance;
                var icon = refs != null ? refs.vehiclePOIIcon : null;
                var color = refs != null ? refs.vehiclePOIBackgroundColor : Color.white;
                var name = string.IsNullOrWhiteSpace(label) ? position.ToString() : label;
                GuidersManager.SetGuiderTarget(position, name, icon, color, DirectionGuiderType.Destination);
            }
            catch
            {
                // guider optional; route still uses NavigationTargetTracker
            }
        }
    }
}
