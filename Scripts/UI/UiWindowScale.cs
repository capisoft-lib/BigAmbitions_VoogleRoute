using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace VoogleRoute.UI
{
    /// <summary>Scales only Voogle Route windows, including their text and controls.</summary>
    internal static class UiWindowScale
    {
        private const float ScreenMargin = 16f;
        private const float MapPairWidth = 2f * 420f + 8f + 2f * ScreenMargin;

        private sealed class Window
        {
            internal GameObject Root;
            internal RectTransform Panel;
            internal bool SharesMapRow;
        }

        private static readonly List<Window> Windows = new List<Window>();
        private static int _screenWidth;
        private static int _screenHeight;

        internal static void Register(GameObject root, RectTransform panel, bool sharesMapRow = false)
        {
            if (root == null || panel == null)
                return;

            for (var i = Windows.Count - 1; i >= 0; i--)
            {
                if (Windows[i].Root == null || Windows[i].Root == root)
                    Windows.RemoveAt(i);
            }

            var window = new Window { Root = root, Panel = panel, SharesMapRow = sharesMapRow };
            Windows.Add(window);
            Apply(window);
        }

        internal static void Refresh()
        {
            _screenWidth = Screen.width;
            _screenHeight = Screen.height;
            for (var i = Windows.Count - 1; i >= 0; i--)
            {
                if (Windows[i].Root == null || Windows[i].Panel == null)
                    Windows.RemoveAt(i);
                else
                    Apply(Windows[i]);
            }
        }

        internal static void Tick()
        {
            if (_screenWidth != Screen.width || _screenHeight != Screen.height)
                Refresh();
        }

        internal static void Clear()
        {
            Windows.Clear();
            _screenWidth = 0;
            _screenHeight = 0;
        }

        private static void Apply(Window window)
        {
            var scaler = window.Root.GetComponent<CanvasScaler>();
            var canvasRect = window.Root.GetComponent<RectTransform>();
            if (scaler == null || canvasRect == null)
                return;

            var width = window.SharesMapRow ? MapPairWidth : window.Panel.rect.width + 2f * ScreenMargin;
            var height = window.Panel.rect.height + 2f * ScreenMargin;
            var requested = ModConfig.WindowScalePercent / 100f;
            var factor = Mathf.Min(requested,
                Screen.width / Mathf.Max(1f, width),
                Screen.height / Mathf.Max(1f, height));
            scaler.scaleFactor = Mathf.Max(0.1f, factor);

            // A saved drag position may have been recorded at a different resolution or scale.
            Canvas.ForceUpdateCanvases();
            var corners = new Vector3[4];
            window.Panel.GetWorldCorners(corners);
            var lower = canvasRect.InverseTransformPoint(corners[0]);
            var upper = canvasRect.InverseTransformPoint(corners[2]);
            var bounds = canvasRect.rect;
            var dx = lower.x < bounds.xMin ? bounds.xMin - lower.x :
                upper.x > bounds.xMax ? bounds.xMax - upper.x : 0f;
            var dy = lower.y < bounds.yMin ? bounds.yMin - lower.y :
                upper.y > bounds.yMax ? bounds.yMax - upper.y : 0f;
            if (dx != 0f || dy != 0f)
                window.Panel.anchoredPosition += new Vector2(dx, dy);
        }
    }
}
