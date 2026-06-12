using Buildings;
using Helpers;
using Streets;
using UnityEngine;

namespace VoogleRoute.Navigation
{
    internal static class MapAddressResolver
    {
        private const float MaxSnapDistance = 80f;
        private const float MaxSnapDistanceSq = MaxSnapDistance * MaxSnapDistance;

        internal static bool TryResolveFromClick(
            Vector3 worldPos,
            CityBuildingController clickedBuilding,
            out Address address,
            out string displayName)
        {
            address = null;
            displayName = "";

            if (clickedBuilding != null)
            {
                address = clickedBuilding.building?.Address;
                if (address != null)
                {
                    displayName = FormatLabel(address, clickedBuilding);
                    return true;
                }
            }

            if (!TryFindNearestBuilding(worldPos, out var nearest, out var distSq) || distSq > MaxSnapDistanceSq)
                return false;

            address = nearest.building?.Address;
            if (address == null)
                return false;

            displayName = FormatLabel(address, nearest);
            return true;
        }

        private static bool TryFindNearestBuilding(
            Vector3 worldPos,
            out CityBuildingController best,
            out float bestDistSq)
        {
            best = null;
            bestDistSq = float.MaxValue;

            if (!CityManager.IsInitialized)
                return false;

            var controllers = CityManager.Instance?.cityBuildingControllers;
            if (controllers == null)
                return false;

            foreach (var cbc in controllers)
            {
                if (cbc == null)
                    continue;

                var reference = GetReferencePosition(cbc);
                var distSq = (reference - worldPos).sqrMagnitude;
                if (distSq < bestDistSq)
                {
                    bestDistSq = distSq;
                    best = cbc;
                }
            }

            return best != null;
        }

        private static Vector3 GetReferencePosition(CityBuildingController cbc)
        {
            var poi = cbc.GetPoiPosition();
            return poi != null ? poi.position : cbc.transform.position;
        }

        private static string FormatLabel(Address address, CityBuildingController cbc)
        {
            try
            {
                var reg = BuildingHelper.GetBuildingRegistration(address);
                if (reg != null && !string.IsNullOrEmpty(reg.BusinessName))
                    return reg.BusinessName + " — " + address.ToFormattedString();
            }
            catch
            {
                // ignore
            }

            _ = cbc;
            return address.ToFormattedString();
        }
    }
}
