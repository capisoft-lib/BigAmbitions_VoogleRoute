using Il2CppTMPro;

using VoogleRoute.Localization;
using VoogleRoute.Navigation;
using VoogleRoute.Rendering;

using UnityEngine;
using UnityEngine.UI;

using Object = UnityEngine.Object;

namespace VoogleRoute.UI;

/// <summary>
/// Panneau VOOGLE ROUTE (gabarit MoreByUs via <see cref="NavPanelLayout"/>).
/// </summary>
public static class RouteToggleHud
{
    private const string RootName = "VoogleRoute_HudRoot_v1";

    private static GameObject? _root;
    private static RectTransform? _panelRect;
    private static bool _legacyCleaned;

    private static Image? _routeButtonImage;
    private static TextMeshProUGUI? _routeLabel;
    private static Image? _autoWalkButtonImage;
    private static TextMeshProUGUI? _autoWalkLabel;
    private static TextMeshProUGUI? _panelTitleLabel;

    // Cache d'état : aucun travail UI tant que rien ne change.
    private static bool _lastActive;
    private static bool _lastRouteOn;
    private static bool _lastWalkOn;
    private static bool _lastOnFoot;
    private static float _lastOffsetY = float.NaN;
    private static bool _forceApply = true;

    private static readonly Color LabelDisabled = new(1f, 1f, 1f, 0.5f);
    private static readonly Color ButtonLabelColor = Color.white;

    public static void EnsureCreated()
    {
        if (!_legacyCleaned)
        {
            _legacyCleaned = true;
            DestroyLegacyRoots();
        }

        if (_root != null)
            return;

        GameUiStyle.EnsureInitialized();

        _root = new GameObject(RootName);
        Object.DontDestroyOnLoad(_root);

        var canvas = _root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9000;

        var scaler = _root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        scaler.scaleFactor = 1f;
        _root.AddComponent<GraphicRaycaster>();

        var layout = NavPanelLayout.CreateMetrics(Mathf.Max(1f, ModConfig.HudButtonScale.Value));

        var panel = CreateRect(_root.transform, "NavPanel");
        _panelRect = panel;
        panel.anchorMin = panel.anchorMax = new Vector2(0f, 0f);
        panel.pivot = new Vector2(0f, 0f);
        panel.anchoredPosition = NavPanelLayout.GetScreenPosition(ModConfig.NavHudOffsetY.Value);
        panel.sizeDelta = new Vector2(layout.PanelWidth, layout.PanelHeight);

        var background = CreateRect(panel, "Background");
        NavPanelLayout.ApplyBodyFrame(background, layout.Scale);
        var backgroundImg = background.gameObject.AddComponent<Image>();
        backgroundImg.raycastTarget = true;
        GameUiStyle.ApplyPanelBg(backgroundImg);

        var header = CreateRect(panel, "Header");
        NavPanelLayout.ApplyHeaderFrame(header, layout);
        var headerImg = header.gameObject.AddComponent<Image>();
        headerImg.raycastTarget = false;
        GameUiStyle.ApplyHeaderBg(headerImg);

        var titleGo = CreateRect(header, "Title");
        titleGo.anchorMin = Vector2.zero;
        titleGo.anchorMax = Vector2.one;
        NavPanelLayout.ApplyHeaderTitleInsets(titleGo, layout);
        _panelTitleLabel = titleGo.gameObject.AddComponent<TextMeshProUGUI>();
        _panelTitleLabel.text = ModLocalization.Get(StringKey.PanelTitle);
        _panelTitleLabel.fontSize = NavPanelLayout.TitleFontSize * layout.Scale;
        _panelTitleLabel.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
        _panelTitleLabel.color = GameUiStyle.TitleColor;
        _panelTitleLabel.alignment = TextAlignmentOptions.Left;
        _panelTitleLabel.raycastTarget = false;
        GameUiStyle.ApplyTitleFont(_panelTitleLabel);

        CreateActionButton(panel, "RouteButton", new Vector2(layout.LeftButtonX, layout.ButtonTopY), layout.HalfButtonWidth,
            layout.ButtonHeight, layout.Scale, (UnityEngine.Events.UnityAction)OnRouteToggleClicked,
            out _routeButtonImage, out _routeLabel);

        CreateActionButton(panel, "AutoWalkButton", new Vector2(layout.RightButtonX, layout.ButtonTopY), layout.HalfButtonWidth,
            layout.ButtonHeight, layout.Scale, (UnityEngine.Events.UnityAction)OnAutoWalkToggleClicked,
            out _autoWalkButtonImage, out _autoWalkLabel);

        _forceApply = true;
        RefreshVisual();
    }

