using System;
using Buildings;
using Entities;
using Helpers;
using Streets;
using UnityEngine;
using VoogleRoute;

namespace VoogleRoute.Navigation
{
    internal enum QuickBookmarkKind
    {
        LastCar,
        LastHome,
        LastShop
    }

    /// <summary>Auto-tracked Last Car / Home / Shop shortcuts for the map bookmarks panel.</summary>
    internal static class QuickBookmarkStore
    {
        internal const int SlotCount = 3;

        private static BookmarkEntry _lastHome;
        private static BookmarkEntry _lastShop;
        private static bool _hasLastHome;
        private static bool _hasLastShop;

        internal static event Action Changed;

        internal static bool TryGet(QuickBookmarkKind kind, out BookmarkEntry entry)
        {
            entry = null;
            switch (kind)
            {
                case QuickBookmarkKind.LastCar:
                    return TryGetLastCar(out entry);
                case QuickBookmarkKind.LastHome:
                    return _hasLastHome ? (entry = _lastHome) != null : false;
                case QuickBookmarkKind.LastShop:
                    return _hasLastShop ? (entry = _lastShop) != null : false;
                default:
                    return false;
            }
        }

        internal static bool TryGetLastCar(out BookmarkEntry entry)
        {
            entry = null;
            if (!ParkedVehicleStore.HasParkedPosition)
                return false;

            return TryCreateFromWorldPosition(ParkedVehicleStore.ParkedPosition, worldOnly: true, out entry);
        }

        internal static void OnVehicleParked()
        {
            Persist();
            Changed?.Invoke();
        }

        internal static void LoadFromConfig(QuickBookmarksConfig saved)
        {
            _lastHome = null;
            _lastShop = null;
            _hasLastHome = false;
            _hasLastShop = false;

            ParkedVehicleStore.LoadFromConfig(saved?.LastCar);

            if (saved?.LastHome != null && BookmarkStore.TryEntryFromConfig(saved.LastHome, out var home))
            {
                _lastHome = home;
                _hasLastHome = true;
            }

            if (saved?.LastShop != null && BookmarkStore.TryEntryFromConfig(saved.LastShop, out var shop))
            {
                _lastShop = shop;
                _hasLastShop = true;
            }
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

                var updated = false;

                if (IsPlayerHome(registration) &&
                    TryCreateFromRegistration(address, registration, out var home))
                {
                    _lastHome = home;
                    _hasLastHome = true;
                    updated = true;
                }

                if (IsBusiness(registration, manager) &&
                    TryCreateFromRegistration(address, registration, out var shop))
                {
                    _lastShop = shop;
                    _hasLastShop = true;
                    updated = true;
                }

                if (updated)
                {
                    Persist();
                    Changed?.Invoke();
                }
            }
            catch (Exception ex)
            {
                ModLog.Error("Quick bookmark enter-building handler failed", ex);
            }
        }

        internal static void Clear()
        {
            _lastHome = null;
            _lastShop = null;
            _hasLastHome = false;
            _hasLastShop = false;
        }

        private static void Persist()
        {
            var saved = new QuickBookmarksConfig();

            if (ParkedVehicleStore.HasParkedPosition &&
                TryCreateFromWorldPosition(ParkedVehicleStore.ParkedPosition, worldOnly: true, out var lastCar))
                saved.LastCar = BookmarkStore.ToConfigEntry(lastCar);

            if (_hasLastHome && _lastHome != null)
                saved.LastHome = BookmarkStore.ToConfigEntry(_lastHome);

            if (_hasLastShop && _lastShop != null)
                saved.LastShop = BookmarkStore.ToConfigEntry(_lastShop);

            BookmarkFileStore.SetQuickBookmarks(saved);
        }

        private static bool IsPlayerHome(BuildingRegistration registration)
        {
            if (registration.BuildingCached?.BuildingType != "ba:buildingtype_residential")
                return false;

            return registration.RentedByPlayer || registration.BuildingOwnedByPlayer;
        }

        private static bool IsBusiness(BuildingRegistration registration, BuildingManager manager)
        {
            if (registration == null || IsPlayerHome(registration))
                return false;

            var businessType = manager?.businessType ?? BusinessTypeHelper.GetData(registration);
            var businessTypeName = registration.businessTypeName ?? "";
            if (businessType == null && string.IsNullOrWhiteSpace(businessTypeName))
                return false;

            return businessTypeName != "ba:businesstype_empty" &&
                   businessTypeName != "ba:businesstype_headquarters" &&
                   businessTypeName != "ba:businesstype_factory";
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

        private static bool TryCreateFromWorldPosition(Vector3 worldPos, bool worldOnly, out BookmarkEntry entry)
        {
            entry = null;
            if (worldPos.sqrMagnitude < 0.01f)
                return false;

            string label;
            Address address = null;
            if (worldOnly)
            {
                label = "(" + Mathf.RoundToInt(worldPos.x) + ", " + Mathf.RoundToInt(worldPos.z) + ")";
            }
            else
            {
                MapAddressResolver.TryResolveBookmarkClick(worldPos, null, out address, out label);
                label ??= "";
            }

            entry = new BookmarkEntry
            {
                WorldX = worldPos.x,
                WorldY = worldPos.y,
                WorldZ = worldPos.z,
                WorldOnly = worldOnly,
                LocationLabel = label
            };

            if (!worldOnly && address != null)
            {
                entry.StreetName = address.streetName;
                entry.StreetNumber = address.streetNumber;
            }

            return true;
        }
    }
}
