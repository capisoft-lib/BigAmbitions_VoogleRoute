using UnityEngine;

namespace VoogleRoute.Rendering
{
    
    internal static class LineRendererMaterial
    {
        private static Shader _shader;
        private static int _baseColorId;
        private static int _colorId;
        private static Material _lastMaterial;
        private static Color _lastAppliedColor = new Color(-1f, -1f, -1f, -1f);

        internal static void Apply(LineRenderer line, Color color)
        {
            line.startColor = color;
            line.endColor = color;

            var shader = GetShader();
            if (shader == null)
                return;

            if (line.material == null || line.material.shader != shader)
                line.material = new Material(shader);

            if (ReferenceEquals(line.material, _lastMaterial) && color == _lastAppliedColor)
                return;

            _lastMaterial = line.material;
            _lastAppliedColor = color;
            line.material.color = color;
            if (line.material.HasProperty(_baseColorId))
                line.material.SetColor(_baseColorId, color);
            if (line.material.HasProperty(_colorId))
                line.material.SetColor(_colorId, color);
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
                return _shader;
            }

            return null;
        }
    }
}
