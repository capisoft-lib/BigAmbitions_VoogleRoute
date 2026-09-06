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
        private static bool _waitingForSelectedRoute;

        private const float SelectedRouteTimeoutSeconds = 10f;
        private const float SelectedRouteRetryDelaySeconds = 0.75f;
        private const int SelectedRouteMaxAttempts = 3;

        internal static bool IsInProgress => _inProgress;

        internal static void RequestFromActionPanel()
        {
            if (_inProgress)
                return;

            if (!GameState.ShouldShowNavigationPanel() ||
                !MovementModeDetector.CanUseAutoDrive())
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

            if (!MovementModeDetector.CanUseAutoDrive())
            {
                Notifications.ShowError("voogle_route_autodrive_not_in_vehicle");
                return;
            }

            TryShowConfirmPopup();
        }

        /// <summary>
        /// A newly selected map destination invalidates the vehicle-route cache. Wait
        /// for that route to be rebuilt before opening the auto-drive confirmation.
        /// </summary>
        internal static void RequestFromMapSelection()
        {
            if (_inProgress || _waitingForSelectedRoute)
                return;

            if (!MovementModeDetector.CanUseAutoDrive())
            {
                Notifications.ShowError("voogle_route_autodrive_not_in_vehicle");
                return;
            }

            if (!NavigationTargetTracker.HasMapGpsTarget)
            {
                Notifications.ShowError("voogle_route_autodrive_no_route");
                return;
            }

            var host = VoogleRouteDriver.Instance;
            if (host == null)
            {
                Notifications.ShowError("voogle_route_autodrive_no_route");
                return;
            }

            host.StartCoroutine(WaitForSelectedRouteCoroutine());
        }

        private static IEnumerator WaitForSelectedRouteCoroutine()
        {
            _waitingForSelectedRoute = true;
            try
            {
                // Let the map close and the regular navigation loop observe the new
                // vanilla destination before requesting an explicit rebuild.
                yield return null;

                var deadline = Time.unscaledTime + SelectedRouteTimeoutSeconds;
                var nextAttemptTime = 0f;
                var attempts = 0;

                while (Time.unscaledTime < deadline)
                {
                    if (!MovementModeDetector.CanUseAutoDrive())
                    {
                        Notifications.ShowError("voogle_route_autodrive_not_in_vehicle");
                        yield break;
                    }

                    if (!NavigationTargetTracker.HasMapGpsTarget)
                    {
                        Notifications.ShowError("voogle_route_autodrive_no_route");
                        yield break;
                    }

                    if (PathFinderService.TryGetCachedRouteForDisplay(out _))
                    {
                        TryShowConfirmPopup();
                        yield break;
                    }

                    if (!PathFinderService.IsAsyncRecalcInProgress &&
                        attempts < SelectedRouteMaxAttempts &&
                        Time.unscaledTime >= nextAttemptTime)
                    {
                        attempts++;
                        nextAttemptTime = Time.unscaledTime + SelectedRouteRetryDelaySeconds;
                        PathFinderService.GetRoute(
                            forceRecalc: true,
                            requestSource: "map_building_auto_drive");
                    }

                    if (attempts >= SelectedRouteMaxAttempts &&
                        !PathFinderService.IsAsyncRecalcInProgress)
                        break;

                    yield return null;
                }

                Notifications.ShowError("voogle_route_autodrive_no_route");
            }
            finally
            {
                _waitingForSelectedRoute = false;
            }
        }

        private static void TryShowConfirmPopup()
        {
            if (!MovementModeDetector.CanUseAutoDrive())
            {
                Notifications.ShowError("voogle_route_autodrive_not_in_vehicle");
                return;
            }

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
            if (_inProgress || !plan.Success || !MovementModeDetector.CanUseAutoDrive())
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
            if (vehicle == null ||
                (vehicle.vehicleType != null && vehicle.vehicleType.spawnInPlayerObject))
            {
                _inProgress = false;
                Notifications.ShowError("voogle_route_autodrive_no_route");
                yield break;
            }

            yield return BigAmbitionsCompatibility.Fade();
            screenFaded = true;

            vehicle = VehicleHelper.GetCurrentVehicleBase();
            if (!MovementModeDetector.CanUseAutoDrive() ||
                vehicle == null ||
                (vehicle.vehicleType != null && vehicle.vehicleType.spawnInPlayerObject))
            {
                yield return UiFader.UnFade();
                screenFaded = false;
                _inProgress = false;
                Notifications.ShowError("voogle_route_autodrive_not_in_vehicle");
                yield break;
            }

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
            if (!BigAmbitionsCompatibility.TryStartTimeMachine(
                    InstanceBehavior<UIs>.Instance.timeMachine,
                    timestamp,
                    disableCancel: true))
            {
                yield return UiFader.UnFade();
                screenFaded = false;
                _inProgress = false;
                Notifications.ShowError("voogle_route_autodrive_busy");
                yield break;
            }

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
