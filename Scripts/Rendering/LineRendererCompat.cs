using System;
using System.Reflection;
using UnityEngine;

namespace VoogleRoute.Rendering
{
    /// <summary>
    /// Optional LineRenderer APIs are stripped from some Big Ambitions player builds.
    /// Access them by name so a missing accessor cannot abort mod initialization.
    /// </summary>
    internal static class LineRendererCompat
    {
        private const BindingFlags InstancePublic = BindingFlags.Instance | BindingFlags.Public;

        internal static readonly bool SupportsRouteRendering =
            HasWritableProperty("useWorldSpace") &&
            HasWritableProperty("startWidth") &&
            HasWritableProperty("endWidth");

        internal static void ApplyCommonStyle(
            LineRenderer line,
            float width,
            int capVertices,
            int cornerVertices,
            LineTextureMode textureMode = LineTextureMode.Stretch)
        {
            TrySet(line, "useWorldSpace", true);
            TrySet(line, "alignment", LineAlignment.View);
            TrySet(line, "textureMode", textureMode);
            TrySet(line, "numCapVertices", capVertices);
            TrySet(line, "numCornerVertices", cornerVertices);
            TrySet(line, "loop", false);
            TrySet(line, "startWidth", width);
            TrySet(line, "endWidth", width);
        }

        internal static void SetTextureMode(LineRenderer line, LineTextureMode mode) =>
            TrySet(line, "textureMode", mode);

        internal static float GetStartWidth(LineRenderer line, float fallback) =>
            TryGet(line, "startWidth", out float value) ? value : fallback;

        internal static int GetPositionCount(LineRenderer line) =>
            TryGet(line, "positionCount", out int value) ? value : 0;

        internal static bool TryGetPosition(LineRenderer line, int index, out Vector3 position)
        {
            position = default;
            if (line == null)
                return false;

            try
            {
                var method = typeof(LineRenderer).GetMethod("GetPosition", InstancePublic);
                if (method == null)
                    return false;
                var value = method.Invoke(line, new object[] { index });
                if (value is Vector3 vector)
                {
                    position = vector;
                    return true;
                }
            }
            catch
            {
                // Optional API unavailable in this player build.
            }

            return false;
        }

        private static bool TrySet<T>(LineRenderer line, string propertyName, T value)
        {
            if (line == null)
                return false;

            try
            {
                var property = typeof(LineRenderer).GetProperty(propertyName, InstancePublic);
                if (property == null || !property.CanWrite)
                    return false;
                property.SetValue(line, value, null);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool TryGet<T>(LineRenderer line, string propertyName, out T value)
        {
            value = default;
            if (line == null)
                return false;

            try
            {
                var property = typeof(LineRenderer).GetProperty(propertyName, InstancePublic);
                if (property == null || !property.CanRead)
                    return false;
                var raw = property.GetValue(line, null);
                if (raw is T typed)
                {
                    value = typed;
                    return true;
                }
            }
            catch
            {
                // Optional API unavailable in this player build.
            }

            return false;
        }

        private static bool HasWritableProperty(string propertyName)
        {
            try
            {
                var property = typeof(LineRenderer).GetProperty(propertyName, InstancePublic);
                return property != null && property.CanWrite;
            }
            catch
            {
                return false;
            }
        }
    }
}
