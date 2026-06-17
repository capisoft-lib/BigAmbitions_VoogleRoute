using System;
using System.Collections.Generic;
using Buildings;
using Helpers;
using Streets;
using UnityEngine;
using VoogleRoute;

namespace VoogleRoute.Navigation
{
    /// <summary>Last 50 visited buildings (any type), most recent first.</summary>
    internal static class VisitHistoryStore
    {
        internal const int MaxCount = 50;

        private static readonly List<BookmarkEntry> Entries = new List<BookmarkEntry>();

        internal static event Action Changed;

        internal static int Count => Entries.Count;

        internal static BookmarkEntry GetAt(int index) =>
            index >= 0 && index < Entries.Count ? Entries[index] : null;

        internal static void LoadFromConfig(IReadOnlyList<BookmarkConfigEntry> saved)
        {
            Entries.Clear();
            if (saved == null)
                return;

            for (var i = 0; i < saved.Count && Entries.Count < MaxCount; i++)
            {
                if (BookmarkStore.TryEntryFromConfig(saved[i], out var entry))
                    Entries.Add(entry);
            }

            if (DeduplicateEntries())
                Persist();
        }

        internal static void OnEnterBuildingDelayed(Address address)
        {
            if (address == null || !BuildingManager.IsInsideBuilding)
                return;

            try
            {
                var manager = BuildingManager.Instance;
                var registration = manager?.buildingRegistration;
                if (registration == null)
                    return;

                if (!TryCreateFromRegistration(address, registration, out var entry))
                    return;

                InsertUniqueAtFront(entry);
            }
            catch (Exception ex)
            {
                ModLog.Error("Visit history enter-building handler failed", ex);
            }
        }

        private static void InsertUniqueAtFront(BookmarkEntry entry)
        {
            for (var i = Entries.Count - 1; i >= 0; i--)
            {
                if (Entries[i].SamePlaceAs(entry))
                {
                    Entries.RemoveAt(i);
                    break;
                }
            }

            Entries.Insert(0, entry);
            while (Entries.Count > MaxCount)
                Entries.RemoveAt(Entries.Count - 1);

            Persist();
            Changed?.Invoke();
        }

        /// <summary>Keeps the first (most recent) entry per place; returns true if any duplicate was removed.</summary>
        private static bool DeduplicateEntries()
        {
            if (Entries.Count < 2)
                return false;

            var removed = false;
            for (var i = 0; i < Entries.Count; i++)
            {
                for (var j = Entries.Count - 1; j > i; j--)
                {
                    if (!Entries[i].SamePlaceAs(Entries[j]))
                        continue;

                    Entries.RemoveAt(j);
                    removed = true;
                }
            }

            while (Entries.Count > MaxCount)
            {
                Entries.RemoveAt(Entries.Count - 1);
                removed = true;
            }

            if (removed)
                Changed?.Invoke();

            return removed;
        }

        internal static void Clear() => Entries.Clear();

        private static void Persist()
        {
            var saved = new List<BookmarkConfigEntry>(Entries.Count);
            for (var i = 0; i < Entries.Count; i++)
                saved.Add(BookmarkStore.ToConfigEntry(Entries[i]));

            BookmarkFileStore.SetVisitHistory(saved);
        }

        private static bool TryCreateFromRegistration(
            Address address,
            BuildingRegistration registration,
            out BookmarkEntry entry)
        {
            entry = null;
            if (registration == null)
                return false;

            var streetName = registration.StreetName;
            var streetNumber = registration.StreetNumber;
            if (string.IsNullOrWhiteSpace(streetName))
            {
                streetName = address?.streetName ?? "";
                streetNumber = address?.streetNumber ?? 0;
            }

            if (string.IsNullOrWhiteSpace(streetName) && streetNumber <= 0)
                return false;

            var label = registration.BusinessName;
            if (string.IsNullOrWhiteSpace(label))
            {
                try
                {
                    label = new Address(streetName, streetNumber).ToFormattedString();
                }
                catch
                {
                    label = streetName + " " + streetNumber;
                }
            }

            entry = new BookmarkEntry
            {
                StreetName = streetName,
                StreetNumber = streetNumber,
                LocationLabel = label
            };

            var resolvedAddress = new Address(streetName, streetNumber);
            if (TryResolveOutdoorPosition(resolvedAddress, out var worldPos))
            {
                entry.WorldX = worldPos.x;
                entry.WorldY = worldPos.y;
                entry.WorldZ = worldPos.z;
            }

            return entry.HasAddress || entry.HasWorldPosition;
        }

        private static bool TryResolveOutdoorPosition(Address address, out Vector3 worldPos)
        {
            if (DestinationResolver.TryResolveWorldPosition(address, out worldPos))
                return true;

            try
            {
                if (CityManager.IsInitialized)
                {
                    var building = CityManager.Instance?.FindCityBuildingController(address);
                    var poi = building?.GetPoiPosition();
                    if (poi != null)
                    {
                        worldPos = poi.position;
                        return true;
                    }
                }
            }
            catch
            {
                // ignore
            }

            worldPos = default;
            return false;
        }
    }
}
