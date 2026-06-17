using System;
using System.Collections;
using BigAmbitions.DayNightCycle;
using Helpers;
using UI;
using UI.Notification;
using UnityEngine;
using Vehicles;
using VoogleRoute.UI;

namespace VoogleRoute.Navigation
{
    /// <summary>Skip-travel flow modeled on taxi rides and bridge skip (UiFader + vanilla TimeMachine).</summary>
    internal static class AutoDriveSkipTravelService
    {
        private static bool _inProgress;

        internal static bool IsInProgress => _inProgress;

        internal static void RequestFromActionPanel()
        {
            if (_inProgress)
                return;

            if (!GameState.ShouldShowNavigationPanel() ||
                MovementModeDetector.CurrentMode != MovementMode.Vehicle)
                return;

            TryShowConfirmPopup();
        }

        [Obsolete("Use RequestFromActionPanel.")]
        internal static void RequestFromHud() => RequestFromActionPanel();

        /// <summary>Auto-drive from bookmarks panel — skips HUD visibility check (city map open).</summary>
        internal static void RequestFromBookmark()
        {
            if (_inProgress)
                return;

            if (MovementModeDetector.CurrentMode != MovementMode.Vehicle)
            {
                Notifications.ShowError("voogle_route_autodrive_not_in_vehicle");
                return;
            }

            TryShowConfirmPopup();
        }

        private static void TryShowConfirmPopup()
        {
            if (!NavigationTargetTracker.HasMapGpsTarget)
            {
                Notifications.ShowError("voogle_route_autodrive_no_route");
                return;
            }

            if (IsTimeMachineRunning())
            {
                Notifications.ShowError("voogle_route_autodrive_busy");
                return;
            }

            if (!AutoDriveSkipPlanner.TryBuildPlan(out var plan))
            {
                Notifications.ShowError(plan.FailureKey ?? "voogle_route_autodrive_no_route");
                return;
            }

            UI.AutoDriveConfirmPopup.Show(plan);
        }

        internal static void StartTravel(AutoDriveSkipPlanner.Plan plan)
        {
            if (_inProgress || !plan.Success)
                return;

            if (IsTimeMachineRunning())
                return;

            var host = VoogleRouteDriver.Instance;
            if (host == null)
            {
                ModLog.Error("Auto-drive skip travel: no VoogleRoute_Driver host.");
                return;
            }

            host.StartCoroutine(TravelCoroutine(plan));
        }

        private static IEnumerator TravelCoroutine(AutoDriveSkipPlanner.Plan plan)
        {
            _inProgress = true;
            var screenFaded = false;

            var vehicle = VehicleHelper.GetCurrentVehicleBase();
            if (vehicle == null)
            {
                _inProgress = false;
                Notifications.ShowError("voogle_route_autodrive_no_route");
                yield break;
            }

            yield return UiFader.Fade();
            screenFaded = true;

            AutoDriveRoadTeleport.Apply(
                vehicle,
                plan.RouteLaneHint,
                plan.TeleportPosition,
                plan.TeleportRotation);

            if (plan.UsesFuel)
                AutoDriveVehicleFuel.ApplyConsumption(vehicle, plan.FuelUsedLiters);

            yield return null;

            var timestamp = TimeHelper.Now();
            timestamp.AddMinutes(plan.TravelMinutes);
            InstanceBehavior<UIs>.Instance.timeMachine.StartTimeMachine(timestamp, disableCancel: true);

            yield return UiFader.UnFade();
            screenFaded = false;
            _inProgress = false;
            RouteActionPanel.RefreshVisual();

            if (screenFaded)
                yield return UiFader.UnFade();
        }

        private static bool IsTimeMachineRunning()
        {
            try
            {
                return InstanceBehavior<UIs>.IsInitialized &&
                       InstanceBehavior<UIs>.Instance.timeMachine.isRunning;
            }
            catch
            {
                return false;
            }
        }
    }
}
