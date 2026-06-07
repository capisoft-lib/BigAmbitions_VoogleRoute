using VoogleRoute;
using VoogleRoute.Navigation;
using UnityEngine;

namespace VoogleRoute.Rendering
{
    
    internal static class FootRouteLineRenderer
    {
        private const string RootName = "VoogleRoute_RouteFoot";
        private static GameObject _root;
        private static LineRenderer _line;
    
        internal static void EnsureCreated()
        {
            if (_line != null && _root != null)
                return;
    
            _root = GameObject.Find(RootName);
            if (_root == null)
            {
                _root = new GameObject(RootName);
                Object.DontDestroyOnLoad(_root);
            }
    
            _line = _root.GetComponent<LineRenderer>();
            if (_line == null)
                _line = _root.AddComponent<LineRenderer>();
    
            ApplyStyle();
        }
    
        internal static void ApplyStyle()
        {
            if (_line == null)
                return;
    
            _line.useWorldSpace = true;
            _line.alignment = LineAlignment.View;
            _line.textureMode = LineTextureMode.Stretch;
            _line.numCapVertices = 4;
            _line.numCornerVertices = 4;
            _line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _line.receiveShadows = false;
            _line.loop = false;
    
            var width = Mathf.Max(0.15f, ModConfig.FootLineWidth);
            _line.startWidth = width;
            _line.endWidth = width;
    
            LineRendererMaterial.Apply(_line);
        }
    
        internal static void ShowPath(PathResult path)
        {
            EnsureCreated();
            if (_root == null || _line == null)
                return;
    
            var points = path.Points ?? System.Array.Empty<Vector3>();
            if (!path.Success || points.Length < 2)
            {
                Hide();
                return;
            }
    
            _root.SetActive(true);
    
            if (!LineRendererPathCache.IsSame(points))
            {
                _line.positionCount = points.Length;
                _line.SetPositions(points);
                LineRendererMaterial.Apply(_line);
            }
        }
    
        internal static void Hide()
        {
            LineRendererPathCache.Reset();
            if (_root != null)
                _root.SetActive(false);
        }
    
        internal static void Destroy()
        {
            if (_root != null)
            {
                Object.Destroy(_root);
                _root = null;
                _line = null;
            }
        }
    }
}
