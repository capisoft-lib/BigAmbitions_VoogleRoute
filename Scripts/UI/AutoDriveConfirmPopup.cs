using System;
using Helpers;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UI.Notification;
using VoogleRoute.Navigation;

using Capisoft.Lib.BaUnifiedUI.Controls;
using Capisoft.Lib.BaUnifiedUI.Core;
using Capisoft.Lib.BaUnifiedUI.Fluent;

namespace VoogleRoute.UI
{
    internal static class AutoDriveConfirmPopup
    {
        private const string RootName = "VoogleRoute_AutoDrivePopup_v3";
        private const string DragPositionId = "voogleroute:auto-drive-confirm";
        private const int CanvasSortOrder = 12000;
        private const float PanelWidth = 600f;
        private const float PanelHeight = 350f;
        private const float ModeButtonHeight = 40f;
        private const float HeaderHeight = 48f;
        private const float BodyFontSize = 18f;
        private const float FooterButtonHeight = 44f;
        private const float FooterBottomInset = 24f;
        private const float ButtonGap = 18f;
        private const float BodyTopGap = 18f;
        private const float BodyBottomGap = 18f;
        private const float PreviewPollSeconds = 0.2f;
        private const float PreviewStoppedSpeed = 0.2f;
        private const float PreviewSettleSeconds = 0.25f;

        private static readonly Color BodyTextColor = new Color(0.92f, 0.94f, 0.96f, 1f);

        private static GameObject _root;
        private static TextMeshProUGUI _titleLabel;
        private static TextMeshProUGUI _bodyLabel;
        private static AutoDriveSkipPlanner.Plan _pendingPlan;
        private static Navigation.Driving.PhysicalDrivePlan _physicalPlan;
        private static Button _watchButton, _skipButton, _confirmButton, _cancelButton;
        private static bool _watchDriving;
        private static string _failureKey;
        private static object _vehicleIdentity, _saveIdentity;
        private static Vector3 _destination;
        private static int _sceneHandle;
        private static CarController _previewCar;
        private static Rigidbody _previewBody;
        private static Transform _previewTransform;
        private static float _nextPreviewCheck, _stoppedSince, _attemptCruise;
        private static bool _wasStopped, _hasAttempt, _attemptSpeeding, _attemptBuildingSide;
        private static Vector3 _attemptPosition, _attemptForward;
        private static object _attemptGraph, _attemptTraffic;

        internal static bool IsOpen => _root != null && _root.activeSelf;

