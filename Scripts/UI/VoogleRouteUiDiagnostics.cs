using System.Text;
using Capisoft.Lib.BaUnifiedUI.Layout;
using UnityEngine;

namespace VoogleRoute.UI
{
    /// <summary>Always-on Unity console diagnostics (Player.log) for UI chrome debugging.</summary>
    internal static class VoogleRouteUiDiagnostics
    {
        internal const string UiBuildTag = "2026-06-14c";

        internal static void LogSessionStart(string modRoot)
        {
            Debug.Log(
                "[VoogleRoute][UI] session start | build=" + UiBuildTag +
                " | mod_root=" + modRoot);
        }

        internal static void LogStaleCheck(GameObject root, string expectedRootName, string reason)
        {
            if (string.IsNullOrEmpty(reason) || reason == "current")
                return;

            Debug.Log(
                "[VoogleRoute][UI] stale root | have=" + (root != null ? root.name : "null") +
                " | want=" + expectedRootName +
                " | reason=" + reason);
        }

        internal static void LogOrphanRoots(string prefix)
        {
            var count = 0;
            var names = new StringBuilder();
            var roots = Resources.FindObjectsOfTypeAll<GameObject>();
            for (var i = 0; i < roots.Length; i++)
            {
                var go = roots[i];
                if (go == null || go.transform.parent != null)
                    continue;
                if (!go.name.StartsWith(prefix))
                    continue;

                count++;
                if (names.Length > 0)
                    names.Append(',');
                names.Append(go.name);
            }

            Debug.Log("[VoogleRoute][UI] orphan scan | prefix=" + prefix + " | count=" + count + " | names=" + names);
        }

        internal static void LogPanelChrome(string tag, RectTransform panel, float panelWidth, float headerExtraTrim = 0f)
        {
            if (panel == null)
            {
                Debug.Log("[VoogleRoute][UI] " + tag + " | panel=null");
                return;
            }

            Canvas.ForceUpdateCanvases();

            var header = panel.Find("Header") as RectTransform;
            var background = panel.Find("Background") as RectTransform;
            var scale = panelWidth / BaUiLayout.PanelWidth;
            BaUiLayout.ComputeHeaderRectHudTrim(panelWidth, scale, headerExtraTrim, out var trimX, out var trimPosX);
            var expectedHeaderWidth = panel.rect.width + trimX;

            Debug.Log(
                "[VoogleRoute][UI] " + tag +
                " | panel=" + FormatRect(panel) +
                " | bg=" + FormatRect(background) +
                " | header=" + FormatRect(header) +
                " | headerParent=" + (header != null ? header.parent?.name : "null") +
                " | panelWidthArg=" + panelWidth.ToString("F1") +
                " | trimX=" + trimX.ToString("F2") +
                " | trimPosX=" + trimPosX.ToString("F2") +
                " | expectedHeaderW=" + expectedHeaderWidth.ToString("F1") +
                " | headerExtraTrim=" + headerExtraTrim.ToString("F2"));
        }

        private static string FormatRect(RectTransform rt)
        {
            if (rt == null)
                return "null";

            var r = rt.rect;
            return string.Format(
                "{0} w={1:F1} h={2:F1} anchor=({3:F2},{4:F2})-({5:F2},{6:F2}) pivot=({7:F2},{8:F2}) pos=({9:F1},{10:F1}) sizeDelta=({11:F1},{12:F1}) offsetMin=({13:F1},{14:F1}) offsetMax=({15:F1},{16:F1})",
                rt.name,
                r.width,
                r.height,
                rt.anchorMin.x,
                rt.anchorMin.y,
                rt.anchorMax.x,
                rt.anchorMax.y,
                rt.pivot.x,
                rt.pivot.y,
                rt.anchoredPosition.x,
                rt.anchoredPosition.y,
                rt.sizeDelta.x,
                rt.sizeDelta.y,
                rt.offsetMin.x,
                rt.offsetMin.y,
                rt.offsetMax.x,
                rt.offsetMax.y);
        }
    }
}
