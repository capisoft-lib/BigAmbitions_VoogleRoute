using System;
using System.Collections.Generic;
using System.Reflection;
using Helpers;
using UnityEngine;
using UnityEngine.AI;
using VoogleRoute.Pathfinding.Geometry;

namespace VoogleRoute.Navigation
{
    /// <summary>
    /// Resolves a reachable ground-level point just outside a Hamptons plot.
    /// Those houses are open-world buildings and intentionally have no
    /// conventional ExitZone.
    /// </summary>
    internal static class HamptonsExitResolver
    {
        private const float BoundarySampleSpacing = 4f;
        private const float BoundaryCornerMargin = 2f;
        private const float OutsideOffset = 0.9f;
        private const float TargetSampleRadius = 1f;
        private const float UpperFloorTargetSampleRadius = 4.25f;
        private const float EntranceForwardOffset = 1.5f;
        private const float MaxEntranceBoundaryDistance = 8f;
        private const float OriginSampleRadius = 1.75f;
        private const float MaxOriginVerticalSnap = 1.25f;
        private const float MaxAgentRepairDistance = 2f;
        private const float ExteriorTargetSampleRadius = 2.5f;
        private const float ActualOutsideStep = 0.5f;
        private const float ActualOutsideClearance = 0.75f;
        private const float MaxActualOutsideSearch = 16f;
        private const float ScanOriginResetDistance = 3f;
        private const float FailedScanRetrySeconds = 3f;
        private const float FailureRepeatSeconds = 30f;
        private const int MaxCandidateChecksPerTick = 4;
        private const int ExteriorAgentTypeId = 1479372276;
        private const int FixedFenceGatePriority = 0;
        private const int RegisteredEntrancePriority = 1;
        private const int PerimeterPriority = 2;

