using Il2CppTMPro;
using MelonLoader;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using VoogleRoute.Localization;
using VoogleRoute.Navigation;
using VoogleRoute.Rendering;
using VoogleRoute.Update;
using Object = UnityEngine.Object;

namespace VoogleRoute.UI;

/// <summary>In-game settings overlay for Voogle Route (opened from the VOOGLE ROUTE panel).</summary>
internal static class RouteSettingsUi
{
    private const string RootName = "VoogleRoute_Settings_v3";
    private const int CanvasSortOrder = 11500;
    private const float PanelWidth = 540f;
    private const float PanelHeight = 600f;
    private const float FooterHeight = 52f;
    private const float RowHeight = 44f;
    private const float RowGap = 8f;
    private const float ToggleWidth = 72f;
    private const float SwatchSize = 40f;

    private static GameObject? _root;
    private static TextMeshProUGUI? _titleLabel;
    private static TextMeshProUGUI? _closeLabel;
    private static RectTransform? _listContent;
    private static readonly List<ToggleRow> ToggleRows = new();
    private static readonly List<Image> ColorSwatches = new();

    private sealed class ToggleRow
    {
        public MelonPreferences_Entry<bool> Entry = null!;
        public StringKey LabelKey;
        public TextMeshProUGUI? Label;
        public TextMeshProUGUI? StateLabel;
        public Image? ButtonImage;
        public Action? OnChanged;
    }

    internal static bool IsOpen => _root != null && _root.activeSelf;

    internal static void EnsureCreated()
    {
        if (_root != null)
            return;

        foreach (var legacyName in new[] { "VoogleRoute_Settings", "VoogleRoute_Settings_v2", "VoogleRoute_Settings_v3" })
        {
            var legacy = GameObject.Find(legacyName);
            if (legacy != null && legacyName != RootName)
                Object.Destroy(legacy);
        }

        GameUiStyle.EnsureInitialized();

        var scale = PanelWidth / NavPanelLayout.PanelWidth;
        var metrics = NavPanelLayout.CreateMetrics(scale);
        var contentInset = metrics.ContentInset;

        _root = new GameObject(RootName);
        Object.DontDestroyOnLoad(_root);

        var canvas = _root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = CanvasSortOrder;

        var scaler = _root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        _root.AddComponent<GraphicRaycaster>();

        var dim = CreateRect(_root.transform, "Dimmer");
        Stretch(dim);
        var dimImg = dim.gameObject.AddComponent<Image>();
        dimImg.color = new Color(0f, 0f, 0f, 0.5f);
        dimImg.raycastTarget = true;
        var dimBtn = dim.gameObject.AddComponent<Button>();
        dimBtn.targetGraphic = dimImg;
        dimBtn.onClick.AddListener((UnityAction)Close);

        var panel = CreateRect(_root.transform, "Panel");
        panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.pivot = new Vector2(0.5f, 0.5f);
        panel.sizeDelta = new Vector2(PanelWidth, PanelHeight);

        var background = CreateRect(panel, "Background");
        NavPanelLayout.ApplyBodyFrame(background, scale);
        var backgroundImg = background.gameObject.AddComponent<Image>();
        backgroundImg.raycastTarget = true;
        GameUiStyle.ApplyPanelBg(backgroundImg);

        var header = CreateRect(panel, "Header");
        NavPanelLayout.ApplyHeaderFrame(header, metrics);
        var headerImg = header.gameObject.AddComponent<Image>();
        headerImg.raycastTarget = false;
        GameUiStyle.ApplyHeaderBg(headerImg);

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
        scrollGo.offsetMin = new Vector2(contentInset, FooterHeight);
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
        _listContent = content;

        var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.spacing = RowGap;
        layout.padding = new RectOffset(4, 4, 6, 6);

        var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.content = content;

        BuildRows(content);

        var closeRow = CreateRect(panel, "CloseRow");
        closeRow.anchorMin = new Vector2(0f, 0f);
        closeRow.anchorMax = new Vector2(1f, 0f);
        closeRow.pivot = new Vector2(0.5f, 0f);
        closeRow.anchoredPosition = new Vector2(0f, NavPanelLayout.BodyBottomPadding * scale + 6f);
        closeRow.sizeDelta = new Vector2(-contentInset * 2f, NavPanelLayout.ButtonHeight * scale);

        var closeImg = closeRow.gameObject.AddComponent<Image>();
        GameUiStyle.ApplyButtonBlue(closeImg);
        var closeBtn = closeRow.gameObject.AddComponent<Button>();
        closeBtn.targetGraphic = closeImg;
        closeBtn.onClick.AddListener((UnityAction)Close);

        var closeLabelGo = CreateRect(closeRow, "Label");
        Stretch(closeLabelGo);
        _closeLabel = closeLabelGo.gameObject.AddComponent<TextMeshProUGUI>();
        _closeLabel.fontSize = 15f;
        _closeLabel.fontStyle = FontStyles.UpperCase;
        _closeLabel.alignment = TextAlignmentOptions.Center;
        _closeLabel.color = Color.white;
        _closeLabel.raycastTarget = false;
        GameUiStyle.ApplyButtonFont(_closeLabel);

        _root.SetActive(false);
        RefreshLocalizedText();
    }

