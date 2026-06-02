using UnityEngine;

namespace VoogleRoute.Rendering;

internal static class LineColorHelper
{
    internal static Color RouteColor => ModConfig.LineColor;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int UnlitColorId = Shader.PropertyToID("_UnlitColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int EmissiveColorId = Shader.PropertyToID("_EmissiveColor");
    private static readonly int TintColorId = Shader.PropertyToID("_TintColor");

    internal static void Apply(LineRenderer line, Color? color = null)
    {
        var c = color ?? RouteColor;
        line.startColor = c;
        line.endColor = c;

        if (line.material == null)
            return;

        line.material.color = c;
        if (line.material.HasProperty(BaseColorId))
            line.material.SetColor(BaseColorId, c);
        if (line.material.HasProperty(UnlitColorId))
            line.material.SetColor(UnlitColorId, c);
        if (line.material.HasProperty(ColorId))
            line.material.SetColor(ColorId, c);
        if (line.material.HasProperty(TintColorId))
            line.material.SetColor(TintColorId, c);
        if (line.material.HasProperty(EmissiveColorId))
            line.material.SetColor(EmissiveColorId, c * 1.2f);
    }
}
