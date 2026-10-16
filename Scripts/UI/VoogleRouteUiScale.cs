using System.Collections.Generic;
using UnityEngine;

namespace VoogleRoute.UI
{
    /// <summary>Applies the configured scale to registered VoogleRoute panels without rebuilding them.</summary>
    internal static class VoogleRouteUiScale
    {
        private const float MinScaleFactor = 0.5f;
        private const float MaxScaleFactor = 1.5f;

        private readonly struct ScaledRect
        {
            internal readonly RectTransform Rect;
            internal readonly Vector3 BaseScale;

            internal ScaledRect(RectTransform rect, Vector3 baseScale)
            {
                Rect = rect;
                BaseScale = baseScale;
            }
        }

        private static readonly List<ScaledRect> Rects = new List<ScaledRect>();
        private static float _scaleFactor = 1f;

        internal static void SetScaleFactor(float scaleFactor)
        {
            _scaleFactor = Mathf.Clamp(scaleFactor, MinScaleFactor, MaxScaleFactor);
            for (var i = Rects.Count - 1; i >= 0; i--)
            {
                var tracked = Rects[i];
                if (tracked.Rect == null)
                {
                    Rects.RemoveAt(i);
                    continue;
                }

                tracked.Rect.localScale = tracked.BaseScale * _scaleFactor;
            }
        }

        internal static void Register(RectTransform rect)
        {
            if (rect == null)
                return;

            for (var i = Rects.Count - 1; i >= 0; i--)
            {
                var tracked = Rects[i];
                if (tracked.Rect == null)
                {
                    Rects.RemoveAt(i);
                    continue;
                }

                if (tracked.Rect != rect)
                    continue;

                rect.localScale = tracked.BaseScale * _scaleFactor;
                return;
            }

            var entry = new ScaledRect(rect, rect.localScale);
            Rects.Add(entry);
            rect.localScale = entry.BaseScale * _scaleFactor;
        }
    }
}
