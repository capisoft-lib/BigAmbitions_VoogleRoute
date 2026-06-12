using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using VoogleRoute;

namespace VoogleRoute.UI
{
    /// <summary>Fenêtre de réglages vanilla (couleur de ligne + color picker jeu).</summary>
    internal static class RouteSettingsUi
    {
        private const string RootName = "VoogleRoute_Settings_sdk_v7";
        private const int CanvasSortOrder = 11500;
        private const float PanelScale = 1.5f;
        private const float PanelHeight = 360f;
        private const float FooterBottomPad = 18f;
        private const float RowHeight = 44f;
        private const float RowGap = 8f;
        private const float SwatchSize = 40f;
        private const float SwatchSpacing = 0f;

        private const int PickerOpenCanvasSortOrder = 8000;

        private static GameObject _root;
        private static Canvas _canvas;
        private static TextMeshProUGUI _titleLabel;
        private static TextMeshProUGUI _routeColorLabel;
        private static TextMeshProUGUI _chooseColorLabel;
        private static TextMeshProUGUI _closeLabel;
        private static bool _loweredForPicker;
        private static readonly List<Image> ColorSwatches = new List<Image>();
        private static readonly List<TextMeshProUGUI> SwatchTipLabels = new List<TextMeshProUGUI>();

        private static float PanelWidth => NavPanelLayout.PanelWidth * PanelScale;

        internal static bool IsOpen => _root != null && _root.activeSelf;

