using Helpers;
using UnityEngine;
using UnityEngine.AI;

namespace VoogleRoute.Navigation
{
    
    internal static class NavMeshFilterProvider
    {
        private static NavMeshQueryFilter _vehicleFilter;
        private static int _vehicleFilterAgentTypeId = int.MinValue;

        internal static NavMeshQueryFilter GetPedestrianRouteFilter() =>
            PlayerController.navMeshQueryFilter;

        internal static NavMeshQueryFilter GetVehicleRouteFilter()
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
}
