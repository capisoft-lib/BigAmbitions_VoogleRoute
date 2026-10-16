using System;
using System.Globalization;
using BigAmbitions.InputSystem;
using Helpers;
using UI.Notification;
using UnityEngine;
using UnityEngine.SceneManagement;
using VoogleRoute.UI;

namespace VoogleRoute.Navigation.Driving
{
    internal static class PhysicalDriveService
    {
        private static VehicleControlLease _lease;
        private static PhysicalDrivePlan _plan;
        private static readonly DrivingTrafficSensor Sensor = new DrivingTrafficSensor();
        private static object _save;
        private static int _scene;
        private static float _progress, _lastProgress, _stalled, _offRoute, _steering;
        private static Vector3 _previousPosition;
        private static float _nextSensorTime, _trafficLimit, _manualGrace;
        private static float _nextVehicleCheck;
        private static float _speedIntegral, _previousTargetSpeed, _nextTelemetryTime;
        private static string _stoppingReason;
        internal static bool IsActive => _lease != null;

        internal static void Request()
        {
            if (IsActive) { Stop(); return; }
            if (AutoDriveSkipTravelService.IsInProgress) return;
            try
            {
                if (PhysicalDrivePlanner.TryBuild(out var plan, out var failure)) AutoDriveConfirmPopup.Show(plan);
                else Notifications.ShowError(failure);
            }
            catch (Exception ex)
            {
                ModLog.Error("Physical drive planning failed", ex);
                Notifications.ShowError("voogle_route_physical_no_route");
            }
        }

        internal static void Start(PhysicalDrivePlan confirmed)
        {
            var started = DrivingPerformance.Begin();
            try { StartCore(confirmed); }
            finally { DrivingPerformance.End(DrivingStage.Start, started); }
        }

        private static void StartCore(PhysicalDrivePlan confirmed)
        {
            if (confirmed == null || !ModConfig.PhysicalAutoDriveEnabled || IsActive || AutoDriveSkipTravelService.IsInProgress) return;
            try
            {
                // Confirmation can stay open while the player changes car, position or destination.
                if (VehicleHelper.GetCurrentVehicleBase() != confirmed.Car ||
                    !NavigationTargetTracker.HasMapGpsTarget ||
                    (NavigationTargetTracker.ActiveTarget - confirmed.Destination).sqrMagnitude > 1f)
                { Notifications.ShowError("voogle_route_physical_start_lane"); return; }
                var fresh = confirmed;
                float startProgress;
                using (var geometry = new VehicleControlLease(confirmed.Car, acquire: false))
                {
                    if (PhysicalDrivePlanner.CanReuse(confirmed, geometry, out startProgress, out var reason))
                    {
                        DrivingPerformance.ReusedPlan();
                        DrivingPerformance.Event("plan_reused samples=" + confirmed.Path.Points.Length);
                    }
                    else
                    {
                        DrivingPerformance.Event("plan_refresh reason=" + reason);
                        if (!PhysicalDrivePlanner.TryBuild(out fresh, out var failure))
                        { Notifications.ShowError(failure); return; }
                        startProgress = fresh.StartProgress;
                    }
                }
                if (AutoDriveVehicleFuel.TryEstimate(fresh.Path.Length - startProgress, out var fuel) && !fuel.HasEnoughFuel)
                { Notifications.ShowError("voogle_route_autodrive_insufficient_fuel"); return; }
                _lease = new VehicleControlLease(fresh.Car);
                _plan = fresh;
                _save = SaveGameManager.Current;
                _scene = SceneManager.GetActiveScene().handle;
                _progress = startProgress;
                Sensor.Reset();
                _lastProgress = _progress;
                _stalled = _offRoute = _steering = 0f;
                _stoppingReason = null;
                _nextSensorTime = 0f;
                _nextVehicleCheck = 0f;
                _speedIntegral = _previousTargetSpeed = _nextTelemetryTime = 0f;
                _trafficLimit = 0f;
                _previousPosition = _lease.RearPosition;
                _manualGrace = Time.unscaledTime + 0.5f;
                RouteActionPanel.RefreshVisual();
                DrivingPerformance.Event("drive_started distance=" + Mathf.RoundToInt(fresh.Path.Length - _progress));
            }
            catch (Exception ex)
            {
                Stop();
                ModLog.Error("Physical drive start failed", ex);
                Notifications.ShowError("voogle_route_physical_no_route");
            }
        }