        internal static void EnsureCreated()
        {
            if (_root != null)
            {
                ModLog.Info("Settings UI already present.");
                return;
            }

            foreach (var legacyName in new[]
                     {
                         "VoogleRoute_Settings", "VoogleRoute_Settings_sdk",
                         "VoogleRoute_Settings_sdk_v2", "VoogleRoute_Settings_sdk_v3",
                         "VoogleRoute_Settings_sdk_v4", "VoogleRoute_Settings_sdk_v5",
                         "VoogleRoute_Settings_sdk_v6"
                     })
            {
                var legacy = GameObject.Find(legacyName);
                if (legacy != null && legacy.name != RootName)
                    Object.Destroy(legacy);
            }

            _root = new GameObject(RootName);
            Object.DontDestroyOnLoad(_root);

            GameStylePanelChrome.SetupOverlayCanvas(_root, CanvasSortOrder);
            _canvas = _root.GetComponent<Canvas>();

            var dim = CreateRect(_root.transform, "Dimmer");
            Stretch(dim);
            var dimImg = dim.gameObject.AddComponent<Image>();
            dimImg.color = new Color(0f, 0f, 0f, 0.5f);
            dimImg.raycastTarget = true;
            var dimBtn = dim.gameObject.AddComponent<Button>();
            dimBtn.targetGraphic = dimImg;
            dimBtn.onClick.AddListener((UnityAction)Close);

            var chrome = GameStylePanelChrome.Build(
                _root.transform,
                PanelWidth,
                PanelHeight,
                "Panel",
                NavPanelLayout.SettingsPanelHeaderWidenTrim);
            var metrics = chrome.Metrics;
            var scale = chrome.Scale;
            var contentInset = chrome.ContentInset;
            var panel = chrome.Panel;
            var closeButtonHeight = RowHeight + 4f;
            var footerReserve = closeButtonHeight + NavPanelLayout.BodyBottomPadding * scale + FooterBottomPad + 12f;

            var header = chrome.Header;
            var titleGo = CreateRect(header, "Title");
            titleGo.anchorMin = Vector2.zero;
            titleGo.anchorMax = Vector2.one;
            NavPanelLayout.ApplyHeaderTitleInsets(titleGo, metrics);
            _titleLabel = titleGo.gameObject.AddComponent<TextMeshProUGUI>();
            _titleLabel.fontSize = NavPanelLayout.TitleFontSize * scale;
            _titleLabel.fontStyle = FontStyles.Bold;
            _titleLabel.color = GameUiStyle.TitleColor;
            _titleLabel.alignment = TextAlignmentOptions.Center;
            GameUiStyle.ApplyTitleFont(_titleLabel);

            var scrollTop = metrics.HeaderHeight + NavPanelLayout.BodyTopPadding * scale;
            var scrollGo = CreateRect(panel, "Scroll");
            scrollGo.anchorMin = Vector2.zero;
            scrollGo.anchorMax = Vector2.one;
            scrollGo.offsetMin = new Vector2(contentInset, footerReserve);
            scrollGo.offsetMax = new Vector2(-contentInset, -scrollTop);

            var scroll = scrollGo.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            var viewport = CreateRect(scrollGo, "Viewport");
            Stretch(viewport);
            viewport.gameObject.AddComponent<RectMask2D>();
            scroll.viewport = viewport;

            var content = CreateRect(viewport, "Content");
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(0f, 0f);

            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.spacing = RowGap;
            layout.padding = new RectOffset(4, 4, 6, 10);

            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = content;

            BuildRows(content, scale);

            var closeRow = CreateRect(panel, "CloseRow");
            closeRow.anchorMin = new Vector2(0f, 0f);
            closeRow.anchorMax = new Vector2(1f, 0f);
            closeRow.pivot = new Vector2(0.5f, 0f);
            closeRow.anchoredPosition = new Vector2(0f, FooterBottomPad);
            closeRow.sizeDelta = new Vector2(-contentInset * 2f, closeButtonHeight);

            var closeImg = GameUiStyle.CreateButtonGraphic(closeRow, scale, GameUiStyle.ApplyButtonBlue);
            var closeBtn = closeRow.gameObject.AddComponent<Button>();
            closeBtn.targetGraphic = closeImg;
            closeBtn.onClick.AddListener((UnityAction)Close);

            var closeLabelGo = CreateRect(closeRow, "Label");
            Stretch(closeLabelGo);
            closeLabelGo.offsetMin = new Vector2(NavPanelLayout.ButtonTextPaddingX * scale, 0f);
            closeLabelGo.offsetMax = new Vector2(-NavPanelLayout.ButtonTextPaddingX * scale,
                -NavPanelLayout.ButtonLabelBottomInset * scale);
            _closeLabel = closeLabelGo.gameObject.AddComponent<TextMeshProUGUI>();
            _closeLabel.fontSize = NavPanelLayout.ButtonFontSize * scale;
            _closeLabel.fontStyle = FontStyles.UpperCase;
            _closeLabel.alignment = TextAlignmentOptions.Center;
            _closeLabel.color = Color.white;
            _closeLabel.raycastTarget = false;
            GameUiStyle.ApplyButtonFont(_closeLabel);

            _root.SetActive(false);
            RefreshLocalizedText();
            ModLog.Info("Settings UI created (route color picker).");
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

        internal static void Destroy()
        {
            if (_root != null)
            {
                Object.Destroy(_root);
                _root = null;
            }

            _canvas = null;
            _titleLabel = null;
            _routeColorLabel = null;
            _chooseColorLabel = null;
            _closeLabel = null;
            _loweredForPicker = false;
            ColorSwatches.Clear();
            SwatchTipLabels.Clear();
        }

        internal static void RefreshLocalizedText()
        {
            if (_titleLabel != null)
                _titleLabel.text = ModUiText.SettingsTitle;
            if (_routeColorLabel != null)
                _routeColorLabel.text = ModUiText.SettingRouteLineColor;
            if (_chooseColorLabel != null)
                _chooseColorLabel.text = ModUiText.SettingChooseColor;
            if (_closeLabel != null)
                _closeLabel.text = ModUiText.SettingClose;

            var tipKeys = new[]
            {
                ModUiText.ColorPresetNeonBlue,
                ModUiText.ColorPresetGreen,
                ModUiText.ColorPresetOrange,
                ModUiText.ColorPresetMagenta,
                ModUiText.ColorPresetWhite,
            };
            for (var i = 0; i < SwatchTipLabels.Count && i < tipKeys.Length; i++)
            {
                if (SwatchTipLabels[i] != null)
                    SwatchTipLabels[i].text = tipKeys[i];
            }

            RefreshAll();
        }

        private static void BuildRows(RectTransform content, float scale)
        {
            AddSectionLabel(content, ModUiText.SettingRouteLineColor);
            AddColorPresets(content);
            AddActionButton(content, scale, ModUiText.SettingChooseColor, GameUiStyle.ApplyButtonBlue, OpenNativeColorPicker);
        }

        private static void AddSectionLabel(RectTransform parent, string text)
        {
            var row = CreateRow(parent, 26f);
            var label = CreateLabel(row, "Label", 14f, FontStyles.Bold);
            label.color = new Color(0.85f, 0.9f, 1f, 1f);
            label.text = text;
            _routeColorLabel = label;
        }

        private static void AddColorPresets(RectTransform parent)
        {
            var rowHeight = SwatchSize + 30f;
            var row = CreateRow(parent, rowHeight);

            var presets = new[]
            {
                new Color(ModConfig.RouteNeonBlueR, ModConfig.RouteNeonBlueG, ModConfig.RouteNeonBlueB, ModConfig.RouteNeonBlueA),
                new Color(0.25f, 0.95f, 0.35f, 0.92f),
                new Color(1f, 0.55f, 0.12f, 0.92f),
                new Color(0.92f, 0.22f, 0.85f, 0.92f),
                new Color(1f, 1f, 1f, 0.92f),
            };

            var names = new[]
            {
                ModUiText.ColorPresetNeonBlue,
                ModUiText.ColorPresetGreen,
                ModUiText.ColorPresetOrange,
                ModUiText.ColorPresetMagenta,
                ModUiText.ColorPresetWhite,
            };

            var stripWidth = presets.Length * SwatchSize + (presets.Length - 1) * SwatchSpacing;
            var strip = CreateRect(row, "SwatchStrip");
            strip.anchorMin = new Vector2(0f, 0.5f);
            strip.anchorMax = new Vector2(0f, 0.5f);
            strip.pivot = new Vector2(0f, 0.5f);
            strip.anchoredPosition = new Vector2(2f, 0f);
            strip.sizeDelta = new Vector2(stripWidth, rowHeight);

            var hLayout = strip.gameObject.AddComponent<HorizontalLayoutGroup>();
            hLayout.spacing = SwatchSpacing;
            hLayout.childAlignment = TextAnchor.UpperLeft;
            hLayout.childControlWidth = false;
            hLayout.childControlHeight = false;
            hLayout.childForceExpandWidth = false;
            hLayout.childForceExpandHeight = false;
            hLayout.padding = new RectOffset(0, 0, 0, 0);

            ColorSwatches.Clear();
            SwatchTipLabels.Clear();
            for (var i = 0; i < presets.Length; i++)
            {
                var color = presets[i];
                var tipText = names[i];
                AddSwatch(strip, rowHeight, color, tipText);
            }
        }

        private static void AddSwatch(RectTransform row, float rowHeight, Color color, string tipText)
        {
            var cell = CreateRect(row, "SwatchCell");
            var cellLe = cell.gameObject.AddComponent<LayoutElement>();
            cellLe.preferredWidth = SwatchSize;
            cellLe.preferredHeight = rowHeight - 4f;

            var column = CreateRect(cell, "Column");
            Stretch(column);

            var swatchRt = CreateRect(column, "Swatch");
            swatchRt.anchorMin = new Vector2(0.5f, 1f);
            swatchRt.anchorMax = new Vector2(0.5f, 1f);
            swatchRt.pivot = new Vector2(0.5f, 1f);
            swatchRt.sizeDelta = new Vector2(SwatchSize, SwatchSize);
            swatchRt.anchoredPosition = Vector2.zero;

            var img = swatchRt.gameObject.AddComponent<Image>();
            img.color = color;
            ColorSwatches.Add(img);

            var outline = swatchRt.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 1f, 1f, 0.35f);
            outline.effectDistance = new Vector2(1f, -1f);

            var btn = swatchRt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            var captured = color;
            btn.onClick.AddListener((UnityAction)(() =>
            {
                ModConfig.SetRouteLineColor(captured);
                RefreshColorSelection();
            }));

            var tipRt = CreateRect(column, "Tip");
            tipRt.anchorMin = new Vector2(0f, 0f);
            tipRt.anchorMax = new Vector2(1f, 0f);
            tipRt.pivot = new Vector2(0.5f, 0f);
            tipRt.sizeDelta = new Vector2(0f, 18f);
            tipRt.anchoredPosition = Vector2.zero;

            var tip = tipRt.gameObject.AddComponent<TextMeshProUGUI>();
            tip.text = tipText;
            tip.fontSize = 10f;
            tip.alignment = TextAlignmentOptions.Center;
            tip.color = new Color(0.8f, 0.85f, 0.95f, 1f);
            tip.raycastTarget = false;
            tip.enableWordWrapping = false;
            tip.overflowMode = TextOverflowModes.Overflow;
            GameUiStyle.ApplyButtonFont(tip);
            SwatchTipLabels.Add(tip);
        }

