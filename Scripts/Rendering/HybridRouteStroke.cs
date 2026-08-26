using System;
using UnityEngine;
using UnityEngine.Rendering;
using VoogleRoute;

namespace VoogleRoute.Rendering
{
    /// <summary>
    /// Uses Unity's LineRenderer when the player exposes its styling API. Some Big Ambitions
    /// builds strip those accessors, so the fallback generates an explicit world-space ribbon.
    /// </summary>
    internal sealed class HybridRouteStroke
    {
        private static bool _backendLogged;

        private readonly GameObject _root;
        private readonly LineRenderer _line;
        private readonly MeshRenderer _meshRenderer;
        private readonly Mesh _mesh;
        private Vector3[] _points = Array.Empty<Vector3>();
        private float _width;
        private Color _color;
        private bool _dashed;

        private HybridRouteStroke(
            GameObject root,
            LineRenderer line,
            MeshRenderer meshRenderer,
            Mesh mesh)
        {
            _root = root;
            _line = line;
            _meshRenderer = meshRenderer;
            _mesh = mesh;
        }

        internal bool IsReady => _root != null && (_line != null || (_meshRenderer != null && _mesh != null));

        internal float Width => _width;

        internal static HybridRouteStroke Attach(GameObject root)
        {
            if (root == null)
                return null;

            HybridRouteStroke stroke;
            if (LineRendererCompat.SupportsRouteRendering)
            {
                var line = root.GetComponent<LineRenderer>();
                if (line == null)
                    line = root.AddComponent<LineRenderer>();
                stroke = new HybridRouteStroke(root, line, null, null);
            }
            else
            {
                var filter = root.GetComponent<MeshFilter>();
                if (filter == null)
                    filter = root.AddComponent<MeshFilter>();

                var renderer = root.GetComponent<MeshRenderer>();
                if (renderer == null)
                    renderer = root.AddComponent<MeshRenderer>();

                var mesh = filter.sharedMesh;
                if (mesh == null)
                {
                    mesh = new Mesh();
                    filter.sharedMesh = mesh;
                }

                stroke = new HybridRouteStroke(root, null, renderer, mesh);
            }

            if (!_backendLogged)
            {
                _backendLogged = true;
                ModLog.Info("Route stroke backend=" +
                            (LineRendererCompat.SupportsRouteRendering ? "line_renderer" : "ribbon_mesh") +
                            " | configurable_line_width=" + LineRendererCompat.SupportsRouteRendering);
            }

            return stroke;
        }

        internal void ApplyStyle(
            float width,
            int capVertices,
            int cornerVertices,
            Color color,
            bool dashed = false)
        {
            var safeWidth = Mathf.Max(0.001f, width);
            var widthChanged = Mathf.Abs(_width - safeWidth) > 0.0001f;
            _width = safeWidth;
            _color = color;
            _dashed = dashed;

            if (_line != null)
            {
                LineRendererCompat.ApplyCommonStyle(_line, _width, capVertices, cornerVertices);
                _line.shadowCastingMode = ShadowCastingMode.Off;
                _line.receiveShadows = false;
                ApplyMaterial();
                return;
            }

            if (_meshRenderer == null || _mesh == null)
                return;

            _meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            _meshRenderer.receiveShadows = false;
            if (widthChanged && _points.Length >= 2)
                RouteRibbonMeshBuilder.Populate(_mesh, _points, _width);
            ApplyMaterial();
        }

        internal void SetColor(Color color)
        {
            _color = color;
            ApplyMaterial();
        }

        internal void SetPositions(Vector3[] points)
        {
            _points = points ?? Array.Empty<Vector3>();
            if (_line != null)
            {
                _line.positionCount = _points.Length;
                if (_points.Length > 0)
                    _line.SetPositions(_points);
            }
            else if (_mesh != null)
            {
                RouteRibbonMeshBuilder.Populate(_mesh, _points, _width);
            }

            ApplyMaterial();
        }

        private void ApplyMaterial()
        {
            if (_line != null)
            {
                if (_dashed)
                    LineRendererMaterial.ApplyDashed(_line, _color);
                else
                    LineRendererMaterial.Apply(_line, _color);
                return;
            }

            if (_meshRenderer != null)
                LineRendererMaterial.ApplyMesh(_meshRenderer, _color, _dashed);
        }
    }

    internal static class RouteRibbonMeshBuilder
    {
        private const float DirectionEpsilonSq = 0.000001f;
        private const float MinimumMiterDenominator = 0.5f;

        internal static void Populate(Mesh mesh, Vector3[] points, float width)
        {
            if (mesh == null)
                return;

            mesh.Clear();
            if (points == null || points.Length < 2)
                return;

            var vertices = new Vector3[points.Length * 2];
            var uvs = new Vector2[vertices.Length];
            var triangles = new int[(points.Length - 1) * 6];
            var halfWidth = Mathf.Max(0.0005f, width * 0.5f);
            var distance = 0f;

            for (var i = 0; i < points.Length; i++)
            {
                if (i > 0)
                    distance += Vector3.Distance(points[i - 1], points[i]);

                var previous = FindDirection(points, i, -1);
                var next = FindDirection(points, i, 1);
                var offset = ResolveOffset(previous, next, halfWidth);
                var vertex = i * 2;
                vertices[vertex] = points[i] + offset;
                vertices[vertex + 1] = points[i] - offset;
                uvs[vertex] = new Vector2(distance, 0f);
                uvs[vertex + 1] = new Vector2(distance, 1f);

                if (i >= points.Length - 1)
                    continue;

                var triangle = i * 6;
                triangles[triangle] = vertex;
                triangles[triangle + 1] = vertex + 2;
                triangles[triangle + 2] = vertex + 1;
                triangles[triangle + 3] = vertex + 1;
                triangles[triangle + 4] = vertex + 2;
                triangles[triangle + 5] = vertex + 3;
            }

            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
        }

        private static Vector3 FindDirection(Vector3[] points, int index, int step)
        {
            var cursor = index + step;
            while (cursor >= 0 && cursor < points.Length)
            {
                var direction = step < 0
                    ? points[index] - points[cursor]
                    : points[cursor] - points[index];
                direction.y = 0f;
                var lengthSq = direction.sqrMagnitude;
                if (lengthSq > DirectionEpsilonSq)
                    return direction / Mathf.Sqrt(lengthSq);
                cursor += step;
            }

            return Vector3.zero;
        }

        private static Vector3 ResolveOffset(Vector3 previous, Vector3 next, float halfWidth)
        {
            if (previous.sqrMagnitude <= DirectionEpsilonSq)
                previous = next;
            if (next.sqrMagnitude <= DirectionEpsilonSq)
                next = previous;
            if (next.sqrMagnitude <= DirectionEpsilonSq)
                return Vector3.right * halfWidth;

            var previousNormal = new Vector3(-previous.z, 0f, previous.x);
            var nextNormal = new Vector3(-next.z, 0f, next.x);
            var miter = previousNormal + nextNormal;
            if (miter.sqrMagnitude <= DirectionEpsilonSq)
                return nextNormal * halfWidth;

            miter /= Mathf.Sqrt(miter.sqrMagnitude);
            var denominator = Mathf.Abs(Vector3.Dot(miter, nextNormal));
            var extent = halfWidth / Mathf.Max(MinimumMiterDenominator, denominator);
            return miter * extent;
        }
    }
}