    internal static void Open()
    {
        EnsureCreated();
        if (_root == null || _listContent == null)
            return;

        RefreshAll();
        LayoutRebuilder.ForceRebuildLayoutImmediate(_listContent);
        _root.SetActive(true);
        LayoutRebuilder.ForceRebuildLayoutImmediate(_listContent);
    }

    internal static void Close()
    {
        if (_root != null)
            _root.SetActive(false);
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

        _titleLabel = null;
        _closeLabel = null;
        _listContent = null;
        ToggleRows.Clear();
        ColorSwatches.Clear();
    }

    internal static void RefreshLocalizedText()
    {
        if (_titleLabel != null)
            _titleLabel.text = ModLocalization.Get(StringKey.SettingsTitle);

        if (_closeLabel != null)
            _closeLabel.text = ModLocalization.Get(StringKey.SettingClose);

        RefreshAll();
    }

    private static void BuildRows(RectTransform content)
    {
        AddSectionLabel(content, StringKey.SettingRouteLineColor);
        AddColorPresets(content);

        AddToggle(content, StringKey.SettingCheckForUpdates, ModConfig.CheckForUpdates);
        AddToggle(content, StringKey.SettingAutoDownloadUpdates, ModConfig.AutoDownloadUpdates);
        AddToggle(content, StringKey.SettingPromptInstallUpdate, ModConfig.PromptInstallUpdate);

        AddActionButton(content, StringKey.SettingCheckNow, GameUiStyle.ApplyButtonBlue,
            (UnityAction)(() => UpdateService.RequestVersionCheck()));

        AddToggle(content, StringKey.SettingShowTurnGuidance, ModConfig.ShowTurnGuidance);
        AddToggle(content, StringKey.SettingShowIntersectionArrows, ModConfig.ShowIntersectionArrows);
        AddToggle(content, StringKey.SettingShowFullRouteLine, ModConfig.ShowFullRouteLine,
            () => PathFinderService.InvalidateCache());
    }

    private static void AddSectionLabel(RectTransform parent, StringKey key)
    {
        var row = CreateRow(parent, 26f);
        var label = CreateLabel(row, "Label", 14f, FontStyles.Bold);
        label.color = new Color(0.85f, 0.9f, 1f, 1f);
        label.text = ModLocalization.Get(key);
    }

    private static void AddColorPresets(RectTransform parent)
    {
        var rowHeight = SwatchSize + 30f;
        var row = CreateRow(parent, rowHeight);
        row.gameObject.AddComponent<RectMask2D>();

        var presets = new (Color color, StringKey nameKey)[]
        {
            (new Color(ModConfig.RouteNeonBlueR, ModConfig.RouteNeonBlueG, ModConfig.RouteNeonBlueB, ModConfig.RouteNeonBlueA),
                StringKey.ColorPresetNeonBlue),
            (new Color(0.25f, 0.95f, 0.35f, 0.92f), StringKey.ColorPresetGreen),
            (new Color(1f, 0.55f, 0.12f, 0.92f), StringKey.ColorPresetOrange),
            (new Color(0.92f, 0.22f, 0.85f, 0.92f), StringKey.ColorPresetMagenta),
            (new Color(1f, 1f, 1f, 0.92f), StringKey.ColorPresetWhite),
        };

        var hLayout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        hLayout.spacing = 10f;
        hLayout.childControlWidth = false;
        hLayout.childControlHeight = false;
        hLayout.padding = new RectOffset(2, 2, 0, 0);

        ColorSwatches.Clear();
        foreach (var (color, nameKey) in presets)
        {
            var cell = CreateRect(row, nameKey.ToString());
            var cellLe = cell.gameObject.AddComponent<LayoutElement>();
            cellLe.preferredWidth = SwatchSize + 6f;
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
            tip.text = ModLocalization.Get(nameKey);
            tip.fontSize = 10f;
            tip.alignment = TextAlignmentOptions.Center;
            tip.color = new Color(0.8f, 0.85f, 0.95f, 1f);
            tip.raycastTarget = false;
            tip.enableWordWrapping = false;
            tip.overflowMode = TextOverflowModes.Overflow;
            GameUiStyle.ApplyButtonFont(tip);
        }
    }

