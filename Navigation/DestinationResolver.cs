using Il2Cpp;
using Il2CppHelpers;
using Il2CppVehicles.DeliveryDriverJob;
using UnityEngine;

namespace VoogleRoute.Navigation;

/// <summary>
/// Lit uniquement <see cref="GameInstance.customDestination"/> (GPS carte).
/// Cible = porte d'entrée du bâtiment quand disponible.
/// </summary>
public static class DestinationResolver
{
    private static Address? _lastResolvedAddress;
    private static bool _hasLastAddress;
    private static float _lastPollTime;
    private static string? _lastResolveWarnKey;
    private static float _lastResolveWarnTime;

    public static void Poll()
    {
        if (!GameState.ShouldRunNavigationSystems())
            return;

        if (!GameState.IsWorldReady())
            return;

        if (Time.unscaledTime - _lastPollTime < 0.25f)
            return;
        _lastPollTime = Time.unscaledTime;
        SyncFromMapDestination();
    }

    private static void SyncFromMapDestination()
    {
        try
        {
            var game = SaveGameManager.Current;
            if (game == null)
                return;

            var address = game.customDestination;
            if (IsInvalidAddress(address))
            {
                if (NavigationTargetTracker.HasMapGpsTarget)
                    NavigationTargetTracker.ClearMapGpsTarget("map destination cleared");
                _hasLastAddress = false;
                _lastResolvedAddress = null;
                return;
            }

            if (_hasLastAddress && AddressesEqual(_lastResolvedAddress, address) &&
                NavigationTargetTracker.HasMapGpsTarget)
                return;

            _lastResolvedAddress = address;
            _hasLastAddress = true;

            if (TryResolveWorldPosition(address, out var worldPos))
                NavigationTargetTracker.SetMapGpsTarget(worldPos);
            else
                WarnResolveOnce($"GPS carte {address.streetName} {address.streetNumber} — porte d'entrée introuvable");
        }
        catch (System.Exception ex)
        {
            WarnResolveOnce($"SyncFromMapDestination : {ex.Message}");
        }
    }

    public static bool TryResolveWorldPosition(Address address, out Vector3 worldPosition)
    {
        worldPosition = default;
        if (!GameState.IsWorldReady())
            return false;

        // 1) Portes d'entrée sur le CityBuildingController (fiable une fois la ville chargée).
        try
        {
            if (CityManager.IsInitialized)
            {
                var cbc = CityManager.Instance?.FindCityBuildingController(address);
                if (cbc != null && TryGetEntranceDoorPosition(cbc, out worldPosition))
                    return true;
            }
        }
        catch
        {
            // ignore
        }

        // 2) Helper livraison (peut NRE si appelé trop tôt — seulement ville prête).
        try
        {
            var entrance = DeliveryJobHelper.GetAddressEntranceTransform(address);
            if (entrance != null)
            {
                worldPosition = entrance.position;
                return true;
            }
        }
        catch
        {
            // ignore — système livraison pas prêt ou adresse invalide
        }

        // 3) Secours via registration + portes.
        try
        {
            var reg = Il2CppHelpers.BuildingHelper.GetBuildingRegistration(address);
            if (reg != null && CityManager.IsInitialized)
            {
                var cbc = CityManager.Instance?.FindCityBuildingController(reg.Address);
                if (cbc != null && TryGetEntranceDoorPosition(cbc, out worldPosition))
                    return true;
            }
        }
        catch
        {
            // ignore
        }

        // 4) Dernier recours : POI carte (pas la porte).
        try
        {
            if (CityManager.IsInitialized)
            {
                var cbc = CityManager.Instance?.FindCityBuildingController(address);
                if (cbc != null)
                {
                    var poi = cbc.GetPoiPosition();
                    if (poi != null)
                    {
                        worldPosition = poi.position;
                        return true;
                    }
                }
            }
        }
        catch
        {
            // ignore
        }

        return false;
    }

    private static void WarnResolveOnce(string message)
    {
        if (!GameState.IsWorldReady())
            return;
        if (_lastResolveWarnKey == message && Time.unscaledTime - _lastResolveWarnTime < 10f)
            return;
        _lastResolveWarnKey = message;
        _lastResolveWarnTime = Time.unscaledTime;
        _ = message;
    }

    private static bool TryGetEntranceDoorPosition(CityBuildingController cbc, out Vector3 worldPosition)
    {
        worldPosition = default;
        var doors = cbc.entranceDoors;
        if (doors == null || doors.Length == 0)
            return false;

        Transform? best = null;
        var bestDist = float.MaxValue;
        Vector3? reference = null;
        try
        {
            if (PlayerHelper.PlayerController != null)
                reference = PlayerHelper.PlayerController.transform.position;
        }
        catch
        {
            // ignore
        }

        for (var i = 0; i < doors.Length; i++)
        {
            var door = doors[i];
            var t = door?.doorTransform;
            if (t == null)
                continue;

            if (reference == null)
            {
                worldPosition = t.position;
                return true;
            }

            var d = (t.position - reference.Value).sqrMagnitude;
            if (d < bestDist)
            {
                bestDist = d;
                best = t;
            }
        }

        if (best == null)
            return false;

        worldPosition = best.position;
        return true;
    }

    public static void Clear()
    {
        _hasLastAddress = false;
        _lastResolvedAddress = null;
    }

    public static bool TryGetMapDestination(out Address address)
    {
        address = null!;
        try
        {
            var game = SaveGameManager.Current;
            if (game == null)
                return false;
            address = game.customDestination;
            return !IsInvalidAddress(address);
        }
        catch
        {
            return false;
        }
    }

    private static bool IsInvalidAddress(Address address)
    {
        try
        {
            if (Address.undefined != null && address == Address.undefined)
                return true;
        }
        catch
        {
            // ignore
        }

        return address.streetNumber <= 0 && (int)address.streetName <= 0;
    }

    private static bool AddressesEqual(Address? a, Address b) =>
        a != null && a.streetName == b.streetName && a.streetNumber == b.streetNumber;
}
