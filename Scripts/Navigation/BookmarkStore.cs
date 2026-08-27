using System;
using System.Collections.Generic;
using Streets;
using UnityEngine;
using VoogleRoute;

namespace VoogleRoute.Navigation
{
    internal static class BookmarkStore
    {
        private static readonly List<BookmarkEntry> Entries = new List<BookmarkEntry>();

        internal static IReadOnlyList<BookmarkEntry> All => Entries;

        internal static event Action Changed;

        internal static void LoadFromConfig(IReadOnlyList<BookmarkConfigEntry> saved, bool persistChanges = true)
        {
            Entries.Clear();
            if (saved == null)
                return;

            var labelsChanged = false;
            for (var i = 0; i < saved.Count; i++)
            {
                if (saved[i] == null || !saved[i].UserCreated)
                    continue;

                if (!BookmarkStore.TryEntryFromConfig(saved[i], out var entry))
                    continue;

                if (BookmarkLabelResolver.TryRefreshStoredLabel(entry))
                    labelsChanged = true;

                Entries.Add(entry);
            }

            if (persistChanges && labelsChanged)
                Persist();
        }

        internal static bool TryEntryFromConfig(BookmarkConfigEntry item, out BookmarkEntry entry)
        {
            entry = null;
            if (item == null)
                return false;

            entry = new BookmarkEntry
            {
                Name = item.Name ?? "",
                StreetName = item.StreetName ?? "",
                StreetNumber = item.StreetNumber,
                WorldX = item.WorldX,
                WorldY = item.WorldY,
                WorldZ = item.WorldZ,
                LocationLabel = item.LocationLabel ?? "",
                WorldOnly = item.WorldOnly,
                UserCreated = item.UserCreated
            };

            return entry.HasAddress || entry.HasWorldPosition;
        }

        internal static BookmarkConfigEntry ToConfigEntry(BookmarkEntry entry)
        {
            if (entry == null)
                return null;

            return new BookmarkConfigEntry
            {
                Name = entry.Name,
                StreetName = entry.StreetName,
                StreetNumber = entry.StreetNumber,
                WorldX = entry.WorldX,
                WorldY = entry.WorldY,
                WorldZ = entry.WorldZ,
                LocationLabel = entry.LocationLabel,
                WorldOnly = entry.WorldOnly,
                UserCreated = entry.UserCreated
            };
        }

        internal static List<BookmarkConfigEntry> ExportToConfig()
        {
            var list = new List<BookmarkConfigEntry>(Entries.Count);
            for (var i = 0; i < Entries.Count; i++)
                list.Add(ToConfigEntry(Entries[i]));

            return list;
        }

        internal static bool CanAdd() => true;

        internal static bool TryAdd(BookmarkEntry entry)
        {
            if (entry == null)
                return false;

            entry.UserCreated = true;
            Entries.Add(entry);
            Persist();
            Changed?.Invoke();
            return true;
        }

        internal static void ClearAll()
        {
            if (Entries.Count == 0)
                return;

            Entries.Clear();
            Persist();
            Changed?.Invoke();
        }

        internal static bool TryRemoveAt(int index)
        {
            if (index < 0 || index >= Entries.Count)
                return false;

            Entries.RemoveAt(index);
            Persist();
            Changed?.Invoke();
            return true;
        }

        internal static BookmarkEntry GetAt(int index) =>
            index >= 0 && index < Entries.Count ? Entries[index] : null;

        private static void Persist() => BookmarkDataSaveStore.PersistCurrent();
    }

    internal sealed class BookmarkConfigEntry
    {
        public string Name { get; set; }

        public string StreetName { get; set; }

        public int StreetNumber { get; set; }

        public float WorldX { get; set; }

        public float WorldY { get; set; }

        public float WorldZ { get; set; }

        public string LocationLabel { get; set; }

        public bool WorldOnly { get; set; }

        public bool UserCreated { get; set; }
    }

    internal sealed class QuickBookmarksConfig
    {
        public BookmarkConfigEntry LastCar { get; set; }

        public BookmarkConfigEntry LastHome { get; set; }

        public BookmarkConfigEntry LastShop { get; set; }
    }
}
