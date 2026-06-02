using UnityEngine;

namespace VoogleRoute.Rendering;

internal static class LineRendererMaterial
{
    private static string? _shaderName;

    internal static void Apply(LineRenderer line)
    {
        var color = ModConfig.LineColor;
        line.startColor = color;
        line.endColor = color;

        var shader = FindShader();
        if (shader == null)
            return;

        if (line.material == null || line.material.shader.name != shader.name)
            line.material = new Material(shader);

        line.material.color = color;
        var baseId = Shader.PropertyToID("_BaseColor");
        var colorId = Shader.PropertyToID("_Color");
        if (line.material.HasProperty(baseId))
            line.material.SetColor(baseId, color);
        if (line.material.HasProperty(colorId))
            line.material.SetColor(colorId, color);
    }

    private static Shader? FindShader()
    {
        string[] names = { "Sprites/Default", "Unlit/Color", "Legacy Shaders/Particles/Alpha Blended" };
        foreach (var n in names)
        {
            var s = Shader.Find(n);
            if (s != null)
            {
                _shaderName = n;
                return s;
            }
        }

        return null;
    }
}
