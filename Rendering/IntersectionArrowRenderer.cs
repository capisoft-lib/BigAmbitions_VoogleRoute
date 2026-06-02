using VoogleRoute.Navigation;
using UnityEngine;
using Object = UnityEngine.Object;

namespace VoogleRoute.Rendering;

/// <summary>
/// Flèches au sol aux prochains croisements (coins NavMesh significatifs).
/// </summary>
public static class IntersectionArrowRenderer
{
    private const string RootName = "VoogleRoute_IntersectionArrows";
    private static Transform? _root;
    private static readonly List<ArrowHandle> Pool = new();

    private sealed class ArrowHandle
    {
        public GameObject GameObject = null!;
        public LineRenderer Line = null!;
        public bool Active;
    }

    public static void EnsureCreated()
    {
        if (_root != null)
            return;
        var go = GameObject.Find(RootName) ?? new GameObject(RootName);
        Object.DontDestroyOnLoad(go);
        _root = go.transform;
    }

    public static void Update(TurnGuidanceState state, Vector3 playerPosition, bool visible)
    {
        EnsureCreated();
        if (_root == null)
            return;

        var show = visible && state.HasGuidance && ModConfig.ShowIntersectionArrows.Value;
        if (!show)
        {
            HideAll();
            return;
        }

        var maxRange = ModConfig.IntersectionMarkerRangeMeters.Value;
        var needed = 0;
        foreach (var turn in state.UpcomingTurns)
        {
            if (turn.Kind is TurnKind.Straight or TurnKind.Arrival)
                continue;
            if (turn.DistanceAlongPath > maxRange)
                continue;
            if ((turn.Position - playerPosition).sqrMagnitude > maxRange * maxRange)
                continue;

            EnsurePoolSize(needed + 1);
            var handle = Pool[needed];
            PlaceArrow(handle, turn);
            handle.Active = true;
            needed++;
        }

        for (var i = needed; i < Pool.Count; i++)
        {
            Pool[i].Active = false;
            Pool[i].GameObject.SetActive(false);
        }
    }

    public static void ApplyStyle()
    {
        foreach (var handle in Pool)
        {
            if (!handle.Active)
                continue;

            ApplyArrowColor(handle.Line);
        }
    }

    public static void Destroy()
    {
        if (_root != null)
        {
            Object.Destroy(_root.gameObject);
            _root = null;
        }
        Pool.Clear();
    }

    private static void HideAll()
    {
        foreach (var h in Pool)
        {
            h.Active = false;
            h.GameObject.SetActive(false);
        }
    }

    private static void EnsurePoolSize(int count)
    {
        while (Pool.Count < count)
        {
            var go = new GameObject($"Arrow_{Pool.Count}");
            go.transform.SetParent(_root, false);
            var line = go.AddComponent<LineRenderer>();
            ConfigureArrowLine(line);
            Pool.Add(new ArrowHandle { GameObject = go, Line = line });
        }
    }

    private static void PlaceArrow(ArrowHandle handle, TurnPoint turn)
    {
        handle.GameObject.SetActive(true);
        var pos = turn.Position + Vector3.up * ModConfig.GroundOffset.Value;

        var outgoing = GetOutgoingDirection(turn);
        if (outgoing.sqrMagnitude < 0.01f)
            outgoing = Vector3.forward;

        outgoing.Normalize();
        var right = Vector3.Cross(Vector3.up, outgoing).normalized;

        var length = ModConfig.IntersectionArrowLength.Value;
        var wingSpread = length * 0.22f;
        var tip = pos + outgoing * length;
        var wingL = pos + outgoing * (length * 0.55f) - right * wingSpread;
        var wingR = pos + outgoing * (length * 0.55f) + right * wingSpread;

        handle.Line.positionCount = 4;
        handle.Line.SetPosition(0, wingL);
        handle.Line.SetPosition(1, tip);
        handle.Line.SetPosition(2, wingR);
        handle.Line.SetPosition(3, pos);

        ApplyArrowColor(handle.Line);
    }

    private static Vector3 GetOutgoingDirection(TurnPoint turn)
    {
        var corners = PathFinderService.LastTurnCorners;
        if (turn.CornerIndex < corners.Length - 1 && turn.CornerIndex >= 0)
            return Flat(corners[turn.CornerIndex + 1] - corners[turn.CornerIndex]);
        if (NavigationTargetTracker.HasTarget)
            return Flat(NavigationTargetTracker.ActiveTarget - turn.Position);
        return Vector3.forward;
    }

    private static Vector3 Flat(Vector3 v) => new(v.x, 0f, v.z);

    private static void ConfigureArrowLine(LineRenderer line)
    {
        line.useWorldSpace = true;
        line.alignment = LineAlignment.View;
        line.loop = false;
        var w = ModConfig.IntersectionArrowWidth.Value;
        line.startWidth = w;
        line.endWidth = w;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
        LineRendererMaterial.Apply(line);
    }

    private static void ApplyArrowColor(LineRenderer line) =>
        LineColorHelper.Apply(line);
}
