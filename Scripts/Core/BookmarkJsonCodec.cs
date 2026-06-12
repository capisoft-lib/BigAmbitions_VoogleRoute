using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using VoogleRoute.Navigation;

namespace VoogleRoute
{
    internal static class BookmarkJsonCodec
    {
        internal static string Serialize(BookmarkFileData data)
        {
            var inv = CultureInfo.InvariantCulture;
            var sb = new StringBuilder();
            sb.Append("{\n");
            sb.Append("  \"bookmarks\": [\n");

            var bookmarks = data?.Bookmarks;
            if (bookmarks != null)
            {
                for (var i = 0; i < bookmarks.Count; i++)
                {
                    AppendBookmarkEntry(sb, "    ", bookmarks[i], inv);
                    if (i < bookmarks.Count - 1)
                        sb.Append(',');
                    sb.Append('\n');
                }
            }

            sb.Append("  ],\n");
            sb.Append("  \"quick_bookmarks\": {\n");
            AppendQuickBookmarkProperty(sb, "    ", "last_car", data?.QuickBookmarks?.LastCar, inv);
            sb.Append(",\n");
            AppendQuickBookmarkProperty(sb, "    ", "last_home", data?.QuickBookmarks?.LastHome, inv);
            sb.Append(",\n");
            AppendQuickBookmarkProperty(sb, "    ", "last_shop", data?.QuickBookmarks?.LastShop, inv);
            sb.Append('\n');
            sb.Append("  }\n");
            sb.Append('}');
            return sb.ToString();
        }

        internal static BookmarkFileData Deserialize(string json)
        {
            var data = new BookmarkFileData
            {
                Bookmarks = ParseBookmarkArray(json, "bookmarks"),
                QuickBookmarks = new QuickBookmarksConfig
                {
                    LastCar = ParseQuickBookmark(json, "last_car"),
                    LastHome = ParseQuickBookmark(json, "last_home"),
                    LastShop = ParseQuickBookmark(json, "last_shop")
                }
            };

            return data;
        }

        private static List<BookmarkConfigEntry> ParseBookmarkArray(string json, string arrayKey)
        {
            var list = new List<BookmarkConfigEntry>();
            var arrayStart = FindArrayStart(json, arrayKey);
            if (arrayStart < 0)
                return list;

            var idx = arrayStart;
            while (true)
            {
                var objectStart = json.IndexOf('{', idx);
                if (objectStart < 0)
                    break;

                var objectEnd = FindMatchingBrace(json, objectStart);
                if (objectEnd < 0)
                    break;

                var slice = json.Substring(objectStart, objectEnd - objectStart + 1);
                if (TryParseBookmarkEntry(slice, out var entry))
                    list.Add(entry);

                idx = objectEnd + 1;
                var arrayEnd = json.IndexOf(']', arrayStart);
                if (arrayEnd >= 0 && idx > arrayEnd)
                    break;
            }

            return list;
        }

        private static BookmarkConfigEntry ParseQuickBookmark(string json, string key)
        {
            var token = "\"" + key + "\"";
            var idx = json.IndexOf(token, StringComparison.OrdinalIgnoreCase);
            if (idx < 0)
                return null;

            var colon = json.IndexOf(':', idx);
            if (colon < 0)
                return null;

            var valueStart = colon + 1;
            while (valueStart < json.Length && char.IsWhiteSpace(json[valueStart]))
                valueStart++;

            if (valueStart >= json.Length)
                return null;

            if (json[valueStart] == 'n') // null
                return null;

            if (json[valueStart] != '{')
                return null;

            var objectEnd = FindMatchingBrace(json, valueStart);
            if (objectEnd < 0)
                return null;

            var slice = json.Substring(valueStart, objectEnd - valueStart + 1);
            return TryParseBookmarkEntry(slice, out var entry) ? entry : null;
        }

        private static bool TryParseBookmarkEntry(string json, out BookmarkConfigEntry entry)
        {
            entry = new BookmarkConfigEntry
            {
                Name = ReadString(json, "name"),
                StreetName = ReadString(json, "street_name"),
                StreetNumber = ReadInt(json, "street_number"),
                WorldX = ReadFloat(json, "world_x"),
                WorldY = ReadFloat(json, "world_y"),
                WorldZ = ReadFloat(json, "world_z"),
                LocationLabel = ReadString(json, "location_label"),
                WorldOnly = ReadBool(json, "world_only")
            };

            var hasAddress = !string.IsNullOrWhiteSpace(entry.StreetName) || entry.StreetNumber > 0;
            var world = new UnityEngine.Vector3(entry.WorldX, entry.WorldY, entry.WorldZ);
            return hasAddress || world.sqrMagnitude > 0.01f;
        }

