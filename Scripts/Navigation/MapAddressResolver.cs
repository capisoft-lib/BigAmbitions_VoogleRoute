using BaPlayerLocation.Subscriber;
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

        internal static bool TryFindNearestBuildingAt(
            Vector3 worldPos,
            out CityBuildingController best,
            float maxDistance = MaxSnapDistance)
        {
            var maxSq = maxDistance * maxDistance;
            if (!TryFindNearestBuilding(worldPos, out best, out var distSq) || distSq > maxSq)
            {
                best = null;
                return false;
            }

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

        internal static bool TryResolveCurrentPlayer(
            out Vector3 worldPos,
            out Address address,
            out string displayLabel)
        {
            worldPos = default;
            address = null;
            displayLabel = "";

            if (!TryGetPlayerWorldPosition(out worldPos))
                return false;

            if (TryResolveIndoorAddress(out address, out displayLabel))
                return true;

            TryFindNearestBuilding(worldPos, out var nearest, out _);
            return TryResolveBookmarkClick(worldPos, nearest, out address, out displayLabel);
        }

        private static bool TryGetPlayerWorldPosition(out Vector3 worldPos)
        {
            worldPos = default;

            if (PlayerLocationSession.IsAvailable)
            {
                worldPos = PlayerLocationSession.Snapshot.Position;
                return worldPos.sqrMagnitude > 0.01f;
            }

            try
            {
                var controller = PlayerHelper.PlayerController;
                if (controller != null)
                {
                    worldPos = controller.transform.position;
                    return true;
                }
            }
            catch
            {
                // ignore
            }

            return false;
        }

        private static bool TryResolveIndoorAddress(out Address address, out string displayLabel)
        {
            address = null;
            displayLabel = "";

            try
            {
                if (!BuildingManager.IsInsideBuilding)
                    return false;

                var registration = BuildingManager.Instance?.buildingRegistration;
                address = registration?.Address;
                if (address == null)
                    return false;

                displayLabel = FormatLabel(address, null);
                return true;
            }
            catch
            {
                return false;
            }
        }

        internal static bool TryResolveBookmarkClick(
            Vector3 worldPos,
            CityBuildingController clickedBuilding,
            out Address address,
            out string displayLabel)
        {
            if (TryResolveFromClick(worldPos, clickedBuilding, out address, out displayLabel))
                return true;

            address = null;
            displayLabel = FormatCoordinates(worldPos);
            return true;
        }

        private static string FormatCoordinates(Vector3 worldPos) =>
            "(" + Mathf.RoundToInt(worldPos.x) + ", " + Mathf.RoundToInt(worldPos.z) + ")";

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