    private static void CreateActionButton(
        RectTransform panel,
        string name,
        Vector2 topAnchoredPos,
        float width,
        float height,
        float scale,
        UnityEngine.Events.UnityAction onClick,
        out Image buttonImage,
        out TextMeshProUGUI label)
    {
        var rect = CreateRect(panel, name);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = topAnchoredPos;
        rect.sizeDelta = new Vector2(width, height);

        buttonImage = rect.gameObject.AddComponent<Image>();
        GameUiStyle.ApplyButtonBlue(buttonImage);

        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = buttonImage;
        button.onClick.AddListener(onClick);

        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.92f, 0.92f, 0.92f, 1f);
        colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        var labelGo = CreateRect(rect, "Label");
        labelGo.anchorMin = Vector2.zero;
        labelGo.anchorMax = Vector2.one;
        labelGo.offsetMin = new Vector2(NavPanelLayout.ButtonTextPaddingX * scale, 0f);
        labelGo.offsetMax = new Vector2(-NavPanelLayout.ButtonTextPaddingX * scale,
            -NavPanelLayout.ButtonLabelBottomInset * scale);
        label = labelGo.gameObject.AddComponent<TextMeshProUGUI>();
        label.fontSize = NavPanelLayout.ButtonFontSize * scale;
        label.fontStyle = FontStyles.UpperCase;
        label.alignment = TextAlignmentOptions.Center;
        label.color = ButtonLabelColor;
        label.raycastTarget = false;
        GameUiStyle.ApplyButtonFont(label);
    }

    public static void UpdateVisibility()
    {
        GameUiStyle.EnsureInitialized();

        if (GameUiStyle.ShouldRebuildHud)
        {
            Destroy();
            GameUiStyle.MarkRebuildHandled();
        }

        EnsureCreated();
        if (_root == null)
            return;

        var offsetY = ModConfig.NavHudOffsetY.Value;
        if (_panelRect != null && (_forceApply || !Mathf.Approximately(offsetY, _lastOffsetY)))
        {
            _lastOffsetY = offsetY;
            _panelRect.anchoredPosition = NavPanelLayout.GetScreenPosition(offsetY);
        }

        var active = GameState.ShouldShowNavigationPanel() && MovementModeDetector.ShouldShowHudButton();
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

        var routeOn = ModConfig.RouteLineEnabled.Value;
        var walkOn = ModConfig.AutoWalkEnabled.Value;
        var onFoot = MovementModeDetector.CurrentMode == MovementMode.OnFoot;
        if (_forceApply || routeOn != _lastRouteOn || walkOn != _lastWalkOn || onFoot != _lastOnFoot)
        {
            _lastRouteOn = routeOn;
            _lastWalkOn = walkOn;
            _lastOnFoot = onFoot;
            RefreshVisual();
        }

        _forceApply = false;
    }

    public static void RefreshLocalizedText()
    {
        if (_panelTitleLabel != null)
            _panelTitleLabel.text = ModLocalization.Get(StringKey.PanelTitle);
        _forceApply = true;
        RefreshVisual();
    }

    public static void RefreshVisual()
    {
        if (_routeButtonImage == null || _routeLabel == null || _autoWalkButtonImage == null || _autoWalkLabel == null)
            return;

        var routeOn = ModConfig.RouteLineEnabled.Value;
        if (routeOn)
            GameUiStyle.ApplyButtonBlue(_routeButtonImage);
        else
            GameUiStyle.ApplyButtonGrey(_routeButtonImage);
        _routeLabel.text = routeOn
            ? ModLocalization.Get(StringKey.RouteOn)
            : ModLocalization.Get(StringKey.RouteOff);
        _routeLabel.color = ButtonLabelColor;

        var walkOn = ModConfig.AutoWalkEnabled.Value;
        var onFoot = MovementModeDetector.CurrentMode == MovementMode.OnFoot;
        if (!onFoot)
        {
            GameUiStyle.ApplyButtonGrey(_autoWalkButtonImage);
            _autoWalkLabel.text = ModLocalization.Get(StringKey.AutoWalk);
            _autoWalkLabel.color = LabelDisabled;
        }
        else
        {
            if (walkOn)
                GameUiStyle.ApplyButtonGreen(_autoWalkButtonImage);
            else
                GameUiStyle.ApplyButtonGrey(_autoWalkButtonImage);
            _autoWalkLabel.text = walkOn
                ? ModLocalization.Get(StringKey.WalkOn)
                : ModLocalization.Get(StringKey.AutoWalk);
            _autoWalkLabel.color = ButtonLabelColor;
        }
    }

    public static void Destroy()
    {
        if (_root != null)
        {
            Object.Destroy(_root);
            _root = null;
            _panelRect = null;
            _forceApply = true;
            ClearButtonRefs();
        }
    }

    private static void DestroyLegacyRoots()
    {
        foreach (var name in new[]
                 {
                     "OnMapGps_HudRoot", "OnMapGps_HudRoot_v2", "OnMapGps_HudRoot_v3",
                     "OnMapGps_HudRoot_v4", "OnMapGps_HudRoot_v5", "OnMapGps_HudRoot_v6",
                     "OnMapGps_HudRoot_v7", "OnMapGps_HudRoot_v8", "OnMapGps_HudRoot_v9",
                     "OnMapGps_HudRoot_v10", "OnMapGps_HudRoot_v11", "OnMapGps_HudRoot_v12",
                     "OnMapGps_HudRoot_v13", "OnMapGps_HudRoot_v14", "OnMapGps_HudRoot_v15",
                     "OnMapGps_HudRoot_v16", "OnMapGps_HudRoot_v17", "OnMapGps_HudRoot_v18",
                     "OnMapGps_HudRoot_v19", "OnMapGps_HudRoot_v20", "OnMapGps_HudRoot_v21",
                     "OnMapGps_HudRoot_v22", "OnMapGps_HudRoot_v23", "OnMapGps_HudRoot_v24",
                     "OnMapGps_HudRoot_v25", "OnMapGps_HudRoot_v26", "OnMapGps_HudRoot_v27",
                     "OnMapGps_HudRoot_v28", "OnMapGps_HudRoot_v29", "OnMapGps_HudRoot_v30",
                     "OnMapGps_HudRoot_v31", "OnMapGps_HudRoot_v32", "OnMapGps_HudRoot_v33",
                     "OnMapGps_HudRoot_v34", "OnMapGps_HudRoot_v35", "OnMapGps_HudRoot_v36",
                     "OnMapGps_HudRoot_v37", "OnMapGps_HudRoot_v38", "OnMapGps_HudRoot_v39",
                     "OnMapGps_HudRoot_v40", "OnMapGps_HudRoot_v41", "OnMapGps_HudRoot_v42",
                     "OnMapGps_HudRoot_v43", "OnMapGps_HudRoot_v44", "OnMapGps_HudRoot_v45",
                     "OnMapGps_HudRoot_v46", "OnMapGps_HudRoot_v47", "OnMapGps_HudRoot_v48",
                     "OnMapGps_HudRoot_v49", "OnMapGps_HudRoot_v50", "OnMapGps_HudRoot_v51",
                     "VoogleRoute_HudRoot", "VoogleRoute_HudRoot_v2", "VoogleRoute_HudRoot_v3",
                     "VoogleRoute_HudRoot_v4", "VoogleRoute_HudRoot_v5", "VoogleRoute_HudRoot_v6",
                     "VoogleRoute_HudRoot_v7", "VoogleRoute_HudRoot_v8", "VoogleRoute_HudRoot_v9",
                     "VoogleRoute_HudRoot_v10", "VoogleRoute_HudRoot_v11", "VoogleRoute_HudRoot_v12",
                     "VoogleRoute_HudRoot_v13", "VoogleRoute_HudRoot_v14", "VoogleRoute_HudRoot_v15",
                     "VoogleRoute_HudRoot_v16", "VoogleRoute_HudRoot_v17", "VoogleRoute_HudRoot_v18",
                     "VoogleRoute_HudRoot_v19", "VoogleRoute_HudRoot_v20", "VoogleRoute_HudRoot_v21",
                    "VoogleRoute_HudRoot_v22", "VoogleRoute_HudRoot_v23", "VoogleRoute_HudRoot_v24",
                    "VoogleRoute_HudRoot_v25", "VoogleRoute_HudRoot_v26", "VoogleRoute_HudRoot_v27",
                    "VoogleRoute_HudRoot_v28", "VoogleRoute_HudRoot_v29", "VoogleRoute_HudRoot_v30",
                    "VoogleRoute_HudRoot_v31", "VoogleRoute_HudRoot_v32", "VoogleRoute_HudRoot_v33",
                    "VoogleRoute_HudRoot_v34", "VoogleRoute_HudRoot_v35", "VoogleRoute_HudRoot_v36",
                    "VoogleRoute_HudRoot_v37", "VoogleRoute_HudRoot_v38", "VoogleRoute_HudRoot_v39",
                    "VoogleRoute_HudRoot_v40", "VoogleRoute_HudRoot_v41", "VoogleRoute_HudRoot_v42",
                    "VoogleRoute_HudRoot_v43", "VoogleRoute_HudRoot_v44",
                    "VoogleRoute_HudRoot_v45", "VoogleRoute_HudRoot_v46", "VoogleRoute_HudRoot_v47",
                    "VoogleRoute_HudRoot_v48", "VoogleRoute_HudRoot_v49", "VoogleRoute_HudRoot_v50"
                  })
        {
            var legacy = GameObject.Find(name);
            if (legacy != null)
                Object.Destroy(legacy);
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

    private static RectTransform CreateRect(Transform parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.AddComponent<RectTransform>();
    }

    private static void OnRouteToggleClicked()
    {
        ModConfig.RouteLineEnabled.Value = !ModConfig.RouteLineEnabled.Value;
        ModConfig.Category.SaveToFile(false);
        if (!ModConfig.RouteLineEnabled.Value)
            RouteLineRenderer.Hide();
        if (!ModConfig.WantsRouteComputation)
            PathFinderService.InvalidateCache();
        RefreshVisual();
    }

    private static void OnAutoWalkToggleClicked()
    {
        ModConfig.AutoWalkEnabled.Value = !ModConfig.AutoWalkEnabled.Value;
        ModConfig.Category.SaveToFile(false);
        if (!ModConfig.AutoWalkEnabled.Value)
            AutoWalkService.Reset();
        RefreshVisual();
    }
}
