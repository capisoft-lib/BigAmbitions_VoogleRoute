using TMPro;
using UnityEngine;
using UnityEngine.Events;
using VoogleRoute;

using Capisoft.Lib.BaUnifiedUI.Controls;
using Capisoft.Lib.BaUnifiedUI.Core;
using Capisoft.Lib.BaUnifiedUI.Fluent;
using Capisoft.Lib.BaUnifiedUI.Layout;

namespace VoogleRoute.UI
{
    /// <summary>Route color settings modal — fluent Content API (same pipeline as bookmarks).</summary>
    internal static class RouteSettingsUi
    {
        private const string RootName = "VoogleRoute_Settings_fluent_v18";
        private const string DragPositionId = "voogleroute:settings";
        private const float CloseButtonExtraInset = 5f;
        private const int CanvasSortOrder = 11500;
        private const int PickerOpenCanvasSortOrder = 8000;

        private static GameObject _root;
        private static Canvas _canvas;
        private static RectTransform _panelRect;
        private static TextMeshProUGUI _titleLabel;
        private static TextMeshProUGUI _footColorLabel;
        private static TextMeshProUGUI _indoorColorLabel;
        private static TextMeshProUGUI _vehicleColorLabel;
        private static TextMeshProUGUI _footChooseColorLabel;
        private static TextMeshProUGUI _indoorChooseColorLabel;
        private static TextMeshProUGUI _vehicleChooseColorLabel;
        private static BaUiColorSwatchDisplay _footSwatch;
        private static BaUiColorSwatchDisplay _indoorSwatch;
        private static BaUiColorSwatchDisplay _vehicleSwatch;
        private static bool _loweredForPicker;

        private enum ColorTarget
        {
            Foot,
            Indoor,
            Vehicle
        }

        private static float PanelWidth => BaUi.Layout.SettingsPanelWidth();

        internal static bool IsOpen => _root != null && _root.activeSelf;

        internal static void EnsureCreated()
        {
            VoogleRoutePanelLifecycle.DestroyIfStale(ref _root, RootName, Destroy);
            if (_root != null)
                return;

            BaUi.EnsureReady();

            var built = BaUi.Modal(RootName, CanvasSortOrder, 0.5f)
                .OnDismiss(Close)
                .Panel(BaPanelRecipe.Settings, PanelWidth)
                .Draggable(DragPositionId)
                .Header(h => h
                    .TitleLeft(ModUiText.SettingsTitle)
                    .CloseButton(Close, CloseButtonExtraInset))
                .Content(c => c.SettingsModal(
                    BaSettingsModalLayout.ColorLines(3, pinFooterClose: false, autoHeight: true),
                    m =>
                {
                    m.ColorLine(
                        ModUiText.SettingFootRouteColor,
                        ModConfig.FootLineColor,
                        ModUiText.SettingChooseColor,
                        BaButtonStyle.Blue,
                        BaUiFocus.Wrap((UnityAction)(() => OpenNativeColorPicker(ColorTarget.Foot))),
                        out _footColorLabel,
                        out _footSwatch,
                        out _footChooseColorLabel);

                    m.ColorLine(
                        ModUiText.SettingIndoorRouteColor,
                        ModConfig.IndoorFootLineColor,
                        ModUiText.SettingChooseColor,
                        BaButtonStyle.Blue,
                        BaUiFocus.Wrap((UnityAction)(() => OpenNativeColorPicker(ColorTarget.Indoor))),
                        out _indoorColorLabel,
                        out _indoorSwatch,
                        out _indoorChooseColorLabel);

                    m.ColorLine(
                        ModUiText.SettingVehicleRouteColor,
                        ModConfig.VehicleLineColor,
                        ModUiText.SettingChooseColor,
                        BaButtonStyle.Blue,
                        BaUiFocus.Wrap((UnityAction)(() => OpenNativeColorPicker(ColorTarget.Vehicle))),
                        out _vehicleColorLabel,
                        out _vehicleSwatch,
                        out _vehicleChooseColorLabel);
                }))
                .Build();

            _root = built.Root;
            _canvas = _root.GetComponent<Canvas>();
            _panelRect = built.Panel;
            _titleLabel = built.Header.Find("Title")?.GetComponent<TextMeshProUGUI>();

            _root.SetActive(false);
            RefreshLocalizedText();
            VoogleRouteUiDiagnostics.LogPanelChrome(
                "settings-built",
                _panelRect,
                PanelWidth,
                BaUi.Layout.SettingsPanelHeaderWidenTrim);
            Debug.Log(
                "[VoogleRoute] Settings UI built | root=" + RootName +
                " | lib=" + BaUi.LibraryVersion +
                " | layout_rev=" + BaUi.LayoutRevision +
                " | panel_h=" + built.PanelHeight.ToString("F1"));
            ModLog.Info("Settings UI created (fluent Content).");
        }

