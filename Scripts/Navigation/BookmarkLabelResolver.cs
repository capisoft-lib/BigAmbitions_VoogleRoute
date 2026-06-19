using Buildings;
using Helpers;
using Streets;

namespace VoogleRoute.Navigation
{
    /// <summary>Resolves bookmark/history labels from the active save when possible.</summary>
    internal static class BookmarkLabelResolver
    {
        internal static string GetDisplayName(BookmarkEntry entry)
        {
            if (entry == null)
                return "";

            if (!string.IsNullOrWhiteSpace(entry.Name))
                return entry.Name;

            if (TryGetCurrentBusinessName(entry, out var businessName))
                return businessName;

            if (!string.IsNullOrWhiteSpace(entry.LocationLabel))
                return entry.LocationLabel;

            return FormatAddress(entry);
        }

        internal static bool TryRefreshStoredLabel(BookmarkEntry entry)
        {
            if (entry == null || !string.IsNullOrWhiteSpace(entry.Name))
                return false;

            if (!TryGetCurrentBusinessName(entry, out var businessName))
                return false;

            if (string.Equals(entry.LocationLabel, businessName, System.StringComparison.Ordinal))
                return false;

            entry.LocationLabel = businessName;
            return true;
        }

        private static bool TryGetCurrentBusinessName(BookmarkEntry entry, out string businessName)
        {
            businessName = null;
            if (entry == null || !entry.HasAddress)
                return false;

            try
            {
                var address = entry.ToAddress();
                if (address == null || string.IsNullOrWhiteSpace(address.streetName) || address.streetNumber <= 0)
                    return false;

                var registration = BuildingHelper.GetBuildingRegistration(address);
                businessName = registration?.BusinessName;
                return !string.IsNullOrWhiteSpace(businessName);
            }
            catch
            {
                return false;
            }
        }

        private static string FormatAddress(BookmarkEntry entry)
        {
            if (!entry.HasAddress)
                return "";

            try
            {
                return entry.ToAddress()?.ToFormattedString() ?? "";
            }
            catch
            {
                return entry.StreetName + " " + entry.StreetNumber;
            }
        }
    }
}