    private static void AddToggle(
        RectTransform parent,
        StringKey labelKey,
        MelonPreferences_Entry<bool> entry,
        Action? onChanged = null)
    {
        var row = CreateRow(parent, RowHeight);

        var hLayout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        hLayout.spacing = 10f;
        hLayout.childControlWidth = true;
        hLayout.childControlHeight = true;
        hLayout.padding = new RectOffset(2, 2, 4, 4);

        var labelGo = CreateRect(row, "Label");
        var labelFlex = labelGo.gameObject.AddComponent<LayoutElement>();
        labelFlex.flexibleWidth = 1f;
        labelFlex.minWidth = 200f;
        var label = CreateLabel(labelGo, "Text", 13f, FontStyles.Normal);
        label.enableWordWrapping = true;
        label.overflowMode = TextOverflowModes.Truncate;
        label.text = ModLocalization.Get(labelKey);

        var btnGo = CreateRect(row, "Toggle");
        var btnLayout = btnGo.gameObject.AddComponent<LayoutElement>();
        btnLayout.preferredWidth = ToggleWidth;
        btnLayout.minHeight = 32f;

        var btnImg = btnGo.gameObject.AddComponent<Image>();
        GameUiStyle.ApplyButtonGrey(btnImg);
        var btn = btnGo.gameObject.AddComponent<Button>();
        btn.targetGraphic = btnImg;

        var stateLabel = CreateLabel(btnGo, "State", 13f, FontStyles.UpperCase);
        stateLabel.alignment = TextAlignmentOptions.Center;

        var toggleRow = new ToggleRow
        {
            Entry = entry,
            LabelKey = labelKey,
            Label = label,
            StateLabel = stateLabel,
            ButtonImage = btnImg,
            OnChanged = onChanged,
        };
        ToggleRows.Add(toggleRow);

        btn.onClick.AddListener((UnityAction)(() =>
        {
            entry.Value = !entry.Value;
            ModConfig.Save();
            toggleRow.OnChanged?.Invoke();
            RefreshToggle(toggleRow);
        }));

        RefreshToggle(toggleRow);
    }

    private static void AddActionButton(
        RectTransform parent,
        StringKey labelKey,
        Action<Image> style,
        UnityAction onClick)
    {
        var row = CreateRow(parent, RowHeight + 4f);
        var img = row.gameObject.AddComponent<Image>();
        style(img);
        var btn = row.gameObject.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(onClick);

        var label = CreateLabel(row, "Label", 14f, FontStyles.UpperCase);
        label.text = ModLocalization.Get(labelKey);
        label.alignment = TextAlignmentOptions.Center;
    }

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

    private static void RefreshAll()
    {
        foreach (var row in ToggleRows)
            RefreshToggle(row);

        RefreshColorSelection();
    }

    private static void RefreshToggle(ToggleRow row)
    {
        if (row.Label != null)
            row.Label.text = ModLocalization.Get(row.LabelKey);

        var on = row.Entry.Value;
        if (row.StateLabel != null)
            row.StateLabel.text = on
                ? ModLocalization.Get(StringKey.SettingOn)
                : ModLocalization.Get(StringKey.SettingOff);

        if (row.ButtonImage != null)
        {
            if (on)
                GameUiStyle.ApplyButtonGreen(row.ButtonImage);
            else
                GameUiStyle.ApplyButtonGrey(row.ButtonImage);
        }
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
                outline.effectColor = selected
                    ? new Color(1f, 1f, 1f, 0.95f)
                    : new Color(1f, 1f, 1f, 0.25f);
        }
    }

    private static bool ColorsMatch(Color a, Color b) =>
        Mathf.Abs(a.r - b.r) < 0.06f &&
        Mathf.Abs(a.g - b.g) < 0.06f &&
        Mathf.Abs(a.b - b.b) < 0.06f;

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
