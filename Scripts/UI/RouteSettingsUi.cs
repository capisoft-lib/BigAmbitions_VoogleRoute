using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
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
        private const string RootName = "VoogleRoute_Settings_fluent_v19";
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
        private static Slider _windowScaleSlider;
        private static TextMeshProUGUI _windowScaleLabel;
        private static TextMeshProUGUI _windowScaleValue;
        private static bool _scaleDirty;
        private static float _scaleChangedAt;
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
                    new BaSettingsModalLayout(colorLineCount: 3, buttonCount: 1,
                        pinFooterClose: false, autoHeight: true),
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
            UiWindowScale.Register(_root, _panelRect);
            _titleLabel = built.Header.Find("Title")?.GetComponent<TextMeshProUGUI>();
            CreateWindowScaleRow(built.Panel.Find("Content") as RectTransform);

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
            if (_windowScaleSlider != null)
                _windowScaleSlider.SetValueWithoutNotify(ModConfig.WindowScalePercent);
            _root.SetActive(true);
            UiWindowScale.Refresh();
        }

        internal static void Close()
        {
            SavePendingScale();
            BaUiFocus.ReleaseForMovement();
            RestoreCanvasSortOrder();
            if (_root != null)
                _root.SetActive(false);
        }

        internal static void TickOverlay()
        {
            UpdateVisibility();

            if (_scaleDirty && Time.unscaledTime - _scaleChangedAt >= 0.25f)
                SavePendingScale();

            if (!_loweredForPicker)
                return;

            if (!VanillaColorPicker.IsOpen)
                RestoreCanvasSortOrder();
        }

        internal static void UpdateVisibility()
        {
            if (_root == null)
                return;

            if ((GameState.IsModUiHidden || GameState.IsOverlayBlockingNavigation()) && IsOpen)
                Close();
        }

        internal static void Toggle()
        {
            if (IsOpen)
                Close();
            else
                Open();
        }

        internal static void Destroy()
        {
            SavePendingScale();
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
            _windowScaleSlider = null;
            _windowScaleLabel = null;
            _windowScaleValue = null;
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
            if (_windowScaleLabel != null)
                _windowScaleLabel.text = ModUiText.WindowScaleLabel;
            if (_windowScaleValue != null)
                _windowScaleValue.text = ModConfig.WindowScalePercent + "%";

            RefreshColorSwatches();
        }

        private static void CreateWindowScaleRow(RectTransform content)
        {
            if (content == null)
                return;

            var row = BaUiWidgets.CreateRect(content, "WindowScaleRow");
            var rowLayout = row.gameObject.AddComponent<LayoutElement>();
            rowLayout.minHeight = rowLayout.preferredHeight =
                BaUiSettingsMetrics.RowHeight + BaUiSettingsMetrics.CloseButtonExtraHeight;

            var labelRect = BaUiWidgets.CreateRect(row, "Label");
            labelRect.anchorMin = new Vector2(0f, 0f);
            labelRect.anchorMax = new Vector2(0.30f, 1f);
            labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
            _windowScaleLabel = labelRect.gameObject.AddComponent<TextMeshProUGUI>();
            _windowScaleLabel.fontSize = 14f;
            _windowScaleLabel.alignment = TextAlignmentOptions.MidlineLeft;
            _windowScaleLabel.raycastTarget = false;
            BaUi.ApplyButtonFont(_windowScaleLabel);

            var track = BaUiWidgets.CreateRect(row, "WindowScaleSlider");
            track.anchorMin = new Vector2(0.33f, 0.35f);
            track.anchorMax = new Vector2(0.85f, 0.65f);
            track.offsetMin = track.offsetMax = Vector2.zero;
            var trackImage = track.gameObject.AddComponent<Image>();
            trackImage.color = new Color(0.16f, 0.22f, 0.30f, 1f);

            var fill = BaUiWidgets.CreateRect(track, "Fill");
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = Vector2.one;
            fill.offsetMin = fill.offsetMax = Vector2.zero;
            var fillImage = fill.gameObject.AddComponent<Image>();
            fillImage.color = new Color(0.12f, 0.69f, 1f, 1f);
            fillImage.raycastTarget = false;

            var handle = BaUiWidgets.CreateRect(track, "Handle");
            handle.anchorMin = new Vector2(0f, 0.5f);
            handle.anchorMax = new Vector2(0f, 0.5f);
            handle.sizeDelta = new Vector2(18f, 30f);
            var handleImage = handle.gameObject.AddComponent<Image>();
            handleImage.color = Color.white;

            _windowScaleSlider = track.gameObject.AddComponent<Slider>();
            _windowScaleSlider.fillRect = fill;
            _windowScaleSlider.handleRect = handle;
            _windowScaleSlider.targetGraphic = handleImage;
            _windowScaleSlider.minValue = 10f;
            _windowScaleSlider.maxValue = 160f;
            _windowScaleSlider.wholeNumbers = true;
            _windowScaleSlider.SetValueWithoutNotify(ModConfig.WindowScalePercent);
            _windowScaleSlider.onValueChanged.AddListener(value =>
            {
                ModConfig.SetWindowScalePercent(Mathf.RoundToInt(value), persist: false);
                if (_windowScaleValue != null)
                    _windowScaleValue.text = ModConfig.WindowScalePercent + "%";
                _scaleDirty = true;
                _scaleChangedAt = Time.unscaledTime;
            });

            var valueRect = BaUiWidgets.CreateRect(row, "Value");
            valueRect.anchorMin = new Vector2(0.87f, 0f);
            valueRect.anchorMax = Vector2.one;
            valueRect.offsetMin = valueRect.offsetMax = Vector2.zero;
            _windowScaleValue = valueRect.gameObject.AddComponent<TextMeshProUGUI>();
            _windowScaleValue.fontSize = 14f;
            _windowScaleValue.alignment = TextAlignmentOptions.MidlineRight;
            _windowScaleValue.raycastTarget = false;
            BaUi.ApplyButtonFont(_windowScaleValue);
        }

        private static void SavePendingScale()
        {
            if (!_scaleDirty)
                return;

            _scaleDirty = false;
            ModOptionsSaveStore.PersistFromModConfig();
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
