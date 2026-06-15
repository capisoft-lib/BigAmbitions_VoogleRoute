using Helpers;
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
            ApplyUiLayer(root);
        }

        /// <summary>Match vanilla UI layer so GameManager.HasInputSelected blocks hotkeys while typing.</summary>
        internal static void ApplyUiLayer(GameObject root)
        {
            if (root == null)
                return;

            SetLayerRecursive(root, LayerHelper.UiLayerIndex);
        }

        private static void SetLayerRecursive(GameObject go, int layer)
        {
            go.layer = layer;
            var transform = go.transform;
            for (var i = 0; i < transform.childCount; i++)
                SetLayerRecursive(transform.GetChild(i).gameObject, layer);
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

        /// <summary>Centered Voogle modal popup — right edge fixed, left-only flush extension.</summary>
        internal static void ApplyModalHeaderFrame(RectTransform header, float scale)
        {
            var leftExtend = NavPanelLayout.SettingsHeaderLeftFlush * scale;

            header.anchorMin = new Vector2(0f, 1f);
            header.anchorMax = new Vector2(1f, 1f);
            header.pivot = new Vector2(0.5f, 1f);
            header.anchoredPosition = Vector2.zero;
            header.sizeDelta = Vector2.zero;
            header.offsetMin = new Vector2(-leftExtend, -NavPanelLayout.HeaderHeight);
            header.offsetMax = Vector2.zero;
        }

        internal static void ApplyHudTrimHeader(
            RectTransform header,
            float panelWidth,
            float headerExtraTrim = 0f)
        {
            var scale = panelWidth / NavPanelLayout.PanelWidth;
            ApplyHeaderFrame(header, NavPanelLayout.CreateMetrics(scale), headerExtraTrim);
        }

        /// <summary>Reapply body bleed + default hud-trim header together (RouteToggleHud recipe).</summary>
        internal static void RestorePanelChrome(RectTransform panel, float panelWidth, float headerExtraTrim = 0f)
        {
            var scale = panelWidth / NavPanelLayout.PanelWidth;
            var background = panel.Find("Background") as RectTransform;
            if (background != null)
                NavPanelLayout.ApplyBodyFrame(background, scale);

            var header = panel.Find("Header") as RectTransform;
            if (header == null)
                return;

            if (header.parent != panel)
                header.SetParent(panel, false);

            ApplyHudTrimHeader(header, panelWidth, headerExtraTrim);
        }

        /// <summary>
        /// Header aligned to NavPanelLayout visible body frame edges (ref panel 370).
        /// Uses BodyVisibleLeft/Right — the calibrated reference, not bleed or toggle guesses.
        /// </summary>
        internal static void ApplyVisibleFrameHeader(RectTransform header, float scale)
        {
            if (header.parent is RectTransform parent && parent.name == "Background")
                header.SetParent(parent.parent, false);

            header.anchorMin = new Vector2(0f, 1f);
            header.anchorMax = new Vector2(1f, 1f);
            header.pivot = new Vector2(0.5f, 1f);
            header.anchoredPosition = Vector2.zero;
            header.sizeDelta = Vector2.zero;

            var leftInset = NavPanelLayout.BodyVisibleLeft * scale;
            var rightExtend = (NavPanelLayout.BodyVisibleRight - NavPanelLayout.PanelWidth) * scale;
            header.offsetMin = new Vector2(leftInset, -NavPanelLayout.HeaderHeight);
            header.offsetMax = new Vector2(rightExtend, 0f);
        }

        /// <summary>
        /// Parents the header on the bled body frame and insets it to the sprite corners.
        /// Guarantees horizontal alignment between header bar and panel frame on tall docked panels.
        /// </summary>
        internal static void ApplyHeaderOnBodyFrame(RectTransform header, RectTransform background, float scale)
        {
            header.SetParent(background, false);
            header.anchorMin = new Vector2(0f, 1f);
            header.anchorMax = new Vector2(1f, 1f);
            header.pivot = new Vector2(0.5f, 1f);
            header.anchoredPosition = Vector2.zero;
            header.sizeDelta = Vector2.zero;
            var leftInset = NavPanelLayout.MainPanelHeaderTightenLeft * scale;
            var rightInset = NavPanelLayout.MainPanelHeaderTightenRight * scale;
            header.offsetMin = new Vector2(leftInset, -NavPanelLayout.HeaderHeight);
            header.offsetMax = new Vector2(-rightInset, 0f);
        }

        /// <summary>Toggle / docked HUD — header inset to visible frame borders (same as RouteToggleHud).</summary>
        internal static void ApplyToggleHudHeaderFrame(RectTransform header, float scale)
        {
            var leftInset = NavPanelLayout.HeaderSliceBorderLeft * scale + NavPanelLayout.ToggleHudHeaderLeftAdjust;
            var rightInset = NavPanelLayout.HeaderSliceBorderRight * scale + NavPanelLayout.ToggleHudHeaderRightAdjust;

            header.anchorMin = new Vector2(0f, 1f);
            header.anchorMax = new Vector2(1f, 1f);
            header.pivot = new Vector2(0.5f, 1f);
            header.anchoredPosition = Vector2.zero;
            header.sizeDelta = Vector2.zero;
            header.offsetMin = new Vector2(leftInset, -NavPanelLayout.HeaderHeight);
            header.offsetMax = new Vector2(-rightInset, 0f);
        }

        /// <summary>Main panel or wide HUD — header edges align with visible body frame (ref-pixel constants).</summary>
        internal static void ApplyMainPanelHeaderFrame(RectTransform header)
        {
            var leftExtend = NavPanelLayout.FrameBleedWidth * 0.5f - NavPanelLayout.FrameOffsetX -
                             NavPanelLayout.MainPanelHeaderTightenLeft;
            var rightExtend = NavPanelLayout.FrameBleedWidth * 0.5f + NavPanelLayout.FrameOffsetX -
                              NavPanelLayout.MainPanelHeaderTightenRight;

            header.anchorMin = new Vector2(0f, 1f);
            header.anchorMax = new Vector2(1f, 1f);
            header.pivot = new Vector2(0.5f, 1f);
            header.anchoredPosition = Vector2.zero;
            header.sizeDelta = Vector2.zero;
            header.offsetMin = new Vector2(-leftExtend, -NavPanelLayout.HeaderHeight);
            header.offsetMax = new Vector2(rightExtend, 0f);
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