        internal static void TickInput()
        {
            if (!IsActive) return;
            try
            {
                if (!_lease.IsCurrent || !ReferenceEquals(_save, SaveGameManager.Current) ||
                    SceneManager.GetActiveScene().handle != _scene || !GameState.IsPlayable() ||
                    !NavigationTargetTracker.HasMapGpsTarget ||
                    (_plan.Destination - NavigationTargetTracker.ActiveTarget).sqrMagnitude > 1f)
                { Stop(reason: "context_changed"); return; }
                if (Time.unscaledTime >= _manualGrace &&
                    ((PlayerAction.Move.Pressing() && PlayerAction.Move.Vector().sqrMagnitude > 0.04f) ||
                     PlayerAction.Cancel.Pressed() || PlayerAction.Interact.Pressed() ||
                     Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Escape))) Stop(reason: "manual_input");
            }
            catch (Exception ex) { Fail(ex); }
        }

        internal static void FixedTick()
        {
            if (!IsActive) return;
            try
            {
                if (!_lease.IsCurrent) { Stop(); return; }
                if (Time.timeScale < 0.01f || GameState.IsOverlayBlockingNavigation())
                { _speedIntegral = 0f; _lease.Hold(); return; }
                if (_stoppingReason != null)
                {
                    _lease.Write(_steering, 0f, 1f);
                    if (_lease.Speed < 0.35f) Stop(_stoppingReason);
                    return;
                }
                var position = _lease.RearPosition;
                if ((position - _previousPosition).sqrMagnitude > 100f) { Stop(); return; }
                _previousPosition = position;
                var projectionStarted = DrivingPerformance.Begin();
                _progress = _plan.Path.Project(position, _progress, out var lateral);
                DrivingPerformance.End(DrivingStage.Projection, projectionStarted);
                DrivingPerformance.ProjectionWork(_plan.Path.LastProjectionSegments);
                _offRoute = lateral > 4f ? _offRoute + Time.fixedDeltaTime : 0f;
                if (_offRoute > 1.2f || float.IsInfinity(lateral)) { BrakeToStop("voogle_route_physical_blocked"); return; }
                var speed = _lease.Speed;
                if (Time.time >= _nextVehicleCheck)
                {
                    _nextVehicleCheck = Time.time + 1f;
                    var car = _lease.Car;
                    var fuelEnabled = car.fuelModule != null && car.fuelModule.useFuel &&
                        car.vehicleType != null && car.vehicleType.maxFuel > 0f &&
                        !(SaveGameManager.Current?.gameVariables?.disableVehicleFuel ?? false);
                    if (fuelEnabled && car.GetCurrentFuel() < 0.001f)
                    { BrakeToStop("voogle_route_autodrive_insufficient_fuel"); return; }
                    if (!car.vehicleController.powertrain.engine.canRun)
                    { BrakeToStop("voogle_route_physical_blocked"); return; }
                }
                var remaining = _plan.Path.Length - _progress;
                if (remaining < 1.2f && speed < 0.35f) { Stop("voogle_route_physical_arrived"); return; }
                if (remaining < 0.1f && DrivingPath.FlatDistance(position, _plan.Path.At(_plan.Path.Length)) > 4f)
                { BrakeToStop("voogle_route_physical_blocked"); return; }

                var lookAhead = Mathf.Clamp(2.8f + speed * 0.6f, 3f, 10f);
                var target = _plan.Path.At(_progress + lookAhead);
                var local = _lease.Vehicle.transform.InverseTransformDirection(DrivingPath.Flat(target - position));
                var curvature = 2f * local.x / Mathf.Max(1f, local.x * local.x + local.z * local.z);
                var desired = Mathf.Atan(_lease.Wheelbase * curvature) * Mathf.Rad2Deg / _lease.MaxSteer;
                _steering = Mathf.MoveTowards(_steering, Mathf.Clamp(desired, -1f, 1f), 2.5f * Time.fixedDeltaTime);
                if (Time.time >= _nextSensorTime)
                {
                    var sensorStarted = DrivingPerformance.Begin();
                    try { _trafficLimit = Sensor.SpeedLimit(_plan, _lease, _progress, DrivingSpeedLimits.CruiseMps(_lease)); }
                    finally { DrivingPerformance.End(DrivingStage.Sensor, sensorStarted); }
                    _nextSensorTime = Time.time + 0.08f;
                }
                var pathLimit = _plan.Path.TargetSpeed(_progress);
                var roadLimit = DrivingSpeedLimits.TargetMps(position);
                var targetSpeed = Mathf.Min(Mathf.Min(_trafficLimit, pathLimit),
                    Mathf.Min(Mathf.Min(DrivingSpeedLimits.CruiseMps(_lease), roadLimit),
                        Mathf.Sqrt(4f * Mathf.Max(0f, remaining - 0.4f))));
                if (lateral > 1.5f) targetSpeed = Mathf.Min(targetSpeed, 3f);
                // Integrate steady drag error, but discard stored acceleration when slowing or holding.
                if (targetSpeed < 0.15f || targetSpeed < _previousTargetSpeed - 0.5f) _speedIntegral = 0f;
                _previousTargetSpeed = targetSpeed;
                var error = targetSpeed - speed;
                var feedForward = 0.08f + speed * 0.008f;
                var demand = error * 0.4f + feedForward + _speedIntegral;
                if ((error > 0f && demand < 1f) || error < 0f)
                    _speedIntegral = Mathf.Clamp(_speedIntegral + error * Time.fixedDeltaTime * 0.12f, 0f, 0.65f);
                var brake = targetSpeed < 0.15f ? 1f : error < -0.12f ? Mathf.Clamp01((-error - 0.05f) * 0.7f) : 0f;
                if (brake > 0f) _speedIntegral = Mathf.MoveTowards(_speedIntegral, 0f, Time.fixedDeltaTime * 0.3f);
                var throttle = brake > 0f ? 0f : Mathf.Clamp01(error * 0.4f + feedForward + _speedIntegral);
                _lease.Write(_steering, throttle, brake);
                if (Time.unscaledTime >= _nextTelemetryTime)
                {
                    _nextTelemetryTime = Time.unscaledTime + 5f;
                    DrivingPerformance.Event(string.Format(CultureInfo.InvariantCulture,
                        "drive_state speed_kmh={0:F1} target_kmh={1:F1} road_kmh={2:F0} path_kmh={3:F1} traffic_kmh={4:F1} gear={5} rpm={6:F0} throttle={7:F2} brake={8:F2} stop_pedestrians={9} speeding={10} run_red={11}",
                        speed * 3.6f, targetSpeed * 3.6f, DrivingSpeedLimits.At(position), pathLimit * 3.6f,
                        _trafficLimit * 3.6f, _lease.Gear, _lease.Rpm, throttle, brake,
                        ModConfig.StopForPedestriansEnabled, ModConfig.AllowSpeedingEnabled, ModConfig.RunRedLightsEnabled));
                }

                if (_progress > _lastProgress + 1f) { _stalled = 0f; _lastProgress = _progress; }
                else if (!Sensor.WaitingForSignal) _stalled += Time.fixedDeltaTime;
                if (_stalled > 25f) BrakeToStop("voogle_route_physical_blocked");
            }
            catch (Exception ex) { Fail(ex); }
        }