        internal static void EnsureCreated()
        {
            VoogleRoutePanelLifecycle.DestroyIfStale(ref _root, RootName, Destroy);
            if (_root != null)
                return;

            var built = BaUi.Modal(RootName, CanvasSortOrder, 0.45f)
                .OnDismiss(Close)
                .Panel(BaPanelRecipe.Modal, PanelWidth, height: PanelHeight)
                .Draggable(DragPositionId)
                .Header(h => h.TitleCenter(ModUiText.AutoDrivePopupTitle))
                .SkipBody()
                .Build();

            _root = built.Root;
            VoogleRouteUiScale.Register(built.Panel);
            var scale = built.Scale;
            var textScale = Mathf.Clamp(scale, 0.85f, 1.15f);
            _titleLabel = built.Header.Find("Title")?.GetComponent<TextMeshProUGUI>();

            var body = BaUiWidgets.CreateRect(built.Panel, "Body");
            body.anchorMin = Vector2.zero;
            body.anchorMax = Vector2.one;
            body.offsetMin = new Vector2(built.ContentInset, FooterBottomInset + FooterButtonHeight + BodyBottomGap);
            body.offsetMax = new Vector2(-built.ContentInset, -(HeaderHeight + BodyTopGap * 2f + ModeButtonHeight));

            _bodyLabel = body.gameObject.AddComponent<TextMeshProUGUI>();
            _bodyLabel.fontSize = BodyFontSize * textScale;
            _bodyLabel.color = BodyTextColor;
            _bodyLabel.alignment = TextAlignmentOptions.Center;
            _bodyLabel.enableWordWrapping = true;
            _bodyLabel.raycastTarget = false;
            BaUi.ApplyButtonFont(_bodyLabel);

            var buttonWidth = (PanelWidth - built.ContentInset * 2f - ButtonGap * textScale) * 0.5f;
            var buttonX = (buttonWidth + ButtonGap * textScale) * 0.5f;
            _watchButton = BaUiWidgets.CreateFooterButton(
                built.Panel, "WatchDrivingButton", Vector2.zero,
                new Vector2(buttonWidth, ModeButtonHeight), textScale,
                ModUiText.WatchDriving, BaButtonStyle.Blue, () => SelectMode(true));
            _skipButton = BaUiWidgets.CreateFooterButton(
                built.Panel, "TimeSkipButton", Vector2.zero,
                new Vector2(buttonWidth, ModeButtonHeight), textScale,
                ModUiText.TimeSkip, BaButtonStyle.Grey, () => SelectMode(false));
            PlaceModeButton(_watchButton, -buttonX);
            PlaceModeButton(_skipButton, buttonX);
            _cancelButton = BaUiWidgets.CreateFooterButton(
                built.Panel, "CancelButton", new Vector2(-buttonX, FooterBottomInset),
                new Vector2(buttonWidth, FooterButtonHeight), textScale,
                ModUiText.AutoDriveCancel, BaButtonStyle.Grey, Close);
            _confirmButton = BaUiWidgets.CreateFooterButton(
                built.Panel, "ConfirmButton", new Vector2(buttonX, FooterBottomInset),
                new Vector2(buttonWidth, FooterButtonHeight), textScale,
                ModUiText.AutoDriveConfirm, BaButtonStyle.Green, Confirm);

            _root.SetActive(false);
        }

        private static void PlaceModeButton(Button button, float x)
        {
            var rect = (RectTransform)button.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(x, -(HeaderHeight + BodyTopGap));
            var label = button.GetComponentInChildren<TextMeshProUGUI>();
            label.enableAutoSizing = true;
            label.fontSizeMax = label.fontSize;
            label.fontSizeMin = label.fontSize * 0.75f;
        }

        internal static void ShowChoice()
        {
            EnsureCreated();
            _pendingPlan = default;
            _physicalPlan = null;
            _vehicleIdentity = VehicleHelper.GetCurrentVehicleBase();
            _saveIdentity = SaveGameManager.Current;
            _destination = NavigationTargetTracker.ActiveTarget;
            _sceneHandle = SceneManager.GetActiveScene().handle;
            _previewCar = _vehicleIdentity as CarController;
            _previewBody = _previewCar?.vehicleController?.GetComponent<Rigidbody>();
            _previewTransform = _previewCar?.vehicleController?.transform;
            _wasStopped = _hasAttempt = false;
            _nextPreviewCheck = 0f;
            _root.SetActive(true);
            SelectMode(ModConfig.WatchDrivingByDefault);
        }

        private static void SelectMode(bool watchDriving)
        {
            _watchDriving = watchDriving;
            _failureKey = null;
            try
            {
                if (_watchDriving)
                    RefreshPhysicalPreview(force: true);
                else if (!AutoDriveSkipPlanner.TryBuildPlan(out _pendingPlan))
                    _failureKey = _pendingPlan.FailureKey ?? "voogle_route_autodrive_no_route";
            }
            catch (Exception ex)
            {
                if (_watchDriving) _physicalPlan = null;
                else _pendingPlan = default;
                _failureKey = _watchDriving ? "voogle_route_physical_no_route" : "voogle_route_autodrive_no_route";
                ModLog.Error("Travel mode planning failed", ex);
            }
            RefreshLocalizedText();
        }

        internal static void Close()
        {
            BaUiFocus.ReleaseForMovement();

            if (_root != null)
                _root.SetActive(false);

            _pendingPlan = default;
            _physicalPlan = null;
            _vehicleIdentity = _saveIdentity = null;
            _failureKey = null;
            ResetPreview();
        }

