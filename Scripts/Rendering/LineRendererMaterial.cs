using UnityEngine;

namespace VoogleRoute.Rendering
{
    
    internal static class LineRendererMaterial
    {
        private const float SubwayDashWorldLength = 5f;

        private static Shader _shader;
        private static int _baseColorId;
        private static int _colorId;
        private static int _mainTexId;
        private static Material _lastMaterial;
        private static Color _lastAppliedColor = new Color(-1f, -1f, -1f, -1f);
        private static bool _lastAppliedDashed;
        private static Texture2D _dashTexture;

        internal static void Apply(LineRenderer line, Color color) =>
            Apply(line, color, dashed: false);

        internal static void ApplyDashed(LineRenderer line, Color color) =>
            Apply(line, color, dashed: true);

        private static void Apply(LineRenderer line, Color color, bool dashed)
        {
            line.startColor = color;
            line.endColor = color;

            var shader = GetShader();
            if (shader == null)
                return;

            if (line.material == null || line.material.shader != shader)
                line.material = new Material(shader);

            if (ReferenceEquals(line.material, _lastMaterial) &&
                color == _lastAppliedColor &&
                dashed == _lastAppliedDashed &&
                !dashed)
                return;

            _lastMaterial = line.material;
            _lastAppliedColor = color;
            _lastAppliedDashed = dashed;
            line.material.color = color;
            if (line.material.HasProperty(_baseColorId))
                line.material.SetColor(_baseColorId, color);
            if (line.material.HasProperty(_colorId))
                line.material.SetColor(_colorId, color);

            if (!dashed)
            {
                line.textureMode = LineTextureMode.Stretch;
                if (line.material.HasProperty(_mainTexId))
                    line.material.SetTexture(_mainTexId, null);
                return;
            }

            line.textureMode = LineTextureMode.Tile;
            var dashTexture = GetDashTexture();
            if (line.material.HasProperty(_mainTexId))
                line.material.SetTexture(_mainTexId, dashTexture);

            var dashRepeat = Mathf.Max(1f, EstimatePolylineLength(line) / SubwayDashWorldLength);
            line.material.mainTextureScale = new Vector2(dashRepeat, 1f);
        }

        private static float EstimatePolylineLength(LineRenderer line)
        {
            var count = line.positionCount;
            if (count < 2)
                return SubwayDashWorldLength;

            var total = 0f;
            var previous = line.GetPosition(0);
            for (var i = 1; i < count; i++)
            {
                var current = line.GetPosition(i);
                total += Vector3.Distance(previous, current);
                previous = current;
            }

            return Mathf.Max(total, SubwayDashWorldLength);
        }

        private static Texture2D GetDashTexture()
        {
            if (_dashTexture != null)
                return _dashTexture;

            const int width = 64;
            _dashTexture = new Texture2D(width, 1, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear
            };

            var pixels = new Color[width];
            for (var i = 0; i < width; i++)
                pixels[i] = i < width / 2 ? Color.white : Color.clear;

            _dashTexture.SetPixels(pixels);
            _dashTexture.Apply();
            return _dashTexture;
        }

        private static Shader GetShader()
        {
            if (_shader != null)
                return _shader;

            string[] names = { "Sprites/Default", "Unlit/Color", "Legacy Shaders/Particles/Alpha Blended" };
            foreach (var n in names)
            {
                var s = Shader.Find(n);
                if (s == null)
                    continue;

                _shader = s;
                _baseColorId = Shader.PropertyToID("_BaseColor");
                _colorId = Shader.PropertyToID("_Color");
                _mainTexId = Shader.PropertyToID("_MainTex");
                return _shader;
            }

            return null;
        }
    }
}