        internal static void LateTick() => _lease?.ClearAssist();
        internal static void RefreshSpeedPolicy()
        {
            if (!IsActive || _plan == null) return;
            _plan.Path.SetCruiseSpeed(DrivingSpeedLimits.CruiseMps(_lease));
            _speedIntegral = 0f;
            _nextSensorTime = 0f;
            _nextTelemetryTime = 0f;
        }
        private static void BrakeToStop(string message)
        {
            _speedIntegral = 0f;
            _stoppingReason = message;
            _lease?.Write(_steering, 0f, 1f);
        }
        internal static void Stop(string message = null, string reason = "requested")
        {
            var owned = _lease;
            _lease = null; _plan = null; _save = null; _stoppingReason = null;
            Sensor.Reset();
            try { owned?.Dispose(); }
            finally
            {
                if (owned != null)
                {
                    DrivingPerformance.Event("drive_stopped reason=" + (message ?? reason) + " progress=" + Mathf.RoundToInt(_progress));
                    RouteActionPanel.RefreshVisual();
                    if (message != null) Notifications.Show(
                        message == "voogle_route_physical_arrived" ? NotificationType.Success : NotificationType.Warning, message);
                }
            }
        }
        private static void Fail(Exception ex)
        {
            Stop(); ModLog.Error("Physical drive interrupted", ex);
            Notifications.ShowError("voogle_route_physical_blocked");
        }
    }
}
