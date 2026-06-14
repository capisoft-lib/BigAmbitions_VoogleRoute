using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VoogleRoute;
using VoogleRoute.Navigation;
using VoogleRoute.Rendering;

using Capisoft.Lib.BaUnifiedUI.Controls;
using Capisoft.Lib.BaUnifiedUI.Core;
using Capisoft.Lib.BaUnifiedUI.Fluent;
using Capisoft.Lib.BaUnifiedUI.Layout;

namespace VoogleRoute.UI
{
    /// <summary>Bottom-left VOOGLE ROUTE action panel (route / walk controls).</summary>
    internal static class RouteActionPanel
    {
        private const string RootName = "VoogleRoute_ActionPanel_v85";

        private static GameObject _root;
        private static RectTransform _panelRect;
        private static float _builtPanelWidth;
        private static bool _loggedVisibleChrome;

        private static Image _routeButtonImage;
        private static TextMeshProUGUI _routeLabel;
        private static Image _autoWalkButtonImage;
        private static TextMeshProUGUI _autoWalkLabel;
        private static TextMeshProUGUI _panelTitleLabel;

        private static bool _lastActive;
        private static bool _lastIndoor;
        private static bool _lastRouteOn;
        private static bool _lastWalkOn;
        private static bool _lastOnFoot;
        private static bool _lastInVehicle;
        private static float _lastOffsetY = float.NaN;
        private static bool _forceApply = true;

        private static readonly Color LabelDisabled = new Color(1f, 1f, 1f, 0.5f);
        private static readonly Color ButtonLabelColor = Color.white;

        internal static void EnsureCreated()
        {
            VoogleRoutePanelLifecycle.DestroyIfStale(ref _root, RootName, Destroy);
            if (_root != null)
                return;

            BaUi.EnsureReady();

            var panelScale = Mathf.Max(1f, ModConfig.HudButtonScale);
            var layout = BaUi.Layout.CreateMetrics(panelScale);

            var built = BaUi.Overlay(RootName, 9000)
                .Dock(BaDock.BottomLeft, marginY: ModConfig.NavHudOffsetY)
                .Panel(BaPanelRecipe.ActionPanel, layout.PanelWidth, height: layout.PanelHeight)
                .Header(h => h
                    .TitleLeft(ModUiText.PanelTitle)
                    .Icons(i => i
                        .Icon(BaIcons.Settings, () => RouteSettingsUi.Toggle(), "\u2699")
                        .Icon(BaIcons.History, () => VisitHistoryPanel.Toggle(), "\u23F1")
                        .Icon(BaIcons.Car, OnLastVehicleClicked, "\u2295", BaButtonStyle.Green)
                        .Icon(BaIcons.Add, OnBookmarkPinClicked, "+", BaButtonStyle.Blue)))
                .SkipBody()
                .AfterPanelChildren(p =>
                {
                    BaUiControls.CreatePanelTopActionButton(
                        p.Panel,
                        "RouteButton",
                        new Vector2(layout.LeftButtonX, layout.ButtonTopY),
                        layout.HalfButtonWidth,
                        layout.ButtonHeight,
                        layout.Scale,
                        BaButtonStyle.Blue,
                        OnRouteClicked,
                        out _routeButtonImage,
                        out _routeLabel);

                    BaUiControls.CreatePanelTopActionButton(
                        p.Panel,
                        "AutoWalkButton",
                        new Vector2(layout.RightButtonX, layout.ButtonTopY),
                        layout.HalfButtonWidth,
                        layout.ButtonHeight,
                        layout.Scale,
                        BaButtonStyle.Blue,
                        OnAutoWalkClicked,
                        out _autoWalkButtonImage,
                        out _autoWalkLabel);
                })
                .Build();

            _builtPanelWidth = layout.PanelWidth;
            VoogleRouteUiDiagnostics.LogPanelChrome("action-built", built.Panel, layout.PanelWidth);

            _root = built.Root;
            _panelRect = built.Panel;
            _panelTitleLabel = built.Header.Find("Title")?.GetComponent<TextMeshProUGUI>();
            _loggedVisibleChrome = false;

            _forceApply = true;
            RefreshVisual();
            Debug.Log(
                "[VoogleRoute] Action panel built | root=" + RootName +
                " | ui_build=" + VoogleRouteUiDiagnostics.UiBuildTag +
                " | lib=" + BaUi.LibraryVersion +
                " | layout_rev=" + BaUi.LayoutRevision +
                " | panelW=" + layout.PanelWidth.ToString("F1"));
            VoogleRouteUiDiagnostics.LogOrphanRoots("VoogleRoute_ActionPanel");
            ModLog.Info("VOOGLE ROUTE action panel created.");
        }