        internal static void Open()
        {
            EnsureCreated();
            if (_root == null)
                return;

            RefreshLocalizedText();
            _root.SetActive(true);
        }

        internal static void Close()
        {
            BaUiFocus.ReleaseForMovement();
            RestoreCanvasSortOrder();
            if (_root != null)
                _root.SetActive(false);
        }

        internal static void TickOverlay()
        {
            UpdateVisibility();

            if (!_loweredForPicker)
                return;

            if (!VanillaColorPicker.IsOpen)
                RestoreCanvasSortOrder();
        }

        internal static void UpdateVisibility()
        {
            if (_root == null)
                return;

            if (GameState.IsOverlayBlockingNavigation() && IsOpen)
                Close();
        }

        internal static void Toggle()
        {
            if (IsOpen)
                Close();
            else
                Open();
        }

        internal static RectTransform GetVisualTestPanelRect() =>
            _panelRect != null && _root != null && _root.activeInHierarchy ? _panelRect : null;

        internal static void Destroy()
        {
            if (_root != null)
            {
                Object.Destroy(_root);
                _root = null;
            }

            _canvas = null;
            _panelRect = null;
            _titleLabel = null;
            _footColorLabel = null;
            _indoorColorLabel = null;
            _vehicleColorLabel = null;
            _footChooseColorLabel = null;
            _indoorChooseColorLabel = null;
            _vehicleChooseColorLabel = null;
            _footSwatch = null;
            _indoorSwatch = null;
            _vehicleSwatch = null;
            _loweredForPicker = false;
        }

        internal static void RefreshLocalizedText()
        {
            if (_titleLabel != null)
                _titleLabel.text = ModUiText.SettingsTitle;
            if (_footColorLabel != null)
                _footColorLabel.text = ModUiText.SettingFootRouteColor;
            if (_indoorColorLabel != null)
                _indoorColorLabel.text = ModUiText.SettingIndoorRouteColor;
            if (_vehicleColorLabel != null)
                _vehicleColorLabel.text = ModUiText.SettingVehicleRouteColor;
            if (_footChooseColorLabel != null)
                _footChooseColorLabel.text = ModUiText.SettingChooseColor;
            if (_indoorChooseColorLabel != null)
                _indoorChooseColorLabel.text = ModUiText.SettingChooseColor;
            if (_vehicleChooseColorLabel != null)
                _vehicleChooseColorLabel.text = ModUiText.SettingChooseColor;

            RefreshColorSwatches();
        }

        private static void ApplyColor(ColorTarget target, Color color)
        {
            switch (target)
            {
                case ColorTarget.Foot:
                    ModConfig.SetFootLineColor(color);
                    break;
                case ColorTarget.Indoor:
                    ModConfig.SetIndoorFootLineColor(color);
                    break;
                default:
                    ModConfig.SetVehicleLineColor(color);
                    break;
            }
        }

        private static Color GetColor(ColorTarget target) =>
            target switch
            {
                ColorTarget.Foot => ModConfig.FootLineColor,
                ColorTarget.Indoor => ModConfig.IndoorFootLineColor,
                _ => ModConfig.VehicleLineColor
            };

        private static void OpenNativeColorPicker(ColorTarget target)
        {
            if (!VanillaColorPicker.TryOpen(GetColor(target), color =>
                {
                    ApplyColor(target, color);
                    RefreshColorSwatches();
                }))
                return;

            LowerCanvasForPicker();
        }

        private static void LowerCanvasForPicker()
        {
            if (_canvas == null)
                return;

            _canvas.sortingOrder = PickerOpenCanvasSortOrder;
            _loweredForPicker = true;
        }

        private static void RestoreCanvasSortOrder()
        {
            if (_canvas != null)
                _canvas.sortingOrder = CanvasSortOrder;
            _loweredForPicker = false;
        }

        private static void RefreshColorSwatches()
        {
            _footSwatch?.SetColor(ModConfig.FootLineColor);
            _indoorSwatch?.SetColor(ModConfig.IndoorFootLineColor);
            _vehicleSwatch?.SetColor(ModConfig.VehicleLineColor);
        }
    }
}
