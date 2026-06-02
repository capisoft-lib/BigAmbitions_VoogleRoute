using UnityEngine;
using UnityEngine.UI;

namespace VoogleRoute.UI;

/// <summary>
/// Cadre header + corps validé (même recette que le panneau VOOGLE ROUTE).
/// Toute fenêtre mod doit passer par ici pour éviter les dérives de layout.
/// </summary>
internal static class GameStylePanelChrome
{
    internal readonly struct BuiltChrome
    {
        public RectTransform Panel { get; init; }
        public RectTransform Background { get; init; }
        public RectTransform Header { get; init; }
        public NavPanelLayout.Metrics Metrics { get; init; }
        public float Scale { get; init; }
        public float ContentInset { get; init; }
    }

    internal static void SetupOverlayCanvas(GameObject root, int sortingOrder)
    {
        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        var scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        scaler.scaleFactor = 1f;

        root.AddComponent<GraphicRaycaster>();
    }

    /// <summary>
    /// Crée Panel + Background (corps) + Header avec le gabarit NavPanelLayout.
    /// </summary>
    /// <param name="headerExtraTrim">
    /// Largeur de trim additionnelle (px ref panel 370). Settings : 2 px / côté pour flush sur le lip du corps.
    /// </param>
    internal static BuiltChrome Build(
        Transform parent,
        float panelWidth,
        float panelHeight,
        string panelName,
        float headerExtraTrim = 0f)
    {
        GameUiStyle.EnsureInitialized();

        var scale = panelWidth / NavPanelLayout.PanelWidth;
        var metrics = NavPanelLayout.CreateMetrics(scale);

        var panel = CreateRect(parent, panelName);
        panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.pivot = new Vector2(0.5f, 0.5f);
        panel.sizeDelta = new Vector2(panelWidth, panelHeight);

        var background = CreateRect(panel, "Background");
        NavPanelLayout.ApplyBodyFrame(background, scale);
        var backgroundImg = background.gameObject.AddComponent<Image>();
        backgroundImg.raycastTarget = true;
        GameUiStyle.ApplyPanelBg(backgroundImg);

        var header = CreateRect(panel, "Header");
        ApplyHeaderFrame(header, metrics, headerExtraTrim);
        var headerImg = header.gameObject.AddComponent<Image>();
        headerImg.raycastTarget = false;
        GameUiStyle.ApplyHeaderBg(headerImg);

        return new BuiltChrome
        {
            Panel = panel,
            Background = background,
            Header = header,
            Metrics = metrics,
            Scale = scale,
            ContentInset = metrics.ContentInset,
        };
    }

    internal static void ApplyHeaderFrameAligned(RectTransform header, in NavPanelLayout.Metrics metrics) =>
        ApplyHeaderFrame(header, metrics);

    internal static void ApplyHeaderFrame(
        RectTransform header,
        in NavPanelLayout.Metrics metrics,
        float headerExtraTrim = 0f)
    {
        header.anchorMin = new Vector2(0f, 1f);
        header.anchorMax = new Vector2(1f, 1f);
        header.pivot = new Vector2(0.5f, 1f);

        NavPanelLayout.ComputeHeaderRectHudTrim(
            metrics.PanelWidth,
            metrics.Scale,
            headerExtraTrim,
            out var sizeDeltaX,
            out var posX);
        header.anchoredPosition = new Vector2(posX, 0f);
        header.sizeDelta = new Vector2(sizeDeltaX, metrics.HeaderHeight);
    }

    private static RectTransform CreateRect(Transform parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.AddComponent<RectTransform>();
    }
}