        internal static void TickOverlay()
        {
            if (!IsOpen)
                return;

            if (Input.GetKeyDown(KeyCode.Escape) || !IsContextCurrent())
            {
                Close();
                return;
            }
            if (!_watchDriving || Time.unscaledTime < _nextPreviewCheck) return;
            _nextPreviewCheck = Time.unscaledTime + PreviewPollSeconds;
            try
            {
                if (RefreshPhysicalPreview(force: false)) RefreshLocalizedText();
            }
            catch (Exception ex)
            {
                _physicalPlan = null;
                _failureKey = "voogle_route_physical_no_route";
                _nextPreviewCheck = Time.unscaledTime + 2f;
                RefreshLocalizedText();
                ModLog.Error("Travel readiness refresh failed", ex);
            }
        }

        private static bool RefreshPhysicalPreview(bool force)
        {
            var started = Navigation.Driving.DrivingPerformance.Begin();
            try { return RefreshPhysicalPreviewCore(force); }
            finally { Navigation.Driving.DrivingPerformance.End(Navigation.Driving.DrivingStage.Preview, started); }
        }

        private static bool RefreshPhysicalPreviewCore(bool force)
        {
            if (_previewCar == null || _previewBody == null || _previewTransform == null)
            {
                var changed = _physicalPlan != null || _failureKey != "voogle_route_autodrive_not_in_vehicle";
                _physicalPlan = null;
                _failureKey = "voogle_route_autodrive_not_in_vehicle";
                return changed;
            }
            // Read velocity directly: polling must not construct a geometry lease or search a route.
            if (Navigation.Driving.DrivingPath.Flat(_previewBody.velocity).sqrMagnitude > PreviewStoppedSpeed * PreviewStoppedSpeed)
            {
                var changed = _physicalPlan != null || _failureKey != "voogle_route_physical_start_lane";
                _wasStopped = _hasAttempt = false;
                _physicalPlan = null;
                _failureKey = "voogle_route_physical_start_lane";
                return changed;
            }
            var now = Time.unscaledTime;
            if (!_wasStopped) { _wasStopped = true; _stoppedSince = now; }
            if (!force && now - _stoppedSince < PreviewSettleSeconds) return false;
            var position = _previewTransform.position;
            var forward = _previewTransform.forward;
            var graph = Navigation.RouteGraphStore.Graph;
            var traffic = GleyUrbanAssets.CurrentSceneData.GetSceneInstance()?.allWaypoints;
            var sameAttempt = _hasAttempt && ReferenceEquals(_attemptGraph, graph) && ReferenceEquals(_attemptTraffic, traffic) &&
                _attemptCruise == ModConfig.PhysicalDriveCruiseCeilingKmh && _attemptSpeeding == ModConfig.AllowSpeedingEnabled &&
                _attemptBuildingSide == ModConfig.ForceCorrectSideArrivalEnabled &&
                (position - _attemptPosition).sqrMagnitude < (_physicalPlan != null ? 2.25f : 0.25f) &&
                Vector3.Angle(forward, _attemptForward) < 5f;
            var expired = _physicalPlan != null && Time.realtimeSinceStartup - _physicalPlan.CreatedAt > 60f;
            // A stationary refusal is retried only after a meaningful change. A valid plan stays cached.
            if (sameAttempt && !expired && (!force || _physicalPlan != null)) return false;
            _hasAttempt = true;
            _attemptPosition = position; _attemptForward = forward;
            _attemptGraph = graph; _attemptTraffic = traffic;
            _attemptCruise = ModConfig.PhysicalDriveCruiseCeilingKmh;
            _attemptSpeeding = ModConfig.AllowSpeedingEnabled;
            _attemptBuildingSide = ModConfig.ForceCorrectSideArrivalEnabled;
            if (Navigation.Driving.PhysicalDrivePlanner.TryBuild(out _physicalPlan, out var failure)) _failureKey = null;
            else { _physicalPlan = null; _failureKey = failure; }
            return true;
        }

