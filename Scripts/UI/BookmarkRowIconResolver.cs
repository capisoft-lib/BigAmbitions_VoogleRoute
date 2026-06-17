using Buildings;
using Helpers;
using Streets;
using UnityEngine;
using VoogleRoute.Navigation;

using Capisoft.Lib.BaUnifiedUI.Fluent;

namespace VoogleRoute.UI
{
    internal readonly struct BookmarkRowIcon
    {
        internal Sprite Icon { get; }
        internal Color Background { get; }

        internal BookmarkRowIcon(Sprite icon, Color background)
        {
            Icon = icon;
            Background = background;
        }

        internal bool HasIcon => Icon != null;
    }

    internal static class BookmarkRowIconResolver
    {
        private const string ResidentialBuildingType = "ba:buildingtype_residential";
        private const string WholesaleBusinessType = "ba:businesstype_wholesalestore";

        internal static bool TryGetForBookmark(BookmarkEntry entry, out BookmarkRowIcon rowIcon)
        {
            rowIcon = default;
            if (!TryGetBuildingRegistration(entry, out var registration))
                return false;

            return TryFromRegistration(registration, out rowIcon);
        }

        internal static bool TryGetForQuickRow(QuickBookmarkKind kind, out BookmarkRowIcon rowIcon)
        {
            rowIcon = default;
            switch (kind)
            {
                case QuickBookmarkKind.LastCar:
                    if (!ParkedVehicleStore.HasParkedPosition)
                        return false;
                    return TryGetCarIcon(out rowIcon);

                case QuickBookmarkKind.LastHome:
                    if (!QuickBookmarkStore.TryGet(QuickBookmarkKind.LastHome, out _))
                        return false;
                    return TryGetResidentialIcon(out rowIcon);

                case QuickBookmarkKind.LastShop:
                    if (!QuickBookmarkStore.TryGet(QuickBookmarkKind.LastShop, out var shopEntry))
                        return false;
                    if (TryGetBuildingRegistration(shopEntry, out var shopRegistration) &&
                        TryFromRegistration(shopRegistration, out rowIcon))
                        return true;
                    return TryGetDefaultShopIcon(out rowIcon);

                default:
                    return false;
            }
        }

        private static bool TryGetBuildingRegistration(BookmarkEntry entry, out BuildingRegistration registration)
        {
            registration = null;
            if (entry == null || !entry.HasAddress)
                return false;

            try
            {
                var address = entry.ToAddress();
                if (address == null || string.IsNullOrWhiteSpace(address.streetName) || address.streetNumber <= 0)
                    return false;

                registration = BuildingHelper.GetBuildingRegistration(address);
                return registration?.BuildingCached != null;
            }
            catch
            {
                return false;
            }
        }

        private static bool TryFromRegistration(BuildingRegistration registration, out BookmarkRowIcon rowIcon)
        {
            rowIcon = default;
            if (registration == null)
                return false;

            try
            {
                var icon = registration.GetPOIIcon();
                if (icon == null)
                    return false;

                rowIcon = new BookmarkRowIcon(icon, registration.GetPOIBackgroundColor());
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool TryGetResidentialIcon(out BookmarkRowIcon rowIcon)
        {
            rowIcon = default;
            try
            {
                var data = BuildingTypeHelper.GetData(ResidentialBuildingType);
                if (data?.poiIcon == null)
                    return false;

                rowIcon = new BookmarkRowIcon(data.poiIcon, data.mapFilterColor);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool TryGetDefaultShopIcon(out BookmarkRowIcon rowIcon)
        {
            rowIcon = default;
            try
            {
                var businessType = BusinessTypeHelper.GetData(WholesaleBusinessType);
                if (businessType?.icon == null)
                    return false;

                rowIcon = new BookmarkRowIcon(businessType.icon, businessType.cityMapFilterColor);
                return true;
            }
            catch
            {
                return false;
            }
        }

        internal static bool TryGetForVehicleRow(out BookmarkRowIcon rowIcon) =>
            TryGetCarIcon(out rowIcon);

        private static bool TryGetCarIcon(out BookmarkRowIcon rowIcon)
        {
            rowIcon = default;
            BaUi.EnsureReady();
            if (!BaUi.TryGetCarIcon(out var icon))
                return false;

            rowIcon = new BookmarkRowIcon(icon, BaUi.Colors.CarPoiBackground);
            return true;
        }
    }
}
