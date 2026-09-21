using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VoogleRoute.Navigation;
using VoogleRoute.Pathfinding.Graph;

namespace VoogleRoute.Phone
{
    /// <summary>Heading-up projector that draws nearby roads, the active route, player and pin.</summary>
    internal sealed class PhoneGpsMapView
    {
        private const int MaxRoadSegments = 220;
        private const int MaxRouteSegments = 96;
        private const int NearestBufferSize = 96;
        private static readonly Color RoadColor = new Color(0.22f, 0.48f, 0.78f, 0.55f);
        private static readonly Color RouteColor = new Color(0.18f, 0.78f, 1f, 1f);
        private static readonly Color GridColor = new Color(0.18f, 0.42f, 0.72f, 0.28f);

        private readonly RectTransform _content;
        private readonly ImagePool _roads;
        private readonly ImagePool _route;
        private readonly ImagePool _grid;
        private readonly RectTransform _player;
        private readonly RectTransform _pin;
        private readonly RectTransform _draft;
        private readonly int[] _nearest = new int[NearestBufferSize];
        private bool _hasDraft;
        private Vector3 _draftWorld;

        private Vector3 _lookAt;
        private float _headingDeg;
        private float _visibleMeters = 160f;
        private Vector2 _mapSize;
        private Vector3 _lastRoadOrigin;
        private float _lastRoadMeters;

        internal PhoneGpsMapView(RectTransform content, RectTransform player, RectTransform pin, RectTransform draft)
        {
            _content = content;
            _player = player;
            _pin = pin;
            _draft = draft;
            _roads = new ImagePool(content, "Road", RoadColor);
            _route = new ImagePool(content, "Route", RouteColor);
            _grid = new ImagePool(content, "Grid", GridColor);
        }

        internal float VisibleMeters
        {
            get => _visibleMeters;
            set => _visibleMeters = Mathf.Clamp(value, 50f, 480f);
        }

        internal Vector3 LookAt => _lookAt;

        internal void SetView(Vector3 lookAt, float headingDeg, Vector2 mapSize)
        {
            _lookAt = lookAt;
            _headingDeg = headingDeg;
            _mapSize = mapSize;
        }

        internal void PanByPixels(Vector2 delta)
        {
            if (_mapSize.y < 1f)
                return;

            var metersPerPixel = _visibleMeters / _mapSize.y;
            var right = Quaternion.Euler(0f, _headingDeg, 0f) * Vector3.right;
            var forward = Quaternion.Euler(0f, _headingDeg, 0f) * Vector3.forward;
            _lookAt -= right * (delta.x * metersPerPixel) + forward * (delta.y * metersPerPixel);
        }

        internal Vector3 LocalToWorld(Vector2 local)
        {
            var metersPerPixel = _mapSize.y < 1f ? 1f : _visibleMeters / _mapSize.y;
            var right = Quaternion.Euler(0f, _headingDeg, 0f) * Vector3.right;
            var forward = Quaternion.Euler(0f, _headingDeg, 0f) * Vector3.forward;
            return _lookAt + right * (local.x * metersPerPixel) + forward * (local.y * metersPerPixel);
        }

        internal void SetDraft(bool hasDraft, Vector3 world)
        {
            _hasDraft = hasDraft;
            _draftWorld = world;
        }

        internal void SetBackgroundVisible(bool visible)
        {
            if (!visible)
            {
                _roads.HideUnused(0);
                _grid.HideUnused(0);
            }
        }

        internal void Redraw(Vector3 playerPos, bool hasDestination, Vector3 destination, bool drawBackground)
        {
            if (drawBackground)
            {
                var rebuildRoads = (_lookAt - _lastRoadOrigin).sqrMagnitude > 64f
                    || !Mathf.Approximately(_visibleMeters, _lastRoadMeters);
                if (rebuildRoads)
                {
                    DrawGrid();
                    DrawRoads();
                    _lastRoadOrigin = _lookAt;
                    _lastRoadMeters = _visibleMeters;
                }
            }
            else
            {
                _roads.HideUnused(0);
                _grid.HideUnused(0);
            }

            DrawRoute();
            _player.gameObject.SetActive(false);
            if (hasDestination)
                PlaceMarker(_pin, destination, 28f);
            _pin.gameObject.SetActive(hasDestination);
            if (_hasDraft && _draft != null)
                PlaceMarker(_draft, _draftWorld, 36f);
            if (_draft != null)
                _draft.gameObject.SetActive(_hasDraft);
            if (_hasDraft && _draft != null)
                _draft.SetAsLastSibling();
            else
                _pin.SetAsLastSibling();
        }

        internal void Clear()
        {
            _roads.HideUnused(0);
            _route.HideUnused(0);
            _grid.HideUnused(0);
        }