        private static void ResetPreview()
        {
            _previewCar = null; _previewBody = null; _previewTransform = null;
            _attemptGraph = _attemptTraffic = null;
            _wasStopped = _hasAttempt = false;
            _nextPreviewCheck = 0f;
        }

        private static bool IsContextCurrent() =>
            _vehicleIdentity != null && ReferenceEquals(_vehicleIdentity, VehicleHelper.GetCurrentVehicleBase()) &&
            ReferenceEquals(_saveIdentity, SaveGameManager.Current) &&
            _sceneHandle == SceneManager.GetActiveScene().handle && NavigationTargetTracker.HasMapGpsTarget &&
            (NavigationTargetTracker.ActiveTarget - _destination).sqrMagnitude <= 1f;

        internal static void RefreshLocalizedText()
        {
            if (_titleLabel != null)
                _titleLabel.text = ModUiText.AutoDrivePopupTitle;

            SetButtonText(_watchButton, ModUiText.WatchDriving);
            SetButtonText(_skipButton, ModUiText.TimeSkip);
            SetButtonText(_confirmButton, ModUiText.AutoDriveConfirm);
            SetButtonText(_cancelButton, ModUiText.AutoDriveCancel);
            if (_watchButton != null) BaUiWidgets.ApplyButtonGraphic((Image)_watchButton.targetGraphic,
                _watchDriving ? BaButtonStyle.Blue : BaButtonStyle.Grey);
            if (_skipButton != null) BaUiWidgets.ApplyButtonGraphic((Image)_skipButton.targetGraphic,
                !_watchDriving ? BaButtonStyle.Blue : BaButtonStyle.Grey);
            if (_confirmButton != null)
                _confirmButton.interactable = _failureKey == null && (_watchDriving ? _physicalPlan != null : _pendingPlan.Success);

            if (_bodyLabel == null) return;
            if (_failureKey != null)
            {
                _bodyLabel.text = ModUiText.TravelModeFailure(_failureKey);
                return;
            }
            if (_watchDriving && _physicalPlan != null)
                _bodyLabel.text = ModUiText.PhysicalDriveBody(Mathf.RoundToInt(_physicalPlan.Path.Length));

            if (!_watchDriving && _pendingPlan.Success)
            {
                _bodyLabel.text = ModUiText.FormatAutoDrivePopupBody(
                    _pendingPlan.TravelMinutes,
                    _pendingPlan.DistanceMeters,
                    _pendingPlan.UsesFuel,
                    _pendingPlan.FuelUsedLiters);
            }
        }

        private static void SetButtonText(Button button, string text)
        {
            if (button != null) button.GetComponentInChildren<TextMeshProUGUI>().text = text;
        }

        private static void Confirm()
        {
            if (!IsContextCurrent())
            {
                Close();
                Notifications.ShowError("voogle_route_autodrive_no_route");
                return;
            }
            if (_failureKey != null) return;
            if (_watchDriving)
            {
                SelectMode(true);
                if (_failureKey != null || _physicalPlan == null) return;
                var physical = _physicalPlan;
                Close();
                CityMapHelper.CloseIfOpen();
                Navigation.Driving.PhysicalDriveService.Start(physical);
                return;
            }
            if (_watchDriving) return;
            // Recheck the displayed route and fuel before committing a time skip.
            SelectMode(false);
            if (_failureKey != null || !_pendingPlan.Success) return;
            var plan = _pendingPlan;
            Close();
            CityMapHelper.CloseIfOpen();
            AutoDriveSkipTravelService.StartTravel(plan);
        }

        internal static void Destroy()
        {
            _physicalPlan = null;
            _pendingPlan = default;
            _vehicleIdentity = _saveIdentity = null;
            _failureKey = null;
            ResetPreview();
            _watchButton = _skipButton = _confirmButton = _cancelButton = null;
            if (_root != null)
            {
                UnityEngine.Object.Destroy(_root);
                _root = null;
                _titleLabel = null;
                _bodyLabel = null;
            }
        }
    }
}
