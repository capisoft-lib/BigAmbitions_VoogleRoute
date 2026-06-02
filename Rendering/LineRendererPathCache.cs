using UnityEngine;

namespace VoogleRoute.Rendering;

/// <summary>Évite de réécrire le LineRenderer si le chemin affiché est inchangé.</summary>
internal static class LineRendererPathCache
{
    private static Vector3[]? _lastReference;
    private static int _count;
    private static uint _hash;

    internal static bool IsSame(Vector3[] points)
    {
        if (points.Length < 2)
            return _lastReference == null || _lastReference.Length == 0;

        if (ReferenceEquals(points, _lastReference))
            return true;

        var hash = ComputeHash(points);
        if (points.Length == _count && hash == _hash)
        {
            _lastReference = points;
            return true;
        }

        _lastReference = points;
        _count = points.Length;
        _hash = hash;
        return false;
    }

    internal static void Reset()
    {
        _lastReference = null;
        _count = 0;
        _hash = 0;
    }

    private static uint ComputeHash(Vector3[] points)
    {
        unchecked
        {
            var h = (uint)points.Length;
            h = h * 31 + HashVec(points[0]);
            h = h * 31 + HashVec(points[^1]);
            if (points.Length > 2)
                h = h * 31 + HashVec(points[points.Length / 2]);
            return h;
        }
    }

    private static uint HashVec(Vector3 v)
    {
        unchecked
        {
            return (uint)v.x.GetHashCode() ^ ((uint)v.z.GetHashCode() << 16);
        }
    }
}