        private static readonly List<BoundaryCandidate> Candidates = new List<BoundaryCandidate>(96);
        private static readonly FieldInfo PlotVolumeField = typeof(HamptonsHouse).GetField(
            "plotVolume",
            BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo VolumeCollidersField = typeof(PlayerVolumeFormedByMultipleColliders).GetField(
            "volumeBoxColliders",
            BindingFlags.Instance | BindingFlags.NonPublic);

        private static int _cachedHouseId;
        private static Vector3 _cachedTarget;
        private static Vector3 _cachedOutsideTarget;
        private static bool _cachedTargetValid;
        private static Vector3 _activeNavOrigin;
        private static bool _activeNavOriginValid;
        private static int _scanHouseId;
        private static Vector3 _scanOrigin;
        private static int _scanIndex;
        private static int _scanSampledCount;
        private static int _scanCompleteCount;
        private static int _scanPartialCount;
        private static float _scanBestLength = float.MaxValue;
        private static Vector3 _scanBestTarget;
        private static Vector3 _scanBestOutsideTarget;
        private static int _scanBestPriority = int.MaxValue;
        private static bool _scanInProgress;
        private static float _nextScanAllowedTime = -999f;
        private static int _lastFailureHouseId;
        private static string _lastFailureReason;
        private static float _lastFailureTime = -999f;

        internal static bool TryGetCurrentHouse(out HamptonsHouse house)
        {
            house = null;
            try
            {
                if (!BuildingManager.IsInitialized || !BuildingManager.IsInsideBuilding)
                    return false;

                house = BuildingManager.Instance?.multipleHeightsBuildingController as HamptonsHouse;
                return house != null && house.plotBounds != null;
            }
            catch
            {
                house = null;
                return false;
            }
        }

        internal static bool TryGetCurrentHouseId(out int houseId)
        {
            houseId = 0;
            if (!TryGetCurrentHouse(out var house) || house == null)
                return false;

            houseId = house.GetInstanceID();
            return houseId != 0;
        }

        internal static bool TryCalculateCurrentRoute(
            Vector3 origin,
            NavMeshPath path,
            out IndoorExitTarget exit)
        {
            exit = IndoorExitTarget.None;
            return TryGetCurrentHouse(out var house) &&
                   TryCalculateRoute(house, origin, path, out exit);
        }

        internal static bool TryCalculateRoute(
            HamptonsHouse house,
            Vector3 origin,
            NavMeshPath navPath,
            out IndoorExitTarget exit)
        {
            exit = IndoorExitTarget.None;
            if (house == null || house.plotBounds == null || navPath == null)
                return Fail(house, "invalid_house_or_plot_bounds", 0, 0, 0);

            var houseId = house.GetInstanceID();
            if (_cachedHouseId != houseId)
            {
                _cachedHouseId = houseId;
                _cachedTarget = default;
                _cachedOutsideTarget = default;
                _cachedTargetValid = false;
                _activeNavOrigin = default;
                _activeNavOriginValid = false;
                ResetScan();
            }

            if (!TryGetFilterAndAgent(out var filter, out var agent))
                return Fail(house, "player_agent_unavailable", 0, 0, 0);

            if (!TrySampleOrigin(origin, filter, agent, out var navOrigin))
                return Fail(house, "origin_not_on_current_floor_navmesh", 0, 0, 0);

            _activeNavOrigin = navOrigin;
            _activeNavOriginValid = true;

            if (_cachedTargetValid &&
                TryCalculateUsablePath(
                    navOrigin,
                    _cachedTarget,
                    _cachedOutsideTarget,
                    house,
                    filter,
                    navPath,
                    out _))
            {
                exit = BuildExit(navPath.corners[^1], _cachedOutsideTarget);
                return true;
            }

            _cachedTargetValid = false;
            if (!_scanInProgress)
            {
                if (Time.unscaledTime < _nextScanAllowedTime)
                    return false;

                StartScan(house, navOrigin);
            }
            else if (_scanHouseId != houseId ||
                     HorizontalDistance(_scanOrigin, navOrigin) > ScanOriginResetDistance)
            {
                StartScan(house, navOrigin);
            }

            var checks = 0;
            while (_scanIndex < Candidates.Count && checks < MaxCandidateChecksPerTick)
            {
                var candidate = Candidates[_scanIndex++];
                checks++;

                if (_scanBestLength < float.MaxValue && candidate.Priority > _scanBestPriority)
                {
                    _scanIndex = Candidates.Count;
                    break;
                }

                var desiredOutside = GetOutsidePoint(house, candidate.WorldPosition);
                if (!TrySampleTarget(desiredOutside, filter, out var hit))
                    continue;

                _scanSampledCount++;
                if (HorizontalDistance(hit.position, desiredOutside) >
                    HamptonsBoundaryPathPolicy.MaxPartialEndpointToTarget)
                    continue;

                if (!TryCalculateUsablePath(
                        navOrigin,
                        hit.position,
                        desiredOutside,
                        house,
                        filter,
                        navPath,
                        out var isPartial))
                    continue;

                if (isPartial)
                    _scanPartialCount++;
                else
                    _scanCompleteCount++;
                var length = PolylineLength(navPath.corners);
                if (candidate.Priority > _scanBestPriority ||
                    (candidate.Priority == _scanBestPriority && length >= _scanBestLength))
                    continue;

                _scanBestPriority = candidate.Priority;
                _scanBestLength = length;
                _scanBestTarget = hit.position;
                _scanBestOutsideTarget = desiredOutside;
            }

            if (_scanIndex < Candidates.Count)
                return false;

            _scanInProgress = false;
            var candidateCount = Candidates.Count;
            var sampledCount = _scanSampledCount;
            var completeCount = _scanCompleteCount;
            var partialCount = _scanPartialCount;
            var bestLength = _scanBestLength;
            var bestTarget = _scanBestTarget;
            var bestOutsideTarget = _scanBestOutsideTarget;

            if (bestLength == float.MaxValue)
            {
                _nextScanAllowedTime = Time.unscaledTime + FailedScanRetrySeconds;
                return Fail(
                    house,
                    "no_usable_path_to_plot_boundary",
                    candidateCount,
                    sampledCount,
                    completeCount,
                    partialCount);
            }

            if (!TryCalculateUsablePath(
                    navOrigin,
                    bestTarget,
                    bestOutsideTarget,
                    house,
                    filter,
                    navPath,
                    out var selectedIsPartial))
            {
                _nextScanAllowedTime = Time.unscaledTime + FailedScanRetrySeconds;
                return Fail(
                    house,
                    "selected_boundary_path_became_invalid",
                    candidateCount,
                    sampledCount,
                    completeCount,
                    partialCount);
            }

            _cachedTarget = bestTarget;
            _cachedOutsideTarget = bestOutsideTarget;
            _cachedTargetValid = true;
            _nextScanAllowedTime = -999f;
            exit = BuildExit(navPath.corners[^1], bestOutsideTarget);

            ModLog.Info(
                "Hamptons exit resolved" +
                " | house=" + HouseName(house) +
                " | agent=" + filter.agentTypeID +
                " | agentOnMesh=" + IsAgentOnNavMesh(agent) +
                " | origin=" + Format(navOrigin) +
                " | target=" + Format(bestTarget) +
                " | outside=" + Format(bestOutsideTarget) +
                " | endpoint=" + Format(navPath.corners[^1]) +
                " | priority=" + _scanBestPriority +
                " | candidates=" + candidateCount +
                " | sampled=" + sampledCount +
                " | complete=" + completeCount +
                " | partial=" + partialCount +
                " | selectedPartial=" + selectedIsPartial +
                " | length=" + bestLength.ToString("F1"));
            return true;
        }

        /// <summary>
        /// Bridges the small seam between the plot's indoor agent (0) and the
        /// city's pedestrian agent. The native Hamptons LateUpdate then owns
        /// OnExitPlot and ExitFromBuilding, preserving every vanilla cleanup.
        /// </summary>
        internal static bool TryCompleteBoundaryHandoff(in IndoorExitTarget exit)
        {
            if (!exit.IsHamptonsPlotExit ||
                exit.HamptonsOutsidePosition.sqrMagnitude <= 0.01f ||
                !TryGetCurrentHouse(out var house))
                return false;

            var player = PlayerHelper.PlayerController;
            var agent = player?.Character?.navmeshAgent;
            if (player == null || agent == null)
                return Fail(house, "boundary_handoff_agent_unavailable", 0, 0, 0);

            var exteriorFilter = PlayerController.navMeshQueryFilter;
            exteriorFilter.agentTypeID = ExteriorAgentTypeId;
            exteriorFilter.areaMask = NavMesh.AllAreas;
            if (!TrySampleExteriorTarget(
                    house,
                    exit.HamptonsOutsidePosition,
                    exteriorFilter,
                    out var exteriorHit))
                return Fail(house, "exterior_handoff_target_not_on_navmesh", 0, 0, 0);

            if (IsInsidePlotVolumeHorizontally(house, exteriorHit.position))
                return Fail(house, "exterior_handoff_target_inside_plot", 0, 0, 0);

            var originalAgentType = agent.agentTypeID;
            var wasEnabled = agent.enabled;
            try
            {
                player.ResetNavigation();
                if (wasEnabled)
                    agent.enabled = false;

                PlayerController.SetNavAgentTypeId(ExteriorAgentTypeId);
                agent.transform.position = exteriorHit.position;
                agent.enabled = true;
                if (!agent.isOnNavMesh)
                    throw new InvalidOperationException("Exterior agent did not bind to its NavMesh.");

                ModLog.Info(
                    "Hamptons boundary handoff completed" +
                    " | house=" + HouseName(house) +
                    " | from=" + Format(exit.WalkPosition) +
                    " | to=" + Format(exteriorHit.position) +
                    " | agent=" + ExteriorAgentTypeId);
                return true;
            }
            catch
            {
                try
                {
                    if (agent.enabled)
                        agent.enabled = false;
                    PlayerController.SetNavAgentTypeId(originalAgentType);
                    agent.enabled = wasEnabled;
                }
                catch
                {
                    // The failure is logged below; vanilla recovery can still
                    // rebuild the player agent on the following frame.
                }

                return Fail(house, "exterior_handoff_failed", 0, 0, 0);
            }
        }

        internal static bool TryEnsurePlayerAgentOnRouteOrigin()
        {
            if (!_activeNavOriginValid || !TryGetCurrentHouse(out var house))
                return false;

            if (!TryGetFilterAndAgent(out _, out var agent) || agent == null || !agent.enabled)
                return false;

            if (IsAgentOnNavMesh(agent))
                return true;

            var delta = agent.transform.position - _activeNavOrigin;
            if (delta.sqrMagnitude > MaxAgentRepairDistance * MaxAgentRepairDistance ||
                Mathf.Abs(delta.y) > MaxOriginVerticalSnap)
            {
                Fail(house, "agent_repair_target_too_far", 0, 0, 0);
                return false;
            }

            try
            {
                if (!agent.Warp(_activeNavOrigin) || !agent.isOnNavMesh)
                {
                    Fail(house, "agent_warp_to_current_floor_failed", 0, 0, 0);
                    return false;
                }

                ModLog.Info(
                    "Hamptons player agent restored to NavMesh" +
                    " | house=" + HouseName(house) +
                    " | position=" + Format(_activeNavOrigin) +
                    " | agent=" + agent.agentTypeID);
                return true;
            }
            catch
            {
                Fail(house, "agent_warp_exception", 0, 0, 0);
                return false;
            }
        }

        internal static void InvalidateCache()
        {
            _cachedHouseId = 0;
            _cachedTarget = default;
            _cachedOutsideTarget = default;
            _cachedTargetValid = false;
            _activeNavOrigin = default;
            _activeNavOriginValid = false;
            ResetScan();
        }

        private static IndoorExitTarget BuildExit(
            Vector3 reachableEndpoint,
            Vector3 outsideTarget) =>
            new IndoorExitTarget(
                reachableEndpoint,
                0,
                false,
                false,
                isHamptonsPlotExit: true,
                hamptonsOutsidePosition: outsideTarget);

        private static bool TryGetFilterAndAgent(out NavMeshQueryFilter filter, out NavMeshAgent agent)
        {
            filter = PlayerController.navMeshQueryFilter;
            agent = null;
            try
            {
                agent = PlayerHelper.PlayerController?.Character?.navmeshAgent;
                if (agent == null)
                    return false;

                filter.agentTypeID = agent.agentTypeID;
                filter.areaMask = NavMesh.AllAreas;
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool TrySampleOrigin(
            Vector3 origin,
            NavMeshQueryFilter filter,
            NavMeshAgent agent,
            out Vector3 sampled)
        {
            sampled = origin;
            if (agent != null && agent.enabled && IsAgentOnNavMesh(agent))
            {
                sampled = agent.transform.position;
                return true;
            }

            if (!NavMesh.SamplePosition(origin, out var hit, OriginSampleRadius, filter))
                return false;

            if (Mathf.Abs(hit.position.y - origin.y) > MaxOriginVerticalSnap)
                return false;

            sampled = hit.position;
            return true;
        }

        private static void StartScan(HamptonsHouse house, Vector3 origin)
        {
            BuildCandidates(house, origin);
            _scanHouseId = house.GetInstanceID();
            _scanOrigin = origin;
            _scanIndex = 0;
            _scanSampledCount = 0;
            _scanCompleteCount = 0;
            _scanPartialCount = 0;
            _scanBestLength = float.MaxValue;
            _scanBestTarget = default;
            _scanBestOutsideTarget = default;
            _scanBestPriority = int.MaxValue;
            _scanInProgress = true;
        }

        private static void ResetScan()
        {
            Candidates.Clear();
            _scanHouseId = 0;
            _scanOrigin = default;
            _scanIndex = 0;
            _scanSampledCount = 0;
            _scanCompleteCount = 0;
            _scanPartialCount = 0;
            _scanBestLength = float.MaxValue;
            _scanBestTarget = default;
            _scanBestOutsideTarget = default;
            _scanBestPriority = int.MaxValue;
            _scanInProgress = false;
            _nextScanAllowedTime = -999f;
        }

        private static void BuildCandidates(HamptonsHouse house, Vector3 origin)
        {
            Candidates.Clear();
            var plotBounds = house.plotBounds;
            AddFixedFenceGateCandidates(house, origin);
            AddRegisteredEntranceCandidates(origin);

            var halfX = Mathf.Abs(plotBounds.size.x) * 0.5f;
            var halfZ = Mathf.Abs(plotBounds.size.z) * 0.5f;
            var usableX = Mathf.Max(0f, halfX - BoundaryCornerMargin);
            var usableZ = Mathf.Max(0f, halfZ - BoundaryCornerMargin);
            var localOriginY = plotBounds.transform.InverseTransformPoint(origin).y;

            AddFaceSamples(plotBounds.transform, origin, true, -halfZ - OutsideOffset, usableX, localOriginY);
            AddFaceSamples(plotBounds.transform, origin, true, halfZ + OutsideOffset, usableX, localOriginY);
            AddFaceSamples(plotBounds.transform, origin, false, -halfX - OutsideOffset, usableZ, localOriginY);
            AddFaceSamples(plotBounds.transform, origin, false, halfX + OutsideOffset, usableZ, localOriginY);
            Candidates.Sort((left, right) =>
            {
                if (left.Priority != right.Priority)
                    return left.Priority.CompareTo(right.Priority);

                return left.StraightDistance.CompareTo(right.StraightDistance);
            });
        }

        private static void AddFixedFenceGateCandidates(HamptonsHouse house, Vector3 origin)
        {
            try
            {
                var gates = house.GetComponentsInChildren<FenceDoor>(includeInactive: true);
                foreach (var gate in gates)
                {
                    if (gate == null || gate.itemController != null)
                        continue;

                    var gateTransform = gate.transform;
                    AddCandidate(gateTransform.position, origin, FixedFenceGatePriority);
                    AddCandidate(
                        gateTransform.position + gateTransform.forward * EntranceForwardOffset,
                        origin,
                        FixedFenceGatePriority);
                    AddCandidate(
                        gateTransform.position - gateTransform.forward * EntranceForwardOffset,
                        origin,
                        FixedFenceGatePriority);
                }
            }
            catch
            {
                // Registered entrances and perimeter samples remain available.
            }
        }

        private static void AddRegisteredEntranceCandidates(Vector3 origin)
        {
            try
            {
                var doors = BuildingManager.Instance?.cityBuildingController?.entranceDoors;
                if (doors == null)
                    return;

                foreach (var door in doors)
                {
                    var doorTransform = door?.doorTransform;
                    if (doorTransform == null)
                        continue;

                    AddCandidate(doorTransform.position, origin, RegisteredEntrancePriority);
                    AddCandidate(
                        doorTransform.position + doorTransform.forward * EntranceForwardOffset,
                        origin,
                        RegisteredEntrancePriority);
                    AddCandidate(
                        doorTransform.position - doorTransform.forward * EntranceForwardOffset,
                        origin,
                        RegisteredEntrancePriority);
                }
            }
            catch
            {
                // The perimeter scan remains available while the house loads.
            }
        }

        private static void AddFaceSamples(
            Transform plotTransform,
            Vector3 origin,
            bool varyX,
            float fixedCoordinate,
            float halfSpan,
            float localY)
        {
            var sampleCount = Mathf.Max(1, Mathf.CeilToInt(halfSpan * 2f / BoundarySampleSpacing));
            for (var index = 0; index <= sampleCount; index++)
            {
                var variable = Mathf.Lerp(-halfSpan, halfSpan, index / (float)sampleCount);
                var local = varyX
                    ? new Vector3(variable, localY, fixedCoordinate)
                    : new Vector3(fixedCoordinate, localY, variable);
                var world = plotTransform.TransformPoint(local);
                AddCandidate(world, origin, PerimeterPriority);
            }
        }

        private static void AddCandidate(Vector3 world, Vector3 origin, int priority) =>
            Candidates.Add(new BoundaryCandidate(
                world,
                HorizontalDistance(origin, world),
                priority));

        private static bool TrySampleTarget(
            Vector3 target,
            NavMeshQueryFilter filter,
            out NavMeshHit hit)
        {
            if (NavMesh.SamplePosition(target, out hit, TargetSampleRadius, filter))
                return true;

            // The route may start upstairs, while the exterior NavMesh is at
            // ground level. A second vertical reach covers every extracted
            // Hamptons floor height (3.5 m) without changing the target X/Z.
            return NavMesh.SamplePosition(target, out hit, UpperFloorTargetSampleRadius, filter);
        }

        private static bool TrySampleExteriorTarget(
            HamptonsHouse house,
            Vector3 target,
            NavMeshQueryFilter filter,
            out NavMeshHit hit)
        {
            var center = house.plotBounds.transform.TransformPoint(Vector3.zero);
            var direction = target - center;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.01f)
                direction = house.transform.forward;
            direction.Normalize();

            for (var distance = 0f; distance <= 6f; distance += ActualOutsideStep)
            {
                var probe = target + direction * distance;
                probe.y = house.transform.position.y;
                if (!NavMesh.SamplePosition(probe, out hit, ExteriorTargetSampleRadius, filter))
                    continue;

                if (!IsInsidePlotVolumeHorizontally(house, hit.position))
                    return true;
            }

            hit = default;
            return false;
        }

        private static bool IsOutsidePlotHorizontally(SizeOrientedBounds plotBounds, Vector3 position)
        {
            var local = plotBounds.transform.InverseTransformPoint(position);
            var halfX = Mathf.Abs(plotBounds.size.x) * 0.5f;
            var halfZ = Mathf.Abs(plotBounds.size.z) * 0.5f;
            return Mathf.Abs(local.x) > halfX || Mathf.Abs(local.z) > halfZ;
        }

        private static bool IsNearPlotBoundaryHorizontally(SizeOrientedBounds plotBounds, Vector3 position)
        {
            var local = plotBounds.transform.InverseTransformPoint(position);
            var halfX = Mathf.Abs(plotBounds.size.x) * 0.5f;
            var halfZ = Mathf.Abs(plotBounds.size.z) * 0.5f;
            var distanceToBoundary = Mathf.Min(
                Mathf.Abs(halfX - Mathf.Abs(local.x)),
                Mathf.Abs(halfZ - Mathf.Abs(local.z)));
            return distanceToBoundary <= MaxEntranceBoundaryDistance;
        }

        private static bool TryCalculateUsablePath(
            Vector3 origin,
            Vector3 target,
            Vector3 desiredOutside,
            HamptonsHouse house,
            NavMeshQueryFilter filter,
            NavMeshPath path,
            out bool isPartial)
        {
            isPartial = false;
            try
            {
                if (!NavMesh.CalculatePath(origin, target, filter, path) ||
                    path.corners is not { Length: >= 2 } corners)
                    return false;

                var status = path.status switch
                {
                    NavMeshPathStatus.PathComplete => HamptonsBoundaryPathStatus.Complete,
                    NavMeshPathStatus.PathPartial => HamptonsBoundaryPathStatus.Partial,
                    _ => HamptonsBoundaryPathStatus.Invalid
                };
                isPartial = status == HamptonsBoundaryPathStatus.Partial;
                return HamptonsBoundaryPathPolicy.IsUsable(
                    status,
                    corners.Length,
                    HorizontalDistance(corners[^1], desiredOutside),
                    DistanceToBoundaryHorizontally(house, corners[^1]));
            }
            catch
            {
                return false;
            }
        }

        private static Vector3 GetOutsidePoint(HamptonsHouse house, Vector3 reference)
        {
            var plotBounds = house.plotBounds;
            var local = plotBounds.transform.InverseTransformPoint(reference);
            var halfX = Mathf.Abs(plotBounds.size.x) * 0.5f;
            var halfZ = Mathf.Abs(plotBounds.size.z) * 0.5f;
            var xDistance = Mathf.Abs(halfX - Mathf.Abs(local.x));
            var zDistance = Mathf.Abs(halfZ - Mathf.Abs(local.z));

            Vector3 fallbackLocal;
            if (xDistance < zDistance)
                fallbackLocal = new Vector3(
                    (local.x < 0f ? -1f : 1f) * (halfX + OutsideOffset),
                    local.y,
                    local.z);
            else
                fallbackLocal = new Vector3(
                    local.x,
                    local.y,
                    (local.z < 0f ? -1f : 1f) * (halfZ + OutsideOffset));

            var fallback = plotBounds.transform.TransformPoint(fallbackLocal);
            if (!TryGetPlotVolumeColliders(house, out _))
                return fallback;

            var center = plotBounds.transform.TransformPoint(Vector3.zero);
            var direction = reference - center;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.01f)
            {
                direction = fallback - center;
                direction.y = 0f;
            }
            direction.Normalize();

            var start = reference;
            for (var distance = 0f; distance <= MaxActualOutsideSearch; distance += ActualOutsideStep)
            {
                var point = start + direction * distance;
                if (!IsInsidePlotVolumeHorizontally(house, point))
                    return point + direction * ActualOutsideClearance;
            }

            return fallback;
        }

        private static float DistanceToBoundaryHorizontally(
            HamptonsHouse house,
            Vector3 position)
        {
            if (TryGetPlotVolumeColliders(house, out var colliders))
            {
                var best = float.MaxValue;
                foreach (var collider in colliders)
                {
                    if (collider == null)
                        continue;

                    var colliderLocal = collider.transform.InverseTransformPoint(position) - collider.center;
                    var half = collider.size * 0.5f;
                    var outsideX = Mathf.Max(Mathf.Abs(colliderLocal.x) - half.x, 0f);
                    var outsideZ = Mathf.Max(Mathf.Abs(colliderLocal.z) - half.z, 0f);
                    var distance = outsideX > 0f || outsideZ > 0f
                        ? Mathf.Sqrt(outsideX * outsideX + outsideZ * outsideZ)
                        : Mathf.Min(
                            half.x - Mathf.Abs(colliderLocal.x),
                            half.z - Mathf.Abs(colliderLocal.z));
                    best = Mathf.Min(best, distance);
                }

                if (best < float.MaxValue)
                    return best;
            }

            var plotBounds = house.plotBounds;
            var local = plotBounds.transform.InverseTransformPoint(position);
            var halfX = Mathf.Abs(plotBounds.size.x) * 0.5f;
            var halfZ = Mathf.Abs(plotBounds.size.z) * 0.5f;
            return Mathf.Min(
                Mathf.Abs(halfX - Mathf.Abs(local.x)),
                Mathf.Abs(halfZ - Mathf.Abs(local.z)));
        }

        private static bool IsInsidePlotVolumeHorizontally(HamptonsHouse house, Vector3 position)
        {
            if (!TryGetPlotVolumeColliders(house, out var colliders))
                return IsInsidePlotBoundsHorizontally(house.plotBounds, position);

            foreach (var collider in colliders)
            {
                if (collider == null)
                    continue;

                var local = collider.transform.InverseTransformPoint(position) - collider.center;
                var half = collider.size * 0.5f;
                if (Mathf.Abs(local.x) <= half.x && Mathf.Abs(local.z) <= half.z)
                    return true;
            }

            return false;
        }

        private static bool TryGetPlotVolumeColliders(
            HamptonsHouse house,
            out BoxCollider[] colliders)
        {
            colliders = null;
            try
            {
                var volume = PlotVolumeField?.GetValue(house) as PlayerVolumeFormedByMultipleColliders;
                colliders = VolumeCollidersField?.GetValue(volume) as BoxCollider[];
                return colliders is { Length: > 0 };
            }
            catch
            {
                colliders = null;
                return false;
            }
        }

        private static bool IsInsidePlotBoundsHorizontally(
            SizeOrientedBounds plotBounds,
            Vector3 position)
        {
            var local = plotBounds.transform.InverseTransformPoint(position);
            var halfX = Mathf.Abs(plotBounds.size.x) * 0.5f;
            var halfZ = Mathf.Abs(plotBounds.size.z) * 0.5f;
            return Mathf.Abs(local.x) <= halfX && Mathf.Abs(local.z) <= halfZ;
        }

        private static float PolylineLength(Vector3[] points)
        {
            var length = 0f;
            for (var index = 1; index < points.Length; index++)
                length += Vector3.Distance(points[index - 1], points[index]);
            return length;
        }

        private static float HorizontalDistance(Vector3 left, Vector3 right)
        {
            left.y = 0f;
            right.y = 0f;
            return Vector3.Distance(left, right);
        }

        private static bool Fail(
            HamptonsHouse house,
            string reason,
            int candidates,
            int sampled,
            int complete,
            int partial = 0)
        {
            var houseId = house != null ? house.GetInstanceID() : 0;
            var now = Time.unscaledTime;
            if (houseId != _lastFailureHouseId ||
                !string.Equals(reason, _lastFailureReason, StringComparison.Ordinal) ||
                now - _lastFailureTime >= FailureRepeatSeconds)
            {
                _lastFailureHouseId = houseId;
                _lastFailureReason = reason;
                _lastFailureTime = now;

                var agent = PlayerHelper.PlayerController?.Character?.navmeshAgent;
                ModLog.Error(
                    "Hamptons exit route unavailable" +
                    " | reason=" + reason +
                    " | house=" + HouseName(house) +
                    " | agent=" + (agent != null ? agent.agentTypeID.ToString() : "none") +
                    " | agentEnabled=" + (agent != null && agent.enabled) +
                    " | agentOnMesh=" + IsAgentOnNavMesh(agent) +
                    " | player=" + (agent != null ? Format(agent.transform.position) : "none") +
                    " | candidates=" + candidates +
                    " | sampled=" + sampled +
                    " | complete=" + complete +
                    " | partial=" + partial);
            }

            return false;
        }

        private static bool IsAgentOnNavMesh(NavMeshAgent agent)
        {
            try
            {
                return agent != null && agent.enabled && agent.isOnNavMesh;
            }
            catch
            {
                return false;
            }
        }

        private static string HouseName(HamptonsHouse house) =>
            house != null && house.gameObject != null ? house.gameObject.name : "none";

        private static string Format(Vector3 value) =>
            "(" + value.x.ToString("F2") + "," + value.y.ToString("F2") + "," + value.z.ToString("F2") + ")";

        private readonly struct BoundaryCandidate
        {
            internal BoundaryCandidate(
                Vector3 worldPosition,
                float straightDistance,
                int priority)
            {
                WorldPosition = worldPosition;
                StraightDistance = straightDistance;
                Priority = priority;
            }

            internal Vector3 WorldPosition { get; }
            internal float StraightDistance { get; }
            internal int Priority { get; }
        }
    }
}
