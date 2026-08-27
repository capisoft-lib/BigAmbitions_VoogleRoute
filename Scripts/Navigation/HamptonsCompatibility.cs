using System;
using System.Reflection;
using Buildings;
using Helpers;
using Streets;
using UnityEngine;
using UnityEngine.AI;

namespace VoogleRoute.Navigation
{
    /// <summary>
    /// Keeps every compile-time reference to 1.0-only Hamptons game types out
    /// of code paths that Mono may JIT on Big Ambitions 0.11.
    /// </summary>
    internal static class HamptonsCompatibility
    {
        private const BindingFlags StaticFlags =
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly bool GameSupportsHamptons =
            typeof(BuildingManager).Assembly.GetType("HamptonsHouse", throwOnError: false) != null;
        private static readonly Type ResolverType = GameSupportsHamptons
            ? typeof(HamptonsCompatibility).Assembly.GetType(
                "VoogleRoute.Navigation.HamptonsExitResolver",
                throwOnError: false)
            : null;
        private static readonly MethodInfo GetCurrentHouseIdMethod =
            ResolverType?.GetMethod("TryGetCurrentHouseId", StaticFlags);
        private static readonly MethodInfo CalculateCurrentRouteMethod =
            ResolverType?.GetMethod("TryCalculateCurrentRoute", StaticFlags);
        private static readonly MethodInfo CompleteBoundaryHandoffMethod =
            ResolverType?.GetMethod("TryCompleteBoundaryHandoff", StaticFlags);
        private static readonly MethodInfo EnsureRouteOriginMethod =
            ResolverType?.GetMethod("TryEnsurePlayerAgentOnRouteOrigin", StaticFlags);
        private static readonly MethodInfo InvalidateCacheMethod =
            ResolverType?.GetMethod("InvalidateCache", StaticFlags);

        internal static bool TryGetCurrentHouseId(out int houseId)
        {
            houseId = 0;
            if (GetCurrentHouseIdMethod == null)
                return false;

            try
            {
                var arguments = new object[] { 0 };
                var found = GetCurrentHouseIdMethod.Invoke(null, arguments) is true;
                if (found && arguments[0] is int resolvedId)
                    houseId = resolvedId;
                return found && houseId != 0;
            }
            catch
            {
                houseId = 0;
                return false;
            }
        }

        /// <summary>
        /// The game clears BuildingManager's active registration before invoking
        /// onExitBuilding for a Hamptons house. Resolve it again from the address
        /// without introducing a direct reference to the 1.0-only house type.
        /// </summary>
        internal static bool TryGetHouseRegistration(
            Address address,
            out BuildingRegistration registration)
        {
            registration = null;
            if (!GameSupportsHamptons || address == null)
                return false;

            try
            {
                registration = BuildingHelper.GetBuildingRegistration(address);
                return registration?.BuildingCached?.BuildingSize == "ba:buildingsize_t";
            }
            catch
            {
                registration = null;
                return false;
            }
        }

        internal static bool TryCalculateCurrentRoute(
            Vector3 origin,
            NavMeshPath path,
            out IndoorExitTarget exit)
        {
            exit = IndoorExitTarget.None;
            if (CalculateCurrentRouteMethod == null)
                return false;

            try
            {
                var arguments = new object[] { origin, path, IndoorExitTarget.None };
                var calculated = CalculateCurrentRouteMethod.Invoke(null, arguments) is true;
                if (calculated && arguments[2] is IndoorExitTarget resolvedExit)
                    exit = resolvedExit;
                return calculated && exit.IsValid;
            }
            catch
            {
                exit = IndoorExitTarget.None;
                return false;
            }
        }

        internal static bool TryCompleteBoundaryHandoff(IndoorExitTarget exit) =>
            InvokeBoolean(CompleteBoundaryHandoffMethod, exit);

        internal static bool TryEnsurePlayerAgentOnRouteOrigin() =>
            InvokeBoolean(EnsureRouteOriginMethod);

        internal static void InvalidateCache()
        {
            try
            {
                InvalidateCacheMethod?.Invoke(null, null);
            }
            catch
            {
                // Hamptons support is optional on legacy game versions.
            }
        }

        private static bool InvokeBoolean(MethodInfo method, params object[] arguments)
        {
            if (method == null)
                return false;

            try
            {
                return method.Invoke(null, arguments) is true;
            }
            catch
            {
                return false;
            }
        }
    }
}
