using UnityEngine;
using UnityEngine.UI;

namespace VoogleRoute.UI
{
    internal static class GameStylePanelChrome
    {
        internal struct BuiltChrome
        {
            public RectTransform Panel;
            public RectTransform Background;
            public RectTransform Header;
            public NavPanelLayout.Metrics Metrics;
            public float Scale;
            public float ContentInset;
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

            var chrome = new BuiltChrome();
            chrome.Panel = panel;
            chrome.Background = background;
            chrome.Header = header;
            chrome.Metrics = metrics;
            chrome.Scale = scale;
            chrome.ContentInset = metrics.ContentInset;
            return chrome;
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

            if (IsSettingsFullWidthHeader(headerExtraTrim))
                ApplySettingsHeaderLeftFlush(header, metrics);
        }

        private static bool IsSettingsFullWidthHeader(float headerExtraTrim) =>
            Mathf.Abs(headerExtraTrim - NavPanelLayout.SettingsPanelHeaderWidenTrim) < 0.01f;

        /// <summary>Colle le bord gauche du header sans déplacer le bord droit (validé en jeu).</summary>
        private static void ApplySettingsHeaderLeftFlush(RectTransform header, in NavPanelLayout.Metrics metrics)
        {
            var extend = NavPanelLayout.SettingsHeaderLeftFlush * metrics.Scale;
            header.anchoredPosition = Vector2.zero;
            header.sizeDelta = Vector2.zero;
            header.offsetMin = new Vector2(-extend, -metrics.HeaderHeight);
            header.offsetMax = Vector2.zero;
        }

        private static RectTransform CreateRect(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.AddComponent<RectTransform>();
        }
    }
}