        private static void AddActionButton(
            RectTransform parent,
            float scale,
            string labelText,
            System.Action<Image> style,
            UnityAction onClick)
        {
            var row = CreateRow(parent, RowHeight + 4f);
            var img = GameUiStyle.CreateButtonGraphic(row, scale, style);
            var btn = row.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(onClick);

            var label = CreateLabel(row, "Label", 14f, FontStyles.UpperCase);
            label.text = labelText;
            label.alignment = TextAlignmentOptions.Center;
            _chooseColorLabel = label;
        }

        private static void OpenNativeColorPicker()
        {
            if (!VanillaColorPicker.TryOpen(ModConfig.LineColor, color =>
                {
                    ModConfig.SetRouteLineColor(color);
                    RefreshColorSelection();
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

        private static void RefreshAll()
        {
            RefreshColorSelection();
        }

        private static void RefreshColorSelection()
        {
            var current = ModConfig.LineColor;
            foreach (var swatch in ColorSwatches)
            {
                if (swatch == null)
                    continue;

                var selected = ColorsMatch(swatch.color, current);
                swatch.transform.localScale = selected ? Vector3.one * 1.1f : Vector3.one;

                var outline = swatch.GetComponent<Outline>();
                if (outline != null)
                {
                    outline.effectColor = selected
                        ? new Color(1f, 1f, 1f, 0.95f)
                        : new Color(1f, 1f, 1f, 0.25f);
                }
            }
        }

        private static bool ColorsMatch(Color a, Color b) =>
            Mathf.Abs(a.r - b.r) < 0.06f &&
            Mathf.Abs(a.g - b.g) < 0.06f &&
            Mathf.Abs(a.b - b.b) < 0.06f;

        private static TextMeshProUGUI CreateLabel(RectTransform parent, string name, float fontSize, FontStyles style)
        {
            var rt = CreateRect(parent, name);
            Stretch(rt, 2f, 2f);
            var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.raycastTarget = false;
            tmp.margin = Vector4.zero;
            tmp.enableWordWrapping = true;
            GameUiStyle.ApplyButtonFont(tmp);
            return tmp;
        }

        private static RectTransform CreateRow(RectTransform parent, float height)
        {
            var row = CreateRect(parent, "Row");
            var le = row.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = height;
            le.minHeight = height;
            return row;
        }

        private static RectTransform CreateRect(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.AddComponent<RectTransform>();
        }

        private static void Stretch(RectTransform rt, float padX = 0f, float padY = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(padX, padY);
            rt.offsetMax = new Vector2(-padX, -padY);
        }
    }
}
