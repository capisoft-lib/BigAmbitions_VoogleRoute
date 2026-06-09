using System.Collections.Generic;
using UnityEngine;
using VoogleRoute.Navigation;
using VoogleRoute.Pathfinding.Geometry;

namespace VoogleRoute.Rendering
{
    internal static class RouteLineDetectionRenderer
    {
        private const string RootName = "VoogleRoute_RouteDetection";

        private static readonly Color CorridorColor = new Color(1f, 0f, 0f, 0.32f);

        private static GameObject _root;
        private static MeshFilter _meshFilter;
        private static MeshRenderer _meshRenderer;
        private static Mesh _mesh;
        private static Vector3[] _lastCenterline;

        internal static void EnsureCreated()
        {
            if (_meshFilter != null && _root != null)
                return;

            _root = GameObject.Find(RootName);
            if (_root == null)
            {
                _root = new GameObject(RootName);
                Object.DontDestroyOnLoad(_root);
            }

            _meshFilter = _root.GetComponent<MeshFilter>();
            if (_meshFilter == null)
                _meshFilter = _root.AddComponent<MeshFilter>();

            _meshRenderer = _root.GetComponent<MeshRenderer>();
            if (_meshRenderer == null)
                _meshRenderer = _root.AddComponent<MeshRenderer>();

            _mesh = new Mesh { name = "VoogleRouteDetectionCorridor" };
            _meshFilter.sharedMesh = _mesh;
            ApplyStyle();
        }

        internal static void ApplyStyle()
        {
            if (_meshRenderer == null)
                return;

            var shader = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color");
            if (shader == null)
                return;

            if (_meshRenderer.sharedMaterial == null || _meshRenderer.sharedMaterial.shader != shader)
                _meshRenderer.sharedMaterial = new Material(shader);

            _meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _meshRenderer.receiveShadows = false;
            _meshRenderer.sharedMaterial.color = CorridorColor;
        }

        internal static void ShowPath(PathResult path)
        {
            if (!ModConfig.ShowLineDetection)
            {
                Hide();
                return;
            }

            EnsureCreated();
            if (_root == null || _mesh == null)
                return;

            var points = ResolveDisplayPoints(path);
            if (points.Length < 2)
            {
                Hide();
                return;
            }

            if (IsSameCenterline(points))
            {
                _root.SetActive(true);
                return;
            }

            _lastCenterline = (Vector3[])points.Clone();
            var isVehicle = MovementModeDetector.CurrentMode == MovementMode.Vehicle;
            var halfWidth = RouteLineDetection.GetCrossTrackMeters(isVehicle);
            var centerline = new List<Vec3>(points.Length);
            for (var i = 0; i < points.Length; i++)
            {
                var p = points[i];
                centerline.Add(new Vec3(p.x, p.y, p.z));
            }

            var polygon = RouteCorridorBuilder.BuildPolygon(centerline, halfWidth);
            if (polygon.Count < 3)
            {
                Hide();
                return;
            }

            BuildMesh(polygon);
            _root.SetActive(true);
        }

        internal static void Hide()
        {
            _lastCenterline = null;
            if (_root != null)
                _root.SetActive(false);
        }

        internal static void Destroy()
        {
            _lastCenterline = null;

            if (_mesh != null)
            {
                Object.Destroy(_mesh);
                _mesh = null;
            }

            if (_root != null)
            {
                Object.Destroy(_root);
                _root = null;
                _meshFilter = null;
                _meshRenderer = null;
            }
        }

        private static Vector3[] ResolveDisplayPoints(PathResult path)
        {
            var points = path.Points ?? System.Array.Empty<Vector3>();
            if (!path.Success || points.Length < 2)
                return System.Array.Empty<Vector3>();

            if (MovementModeDetector.CurrentMode != MovementMode.Vehicle)
                return points;

            if (!MovementModeDetector.TryGetVehiclePose(out var pose, out var forward))
                return points;

            const float trimBehindVehicleMeters = 6f;
            var trimmed = PathGeometry.TrimBehindOrigin(points, pose, forward, trimBehindVehicleMeters);
            return trimmed.Length >= 2 ? trimmed : points;
        }

        private static bool IsSameCenterline(Vector3[] points)
        {
            if (_lastCenterline == null || _lastCenterline.Length != points.Length)
                return false;

            for (var i = 0; i < points.Length; i++)
            {
                if ((_lastCenterline[i] - points[i]).sqrMagnitude > 0.01f)
                    return false;
            }

            return true;
        }

        private static void BuildMesh(IReadOnlyList<Vec3> polygon)
        {
            var vertexCount = polygon.Count;
            var segmentCount = vertexCount / 2;
            if (segmentCount < 2)
                return;

            var vertices = new Vector3[vertexCount];
            for (var i = 0; i < vertexCount; i++)
            {
                var p = polygon[i];
                vertices[i] = new Vector3(p.X, p.Y, p.Z);
            }

            var triangleCount = (segmentCount - 1) * 2;
            var triangles = new int[triangleCount * 3];
            var write = 0;
            for (var i = 0; i < segmentCount - 1; i++)
            {
                var topLeft = i;
                var topRight = i + 1;
                var bottomRight = vertexCount - 1 - (i + 1);
                var bottomLeft = vertexCount - 1 - i;

                triangles[write++] = topLeft;
                triangles[write++] = topRight;
                triangles[write++] = bottomRight;

                triangles[write++] = topLeft;
                triangles[write++] = bottomRight;
                triangles[write++] = bottomLeft;
            }

            _mesh.Clear();
            _mesh.vertices = vertices;
            _mesh.triangles = triangles;
            _mesh.RecalculateBounds();
            _mesh.RecalculateNormals();
        }
    }
}
