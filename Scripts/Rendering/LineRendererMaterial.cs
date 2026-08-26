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

        internal static void ApplyMesh(MeshRenderer renderer, Color color, bool dashed)
        {
            if (renderer == null)
                return;

            ApplyRenderer(renderer, color, dashed, dashed ? 1f / SubwayDashWorldLength : 1f);
        }

        private static void Apply(LineRenderer line, Color color, bool dashed)
        {
            line.startColor = color;
            line.endColor = color;

            LineRendererCompat.SetTextureMode(
                line,
                dashed ? LineTextureMode.Tile : LineTextureMode.Stretch);
            var textureScale = dashed
                ? Mathf.Max(1f, EstimatePolylineLength(line) / SubwayDashWorldLength)
                : 1f;
            ApplyRenderer(line, color, dashed, textureScale);
        }

        private static void ApplyRenderer(Renderer renderer, Color color, bool dashed, float textureScaleX)
        {
            var shader = GetShader();
            if (shader == null)
                return;

            if (renderer.material == null || renderer.material.shader != shader)
                renderer.material = new Material(shader);

            if (ReferenceEquals(renderer.material, _lastMaterial) &&
                color == _lastAppliedColor &&
                dashed == _lastAppliedDashed &&
                !dashed)
                return;

            _lastMaterial = renderer.material;
            _lastAppliedColor = color;
            _lastAppliedDashed = dashed;
            renderer.material.color = color;
            if (renderer.material.HasProperty(_baseColorId))
                renderer.material.SetColor(_baseColorId, color);
            if (renderer.material.HasProperty(_colorId))
                renderer.material.SetColor(_colorId, color);

            if (renderer.material.HasProperty(_mainTexId))
                renderer.material.SetTexture(_mainTexId, dashed ? GetDashTexture() : null);
            renderer.material.mainTextureScale = new Vector2(textureScaleX, 1f);
        }

        private static float EstimatePolylineLength(LineRenderer line)
        {
            var count = LineRendererCompat.GetPositionCount(line);
            if (count < 2)
                return SubwayDashWorldLength;

            var total = 0f;
            if (!LineRendererCompat.TryGetPosition(line, 0, out var previous))
                return SubwayDashWorldLength;
            for (var i = 1; i < count; i++)
            {
                if (!LineRendererCompat.TryGetPosition(line, i, out var current))
                    return SubwayDashWorldLength;
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
