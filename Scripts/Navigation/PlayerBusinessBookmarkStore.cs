using System;
using System.Collections.Generic;
using Streets;

namespace VoogleRoute.Navigation
{
    /// <summary>Read-only shortcuts rebuilt from the current save, never persisted as bookmarks.</summary>
    internal static class PlayerBusinessBookmarkStore
    {
        private static readonly List<BookmarkEntry> Entries = new List<BookmarkEntry>();
        internal static IReadOnlyList<BookmarkEntry> All => Entries;

        internal static void Clear() => Entries.Clear();

        internal static void Refresh()
        {
            Entries.Clear();
            var registrations = SaveGameManager.Current?.BuildingRegistrations;
            if (registrations == null)
                return;

            var addresses = new HashSet<(string, int)>();
            foreach (var registration in registrations)
            {
                // RentedByPlayer is also the game's IsPlayerOwnedBusiness flag. Owning the
                // real estate alone does not mean the player operates the tenant's business.
                if (registration == null || !registration.RentedByPlayer ||
                    string.IsNullOrWhiteSpace(registration.BusinessName) ||
                    string.IsNullOrWhiteSpace(registration.StreetName) ||
                    string.Equals(registration.businessTypeName, "ba:businesstype_empty", StringComparison.Ordinal))
                    continue;

                try
                {
                    if (registration.GetBuildingType() == "ba:buildingtype_residential")
                        continue;

                    var street = registration.StreetName;
                    var number = registration.StreetNumber;
                    if (!addresses.Add((street.ToUpperInvariant(), number)))
                        continue;

                    Entries.Add(new BookmarkEntry
                    {
                        Name = registration.BusinessName,
                        StreetName = street,
                        StreetNumber = number,
                        LocationLabel = new Address(street, number).ToFormattedString()
                    });
                }
                catch (Exception ex)
                {
                    // One unavailable building must not hide the rest of the portfolio.
                    ModLog.Error("Failed to create a player business shortcut", ex);
                }
            }

            Entries.Sort((a, b) =>
            {
                var name = StringComparer.CurrentCultureIgnoreCase.Compare(a.DisplayName, b.DisplayName);
                if (name != 0) return name;
                var street = StringComparer.Ordinal.Compare(a.StreetName, b.StreetName);
                return street != 0 ? street : a.StreetNumber.CompareTo(b.StreetNumber);
            });
        }
    }
}