        internal static void UpdateVisibility()
        {
            BaUi.EnsureReady();
            VoogleRoutePanelLifecycle.DestroyIfStale(ref _root, RootName, Destroy);
            EnsureCreated();
            if (_root == null)
                return;

            var offsetY = ModConfig.NavHudOffsetY;
            if (_panelRect != null && (_forceApply || !Mathf.Approximately(offsetY, _lastOffsetY)))
            {
                _lastOffsetY = offsetY;
                _panelRect.anchoredPosition = BaUiLayout.GetScreenPosition(offsetY);
            }

            var indoor = GameState.IsIndoorNavigationContext();
            var active = indoor
                ? GameState.ShouldShowIndoorNavigationPanel()
                : GameState.ShouldShowNavigationPanel() && MovementModeDetector.ShouldShowActionPanel();
            if (_forceApply || active != _lastActive)
            {
                _lastActive = active;
                _root.SetActive(active);
            }

            if (!active)
            {
                _forceApply = false;
                return;
            }

            if (!_loggedVisibleChrome && _panelRect != null)
            {
                _loggedVisibleChrome = true;
                BaUiWidgets.RestoreDockedPanelChrome(_panelRect, _builtPanelWidth);
                VoogleRouteUiDiagnostics.LogPanelChrome("action-when-visible", _panelRect, _builtPanelWidth);
            }

            var routeOn = indoor ? ModConfig.IndoorRouteLineEnabled : ModConfig.RouteLineEnabled;
            var walkOn = indoor ? ModConfig.IndoorAutoWalkEnabled : ModConfig.AutoWalkEnabled;
            var onFoot = MovementModeDetector.CurrentMode == MovementMode.OnFoot;
            var inVehicle = MovementModeDetector.CurrentMode == MovementMode.Vehicle;
            if (_forceApply || indoor != _lastIndoor || routeOn != _lastRouteOn || walkOn != _lastWalkOn ||
                onFoot != _lastOnFoot || inVehicle != _lastInVehicle)
            {
                _lastIndoor = indoor;
                _lastRouteOn = routeOn;
                _lastWalkOn = walkOn;
                _lastOnFoot = onFoot;
                _lastInVehicle = inVehicle;
                RefreshVisual();
            }

            _forceApply = false;

            if (active && !RouteSettingsUi.IsOpen && !AutoDriveConfirmPopup.IsOpen &&
                !CityMapBookmarkAddDialog.IsOpen && !VisitHistoryPanel.IsOpen && !GameState.IsCityMapOpen())
                BaUiFocus.ReleaseForMovement();
        }

        internal static void RefreshLocalizedText() => RefreshVisual();