        private static void AppendQuickBookmarkProperty(
            StringBuilder sb,
            string indent,
            string key,
            BookmarkConfigEntry entry,
            CultureInfo inv)
        {
            sb.Append(indent).Append('"').Append(key).Append("\": ");
            if (entry == null)
                sb.Append("null");
            else
                AppendBookmarkEntry(sb, indent, entry, inv);
        }

        private static void AppendBookmarkEntry(
            StringBuilder sb,
            string indent,
            BookmarkConfigEntry entry,
            CultureInfo inv)
        {
            sb.Append("{\n");
            sb.Append(indent).Append("  \"name\": \"").Append(Escape(entry?.Name)).Append("\",\n");
            sb.Append(indent).Append("  \"street_name\": \"").Append(Escape(entry?.StreetName)).Append("\",\n");
            sb.Append(indent).Append("  \"street_number\": ").Append(entry?.StreetNumber ?? 0).Append(",\n");
            sb.Append(indent).Append("  \"world_x\": ").Append((entry?.WorldX ?? 0f).ToString(inv)).Append(",\n");
            sb.Append(indent).Append("  \"world_y\": ").Append((entry?.WorldY ?? 0f).ToString(inv)).Append(",\n");
            sb.Append(indent).Append("  \"world_z\": ").Append((entry?.WorldZ ?? 0f).ToString(inv)).Append(",\n");
            sb.Append(indent).Append("  \"location_label\": \"").Append(Escape(entry?.LocationLabel)).Append("\",\n");
            sb.Append(indent).Append("  \"world_only\": ").Append(entry != null && entry.WorldOnly ? "true" : "false").Append('\n');
            sb.Append(indent).Append('}');
        }

        private static int FindArrayStart(string json, string arrayKey)
        {
            var token = "\"" + arrayKey + "\"";
            var idx = json.IndexOf(token, StringComparison.OrdinalIgnoreCase);
            if (idx < 0)
                return -1;

            var bracket = json.IndexOf('[', idx);
            return bracket;
        }

        private static int FindMatchingBrace(string json, int openIndex)
        {
            var depth = 0;
            for (var i = openIndex; i < json.Length; i++)
            {
                if (json[i] == '{')
                    depth++;
                else if (json[i] == '}')
                {
                    depth--;
                    if (depth == 0)
                        return i;
                }
            }

            return -1;
        }

        private static string Escape(string value) =>
            string.IsNullOrEmpty(value) ? string.Empty : value.Replace("\\", "\\\\").Replace("\"", "\\\"");

        private static string ReadString(string json, string key)
        {
            var token = "\"" + key + "\"";
            var idx = json.IndexOf(token, StringComparison.OrdinalIgnoreCase);
            if (idx < 0)
                return string.Empty;

            var colon = json.IndexOf(':', idx);
            if (colon < 0)
                return string.Empty;

            var quote = json.IndexOf('"', colon + 1);
            if (quote < 0)
                return string.Empty;

            var sb = new StringBuilder();
            for (var i = quote + 1; i < json.Length; i++)
            {
                var ch = json[i];
                if (ch == '\\' && i + 1 < json.Length)
                {
                    var next = json[i + 1];
                    if (next == '"' || next == '\\')
                    {
                        sb.Append(next);
                        i++;
                        continue;
                    }
                }

                if (ch == '"')
                    break;

                sb.Append(ch);
            }

            return sb.ToString();
        }

        private static int ReadInt(string json, string key)
        {
            if (!TryReadNumberToken(json, key, out var raw))
                return 0;

            return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
                ? value
                : 0;
        }

        private static float ReadFloat(string json, string key)
        {
            if (!TryReadNumberToken(json, key, out var raw))
                return 0f;

            return float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
                ? value
                : 0f;
        }

        private static bool ReadBool(string json, string key)
        {
            if (!TryReadNumberToken(json, key, out var raw))
                return false;

            return raw.Equals("true", StringComparison.OrdinalIgnoreCase);
        }

        private static bool TryReadNumberToken(string json, string key, out string raw)
        {
            raw = null;
            var token = "\"" + key + "\"";
            var idx = json.IndexOf(token, StringComparison.OrdinalIgnoreCase);
            if (idx < 0)
                return false;

            var colon = json.IndexOf(':', idx);
            if (colon < 0)
                return false;

            var end = json.IndexOfAny(new[] { ',', '}', '\n', '\r' }, colon + 1);
            if (end < 0)
                end = json.Length;

            raw = json.Substring(colon + 1, end - colon - 1).Trim().Trim('"');
            return true;
        }
    }
}