        private Vector2 WorldToLocal(Vector3 world)
        {
            var dx = world.x - _lookAt.x;
            var dz = world.z - _lookAt.z;
            var rad = _headingDeg * Mathf.Deg2Rad;
            var localX = dx * Mathf.Cos(rad) - dz * Mathf.Sin(rad);
            var localZ = dx * Mathf.Sin(rad) + dz * Mathf.Cos(rad);
            var scale = _mapSize.y / Mathf.Max(1f, _visibleMeters);
            return new Vector2(localX * scale, localZ * scale);
        }

        private void DrawGrid()
        {
            var used = 0;
            var step = _visibleMeters >= 280f ? 100f : 50f;
            var half = _visibleMeters * 0.7f;
            var minX = Mathf.Floor((_lookAt.x - half) / step) * step;
            var maxX = Mathf.Ceil((_lookAt.x + half) / step) * step;
            var minZ = Mathf.Floor((_lookAt.z - half) / step) * step;
            var maxZ = Mathf.Ceil((_lookAt.z + half) / step) * step;
            for (var x = minX; x <= maxX && used < 24; x += step)
                PlaceSegment(_grid.Next(ref used), new Vector3(x, 0f, minZ), new Vector3(x, 0f, maxZ), 1.2f);
            for (var z = minZ; z <= maxZ && used < 48; z += step)
                PlaceSegment(_grid.Next(ref used), new Vector3(minX, 0f, z), new Vector3(maxX, 0f, z), 1.2f);
            _grid.HideUnused(used);
        }

        private void DrawRoads()
        {
            var used = 0;
            if (!RouteGraphStore.TryEnsureLoaded())
            {
                _roads.HideUnused(0);
                return;
            }

            var graph = RouteGraphStore.Graph;
            var origin = new VoogleRoute.Pathfinding.Geometry.Vec3(_lookAt.x, _lookAt.y, _lookAt.z);
            var count = graph.CollectNearest(origin, _visibleMeters * 0.85f, _nearest);
            for (var i = 0; i < count && used < MaxRoadSegments; i++)
            {
                var from = _nearest[i];
                var fromPos = ToUnity(graph.GetPosition(from));
                var neighbors = graph.GetForwardNeighbors(from);
                for (var n = 0; n < neighbors.Length && used < MaxRoadSegments; n++)
                {
                    var toPos = ToUnity(graph.GetPosition(neighbors[n]));
                    PlaceSegment(_roads.Next(ref used), fromPos, toPos, 2.4f);
                }
            }

            _roads.HideUnused(used);
        }

        private void DrawRoute()
        {
            var used = 0;
            if (PathFinderService.TryGetCachedRouteForDisplay(out var path)
                && path.Points != null
                && path.Points.Length >= 2)
            {
                var points = path.Points;
                var stride = Mathf.Max(1, points.Length / MaxRouteSegments);
                var previous = points[0];
                for (var i = stride; i < points.Length && used < MaxRouteSegments; i += stride)
                {
                    PlaceSegment(_route.Next(ref used), previous, points[i], 5f);
                    previous = points[i];
                }

                if (used < MaxRouteSegments)
                    PlaceSegment(_route.Next(ref used), previous, points[points.Length - 1], 5f);
            }

            _route.HideUnused(used);
        }

        private void PlaceMarker(RectTransform marker, Vector3 world, float size)
        {
            marker.anchoredPosition = WorldToLocal(world);
            marker.sizeDelta = new Vector2(size, size);
        }

        private void PlaceSegment(Image image, Vector3 a, Vector3 b, float width)
        {
            var start = WorldToLocal(a);
            var end = WorldToLocal(b);
            var delta = end - start;
            var length = delta.magnitude;
            if (length < 0.5f)
            {
                image.gameObject.SetActive(false);
                return;
            }

            var rect = image.rectTransform;
            rect.anchoredPosition = (start + end) * 0.5f;
            rect.sizeDelta = new Vector2(length, width);
            rect.localEulerAngles = new Vector3(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            image.gameObject.SetActive(true);
        }

        private static Vector3 ToUnity(VoogleRoute.Pathfinding.Geometry.Vec3 v) =>
            new Vector3(v.X, v.Y, v.Z);

        private sealed class ImagePool
        {
            private readonly RectTransform _parent;
            private readonly string _prefix;
            private readonly Color _color;
            private readonly List<Image> _items = new List<Image>();

            internal ImagePool(RectTransform parent, string prefix, Color color)
            {
                _parent = parent;
                _prefix = prefix;
                _color = color;
            }

            internal Image Next(ref int used)
            {
                if (used >= _items.Count)
                {
                    var go = new GameObject(_prefix + used, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                    var rect = (RectTransform)go.transform;
                    rect.SetParent(_parent, false);
                    rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
                    var image = go.GetComponent<Image>();
                    image.color = _color;
                    image.raycastTarget = false;
                    _items.Add(image);
                }

                var next = _items[used];
                next.gameObject.SetActive(true);
                used++;
                return next;
            }

            internal void HideUnused(int used)
            {
                for (var i = used; i < _items.Count; i++)
                    _items[i].gameObject.SetActive(false);
            }
        }
    }
}
