using Il2CppTMPro;
using VoogleRoute.Navigation;
using UnityEngine;
using Object = UnityEngine.Object;

namespace VoogleRoute.UI;

/// <summary>Panneau debug en jeu — uniquement si ShowDebugOverlay (mode dev).</summary>
public static class DebugOverlayHud
{
    private const string RootName = "VoogleRoute_DebugOverlay";
    private static TextMeshProUGUI? _label;

    public static void EnsureCreated()
    {
        if (_label != null)
            return;

        var root = new GameObject(RootName);
        Object.DontDestroyOnLoad(root);

        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 8999;

        root.AddComponent<UnityEngine.UI.CanvasScaler>().uiScaleMode =
            UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;

        var go = new GameObject("Text");
        go.transform.SetParent(root.transform, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0f, 0f);
        rect.pivot = new Vector2(0f, 0f);
        rect.anchoredPosition = new Vector2(12f, 120f);
        rect.sizeDelta = new Vector2(520f, 200f);

        _label = go.AddComponent<TextMeshProUGUI>();
        _label.fontSize = 14f;
        _label.alignment = TextAlignmentOptions.BottomLeft;
        _label.color = new Color(0.9f, 0.95f, 1f, 0.92f);
    }

    public static void Update(
        bool playable,
        MovementMode mode,
        bool outdoor,
        bool hasTarget,
        Vector3 target,
        PathResult path,
        TurnGuidanceState guidance)
    {
        if (!ModConfig.ShowDebugOverlay.Value)
        {
            if (_label != null)
                _label.transform.root.gameObject.SetActive(false);
            return;
        }

        EnsureCreated();
        if (_label == null)
            return;

        _label.transform.root.gameObject.SetActive(true);
        _label.text =
            $"[VoogleRoute debug]\n" +
            $"playable={playable} dehors={outdoor} mode={mode}\n" +
            $"GPS carte={hasTarget} src={NavigationTargetTracker.LastSource}\n" +
            $"target=({target.x:F0},{target.y:F0},{target.z:F0})\n" +
            $"path ok={path.Success} pts={path.PointCount} corners={PathFinderService.LastTurnCorners.Length}\n" +
            $"HUD: {guidance.InstructionLine1} | {guidance.InstructionLine2}\n" +
            $"{GameUiStyle.StatusLine}\n" +
            $"NavHudOffsetY={ModConfig.NavHudOffsetY.Value} (MelonPreferences)";
    }

    public static void Destroy()
    {
        var existing = GameObject.Find(RootName);
        if (existing != null)
            Object.Destroy(existing);
        _label = null;
    }
}
