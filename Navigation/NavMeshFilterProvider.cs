using Il2Cpp;
using Il2CppHelpers;
using UnityEngine;
using UnityEngine.AI;

namespace VoogleRoute.Navigation;

/// <summary>
/// Filtre piéton (jeu) vs filtre véhicule (agentType route) pour rester sur la chaussée en voiture.
/// </summary>
internal static class NavMeshFilterProvider
{
    private static NavMeshQueryFilter _vehicleFilter;
    private static int _vehicleFilterAgentTypeId = int.MinValue;

    /// <summary>Filtre piéton (graphe complet, trottoirs inclus).</summary>
    public static NavMeshQueryFilter GetPedestrianRouteFilter() =>
        PlayerController.navMeshQueryFilter;

    /// <summary>Alias legacy.</summary>
    public static NavMeshQueryFilter GetRouteCalculationFilter() =>
        GetPedestrianRouteFilter();

    /// <summary>Filtre chaussée — agent NavMesh véhicule + masque du joueur.</summary>
    public static NavMeshQueryFilter GetVehicleRouteFilter()
    {
        var pedestrian = PlayerController.navMeshQueryFilter;
        var vehicleAgentId = ResolveVehicleAgentTypeId(pedestrian.agentTypeID);
        if (_vehicleRouteFilterAgentTypeId != vehicleAgentId)
        {
            _vehicleRouteFilter = pedestrian;
            _vehicleRouteFilter.agentTypeID = vehicleAgentId;
            _vehicleRouteFilterAgentTypeId = vehicleAgentId;
        }

        return _vehicleRouteFilter;
    }

    private static NavMeshQueryFilter _vehicleRouteFilter;
    private static int _vehicleRouteFilterAgentTypeId = int.MinValue;

    /// <summary>Filtre agent route (snap affichage véhicule).</summary>
    public static NavMeshQueryFilter GetVehicleSnapFilter()
    {
        var pedestrian = PlayerController.navMeshQueryFilter;
        var vehicleAgentId = ResolveVehicleAgentTypeId(pedestrian.agentTypeID);
        if (_vehicleFilterAgentTypeId != vehicleAgentId)
        {
            _vehicleFilter = pedestrian;
            _vehicleFilter.agentTypeID = vehicleAgentId;
            _vehicleFilterAgentTypeId = vehicleAgentId;
        }

        return _vehicleFilter;
    }

    public static bool AllowSidewalkFallback => true;

    private static int ResolveVehicleAgentTypeId(int pedestrianAgentTypeId)
    {
        try
        {
            var agent = PlayerHelper.PlayerController?.Character?.navmeshAgent;
            if (agent != null && agent.agentTypeID != pedestrianAgentTypeId)
                return agent.agentTypeID;
        }
        catch
        {
            // ignore
        }

        var bestId = -1;
        var bestRadius = 0f;
        for (var id = 0; id < 16; id++)
        {
            if (id == pedestrianAgentTypeId)
                continue;
            try
            {
                var settings = NavMesh.GetSettingsByID(id);
                if (settings.agentTypeID != id)
                    continue;
                if (settings.agentRadius > bestRadius)
                {
                    bestRadius = settings.agentRadius;
                    bestId = id;
                }
            }
            catch
            {
                // ignore
            }
        }

        if (bestId >= 0)
            return bestId;

        return pedestrianAgentTypeId != 1 ? 1 : 2;
    }
}
