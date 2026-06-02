using Il2CppTMPro;
using VoogleRoute.Navigation;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace VoogleRoute.UI;

/// <summary>HUD virage (voiture) — distance + instruction, stable entre les recalculs.</summary>
public static class TurnNavigationHud
{
    private const string RootName = "VoogleRoute_TurnHudRoot_v2";
    private static GameObject? _root;
    private static TextMeshProUGUI? _distanceLabel;
    private static TextMeshProUGUI? _instructionLabel;

    private static TurnGuidanceState _lastState;
    private static bool _hasLastState;

    public static void EnsureCreated()
    {
        var legacy = GameObject.Find("VoogleRoute_TurnHudRoot");
        if (legacy != null)
            Object.Destroy(legacy);

        if (_root != null)
            return;

        _root = new GameObject(RootName);
        Object.DontDestroyOnLoad(_root);

        var canvas = _root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9002;

        var scaler = _root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        var panel = new GameObject("TurnPanel");
        panel.transform.SetParent(_root.transform, false);
        var rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -118f);
        rect.sizeDelta = new Vector2(440f, 92f);

        var panelBg = panel.AddComponent<Image>();
        panelBg.color = new Color(0.06f, 0.09f, 0.13f, 0.92f);

        var outline = panel.AddComponent<Outline>();
        outline.effectColor = new Color(0.55f, 0.62f, 0.72f, 0.45f);
        outline.effectDistance = new Vector2(1f, -1f);

        _distanceLabel = CreateLabel(panel.transform, "Distance", 34, new Vector2(0f, 20f));
        _instructionLabel = CreateLabel(panel.transform, "Instruction", 22, new Vector2(0f, -20f));
    }

    /// <param name="state">Nouvel état ; ignoré si <paramref name="visible"/> false.</param>
    /// <param name="visible">Afficher le dernier état valide (voiture).</param>
    public static void Update(TurnGuidanceState state, bool visible)
    {
        EnsureCreated();
        if (_root == null)
            return;

        if (visible && state.HasGuidance)
        {
            _lastState = state;
            _hasLastState = true;
        }

        var show = visible && _hasLastState && _lastState.HasGuidance && ModConfig.ShowTurnGuidance.Value;
        _root.SetActive(show);
        if (!show || _distanceLabel == null || _instructionLabel == null)
            return;

        _distanceLabel.text = _lastState.InstructionLine1;
        _instructionLabel.text = _lastState.InstructionLine2;
    }

    public static void Clear()
    {
        _hasLastState = false;
        _lastState = default;
        if (_root != null)
            _root.SetActive(false);
    }

    public static void Destroy()
    {
        if (_root != null)
        {
            Object.Destroy(_root);
            _root = null;
            _distanceLabel = null;
            _instructionLabel = null;
        }

        _hasLastState = false;
    }

    private static TextMeshProUGUI CreateLabel(Transform parent, string name, float fontSize, Vector2 anchoredY)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(420f, 42f);
        rect.anchoredPosition = anchoredY;

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.fontStyle = FontStyles.Bold;
        return tmp;
    }
}