        internal static void RefreshVisual()
        {
            if (_routeButtonImage == null || _routeLabel == null || _autoWalkButtonImage == null || _autoWalkLabel == null)
                return;

            if (_panelTitleLabel != null)
                _panelTitleLabel.text = ModUiText.PanelTitle;

            var indoor = GameState.IsIndoorNavigationContext();
            var routeOn = indoor ? ModConfig.IndoorRouteLineEnabled : ModConfig.RouteLineEnabled;
            if (routeOn)
                BaUi.StyleButton(_routeButtonImage, BaButtonStyle.Blue);
            else
                BaUi.StyleButton(_routeButtonImage, BaButtonStyle.Grey);
            _routeLabel.text = indoor
                ? (routeOn ? ModUiText.WayOutOn : ModUiText.WayOutOff)
                : (routeOn ? ModUiText.RouteOn : ModUiText.RouteOff);
            _routeLabel.color = ButtonLabelColor;

            var walkOn = indoor ? ModConfig.IndoorAutoWalkEnabled : ModConfig.AutoWalkEnabled;
            var onFoot = MovementModeDetector.CurrentMode == MovementMode.OnFoot;
            var inVehicle = MovementModeDetector.CurrentMode == MovementMode.Vehicle;
            if (inVehicle)
            {
                BaUi.StyleButton(_autoWalkButtonImage, BaButtonStyle.Grey);
                _autoWalkLabel.text = ModUiText.AutoDrive;
                _autoWalkLabel.color = AutoDriveSkipTravelService.IsInProgress ? LabelDisabled : ButtonLabelColor;
            }
            else if (!onFoot && !indoor)
            {
                BaUi.StyleButton(_autoWalkButtonImage, BaButtonStyle.Grey);
                _autoWalkLabel.text = ModUiText.AutoWalk;
                _autoWalkLabel.color = LabelDisabled;
            }
            else
            {
                if (walkOn)
                    BaUi.StyleButton(_autoWalkButtonImage, BaButtonStyle.Green);
                else
                    BaUi.StyleButton(_autoWalkButtonImage, BaButtonStyle.Grey);
                _autoWalkLabel.text = indoor
                    ? (walkOn ? ModUiText.GetOutOn : ModUiText.GetOut)
                    : (walkOn ? ModUiText.WalkOn : ModUiText.AutoWalk);
                _autoWalkLabel.color = ButtonLabelColor;
            }
        }

        internal static RectTransform GetVisualTestPanelRect() =>
            _panelRect != null && _root != null && _root.activeInHierarchy ? _panelRect : null;

        internal static void Destroy()
        {
            if (_root != null)
            {
                Object.Destroy(_root);
                _root = null;
                _panelRect = null;
                _loggedVisibleChrome = false;
                _forceApply = true;
                ClearButtonRefs();
            }
        }

        private static void ClearButtonRefs()
        {
            _routeButtonImage = null;
            _routeLabel = null;
            _autoWalkButtonImage = null;
            _autoWalkLabel = null;
            _panelTitleLabel = null;
        }

        private static void OnBookmarkPinClicked() => BookmarkPickService.TryOpenDialogAtCurrentPosition();

        private static void OnLastVehicleClicked()
        {
            if (ParkedVehicleDestinationService.TryNavigateToParkedVehicle())
                return;

            ModLog.Info("No parked vehicle position saved yet.");
        }

        private static void OnRouteClicked()
        {
            if (GameState.IsIndoorNavigationContext())
            {
                ModConfig.SetIndoorRouteLineEnabled(!ModConfig.IndoorRouteLineEnabled);
                if (!ModConfig.IndoorRouteLineEnabled)
                    RouteLineRenderer.Hide();
                RefreshVisual();
                return;
            }

            ModConfig.SetRouteLineEnabled(!ModConfig.RouteLineEnabled);
            if (!ModConfig.RouteLineEnabled)
                RouteLineRenderer.Hide();
            if (!ModConfig.WantsRouteComputation)
                PathFinderService.InvalidateCache();
            RefreshVisual();
        }

        private static void OnAutoWalkClicked()
        {
            if (GameState.IsIndoorNavigationContext())
            {
                ModConfig.SetIndoorAutoWalkEnabled(!ModConfig.IndoorAutoWalkEnabled);
                if (!ModConfig.IndoorAutoWalkEnabled)
                    IndoorAutoWalkService.Reset();
                RefreshVisual();
                return;
            }

            if (MovementModeDetector.CurrentMode == MovementMode.Vehicle)
            {
                AutoDriveSkipTravelService.RequestFromActionPanel();
                return;
            }

            if (MovementModeDetector.CurrentMode != MovementMode.OnFoot)
                return;

            ModConfig.SetAutoWalkEnabled(!ModConfig.AutoWalkEnabled);
            if (!ModConfig.AutoWalkEnabled)
                AutoWalkService.Reset();
            RefreshVisual();
        }
    }
}
