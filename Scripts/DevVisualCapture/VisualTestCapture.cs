using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace VoogleRoute
{
    internal static class VisualTestCapture
    {
        internal static IEnumerator CaptureAfterFrames(
            VisualTestScreenBounds bounds,
            bool fullScreen,
            Action<Texture2D> onComplete)
        {
            yield return null;
            Canvas.ForceUpdateCanvases();
            yield return new WaitForEndOfFrame();

            Texture2D texture = null;
            try
            {
                texture = fullScreen || !bounds.IsValid
                    ? CaptureFullScreen()
                    : CaptureScreenBounds(bounds);
                onComplete?.Invoke(texture);
            }
            catch (Exception ex)
            {
                if (texture != null)
                    UnityEngine.Object.Destroy(texture);
                onComplete?.Invoke(null);
                ModLog.Info("[VisualTest] Capture failed: " + ex.Message);
            }
        }

        internal static void SavePng(Texture2D texture, string outputPath)
        {
            if (texture == null)
                throw new ArgumentNullException(nameof(texture));

            var directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            var bytes = ImageConversion.EncodeToPNG(texture);
            File.WriteAllBytes(outputPath, bytes);
        }

        private static Texture2D CaptureFullScreen()
        {
            var source = ScreenCapture.CaptureScreenshotAsTexture();
            try
            {
                return CopyTexture(source);
            }
            finally
            {
                UnityEngine.Object.Destroy(source);
            }
        }

        private static Texture2D CaptureScreenBounds(VisualTestScreenBounds bounds)
        {
            var source = ScreenCapture.CaptureScreenshotAsTexture();
            try
            {
                return CropTexture(source, bounds.X, bounds.Y, bounds.Width, bounds.Height);
            }
            finally
            {
                UnityEngine.Object.Destroy(source);
            }
        }

        internal static bool TryGetScreenBounds(
            RectTransform panelRect,
            int marginPixels,
            out int x,
            out int y,
            out int width,
            out int height)
        {
            x = y = width = height = 0;
            if (panelRect == null || !panelRect.gameObject.activeInHierarchy)
                return false;

            var corners = new Vector3[4];
            panelRect.GetWorldCorners(corners);

            var canvas = panelRect.GetComponentInParent<Canvas>();
            Camera camera = null;
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                camera = canvas.worldCamera;

            var min = new Vector2(float.MaxValue, float.MaxValue);
            var max = new Vector2(float.MinValue, float.MinValue);
            for (var i = 0; i < corners.Length; i++)
            {
                var screen = RectTransformUtility.WorldToScreenPoint(camera, corners[i]);
                min = Vector2.Min(min, screen);
                max = Vector2.Max(max, screen);
            }

            var left = Mathf.FloorToInt(min.x) - marginPixels;
            var bottom = Mathf.FloorToInt(min.y) - marginPixels;
            var right = Mathf.CeilToInt(max.x) + marginPixels;
            var top = Mathf.CeilToInt(max.y) + marginPixels;

            left = Mathf.Clamp(left, 0, Screen.width - 1);
            bottom = Mathf.Clamp(bottom, 0, Screen.height - 1);
            right = Mathf.Clamp(right, left + 1, Screen.width);
            top = Mathf.Clamp(top, bottom + 1, Screen.height);

            width = right - left;
            height = top - bottom;
            x = left;
            y = bottom;
            return width > 0 && height > 0;
        }

        private static Texture2D CropTexture(Texture2D source, int x, int bottomY, int width, int height)
        {
            var topLeftY = Screen.height - bottomY - height;
            var pixels = source.GetPixels(x, topLeftY, width, height);
            var cropped = new Texture2D(width, height, TextureFormat.RGBA32, false);
            cropped.SetPixels(pixels);
            cropped.Apply();
            return cropped;
        }

        private static Texture2D CopyTexture(Texture2D source)
        {
            var copy = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
            copy.SetPixels(source.GetPixels());
            copy.Apply();
            return copy;
        }
    }
}
