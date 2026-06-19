using System;
using System.Globalization;
using System.Text;

namespace VoogleRoute
{
    /// <summary>
    /// Manual JSON codec for per-save mod options. Avoids System.Text.Json, which fails on IL2CPP
    /// (Utf8JsonWriter VTable setup error).
    /// </summary>
    internal static class ModOptionsJsonCodec
    {
        internal static string Serialize(ModOptionsSaveData data)
        {
            ModOptionsSaveData.EnsureDefaults(data);
            var inv = CultureInfo.InvariantCulture;
            var sb = new StringBuilder();
            sb.Append('{');
            AppendBool(sb, "displayOutside", data.DisplayOutside);
            AppendBool(sb, "displayInside", data.DisplayInside);
            AppendBool(sb, "routeLine", data.RouteLine);
            AppendBool(sb, "autoWalk", data.AutoWalk);
            AppendBool(sb, "indoorRoute", data.IndoorRoute);
            AppendBool(sb, "indoorAutowalk", data.IndoorAutowalk);
            AppendBool(sb, "useSubway", data.UseSubway);
            AppendInt(sb, "baseTaxiMultiplier", data.BaseTaxiMultiplier);
            AppendBool(sb, "forceCorrectSideArrival", data.ForceCorrectSideArrival);
            AppendBool(sb, "allowUturnAtStart", data.AllowUturnAtStart);
            AppendBool(sb, "autoEnterDestination", data.AutoEnterDestination);
            AppendColorArray(sb, "footRouteLineColor", data.FootRouteLineColor, inv);
            AppendColorArray(sb, "vehicleRouteLineColor", data.VehicleRouteLineColor, inv);
            AppendColorArray(sb, "indoorRouteLineColor", data.IndoorRouteLineColor, inv, trailingComma: false);
            sb.Append('}');
            return sb.ToString();
        }

        internal static ModOptionsSaveData Deserialize(string json)
        {
            var data = new ModOptionsSaveData
            {
                DisplayOutside = ReadBool(json, "displayOutside", true),
                DisplayInside = ReadBool(json, "displayInside", true),
                RouteLine = ReadBool(json, "routeLine", true),
                AutoWalk = ReadBool(json, "autoWalk", false),
                IndoorRoute = ReadBool(json, "indoorRoute", true),
                IndoorAutowalk = ReadBool(json, "indoorAutowalk", false),
                UseSubway = ReadBool(json, "useSubway", true),
                BaseTaxiMultiplier = ReadInt(json, "baseTaxiMultiplier", 2),
                ForceCorrectSideArrival = ReadBool(json, "forceCorrectSideArrival", false),
                AllowUturnAtStart = ReadBool(json, "allowUturnAtStart", false),
                AutoEnterDestination = ReadBool(json, "autoEnterDestination", true),
                FootRouteLineColor = ReadColorArray(json, "footRouteLineColor"),
                VehicleRouteLineColor = ReadColorArray(json, "vehicleRouteLineColor"),
                IndoorRouteLineColor = ReadColorArray(json, "indoorRouteLineColor")
            };

            ModOptionsSaveData.EnsureDefaults(data);
            return data;
        }

        private static void AppendBool(StringBuilder sb, string key, bool value)
        {
            sb.Append('"').Append(key).Append("\":").Append(value ? "true" : "false").Append(',');
        }

        private static void AppendInt(StringBuilder sb, string key, int value)
        {
            sb.Append('"').Append(key).Append("\":").Append(value).Append(',');
        }

        private static void AppendColorArray(
            StringBuilder sb,
            string key,
            float[] color,
            CultureInfo inv,
            bool trailingComma = true)
        {
            sb.Append('"').Append(key).Append("\":[");
            if (color != null)
            {
                for (var i = 0; i < color.Length; i++)
                {
                    if (i > 0)
                        sb.Append(',');
                    sb.Append(color[i].ToString(inv));
                }
            }

            sb.Append(']');
            if (trailingComma)
                sb.Append(',');
        }

        private static bool ReadBool(string json, string key, bool defaultValue)
        {
            if (ModLog.TryReadBool(json, key, out var value))
                return value;

            return defaultValue;
        }

        private static int ReadInt(string json, string key, int defaultValue)
        {
            var token = "\"" + key + "\"";
            var idx = json.IndexOf(token, StringComparison.OrdinalIgnoreCase);
            if (idx < 0)
                return defaultValue;

            var colon = json.IndexOf(':', idx);
            if (colon < 0)
                return defaultValue;

            var end = json.IndexOfAny(new[] { ',', '}', '\n', '\r' }, colon + 1);
            if (end < 0)
                end = json.Length;

            var raw = json.Substring(colon + 1, end - colon - 1).Trim();
            return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
                ? value
                : defaultValue;
        }

        private static float[] ReadColorArray(string json, string key)
        {
            var token = "\"" + key + "\"";
            var idx = json.IndexOf(token, StringComparison.OrdinalIgnoreCase);
            if (idx < 0)
                return null;

            var bracket = json.IndexOf('[', idx);
            if (bracket < 0)
                return null;

            var close = json.IndexOf(']', bracket);
            if (close < 0)
                return null;

            var inner = json.Substring(bracket + 1, close - bracket - 1);
            if (string.IsNullOrWhiteSpace(inner))
                return null;

            var parts = inner.Split(',');
            if (parts.Length < 4)
                return null;

            var color = new float[4];
            for (var i = 0; i < 4; i++)
            {
                if (!float.TryParse(parts[i].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out color[i]))
                    return null;
            }

            return color;
        }
    }
}
