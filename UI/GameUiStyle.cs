using System;
using System.IO;
using Il2CppTMPro;
using UnityEngine;
using UnityEngine.UI;

namespace VoogleRoute.UI;

/// <summary>
/// Reproduit le style exact des fenêtres overlay vanilla (cf. CurrentBuildingUI).
/// Les sprites/polices sont retrouvés PAR NOM parmi les ressources chargées,
/// puis mis en cache. Aucun scan répété : zéro coût par frame une fois prêt.
/// </summary>
internal static class GameUiStyle
{
    // Noms réels relevés dans la hiérarchie vanilla (UiInspector).
    private const string PanelBgName = "grey-round-bordered";
    private const string HeaderBgName = "darkgreybox-header@2x";
    private const string IconBgName = "Gradient-Blue-Round";
    private const string BtnBlueName = "Gradient-Blue-Round";
    private const string BtnGreyName = "Gradient-Gray-Border-Round";
    private const string BtnGreenName = "Gradient-Green-Round";
    private const string FontRegularName = "Rubik-Regular SDF";
    private const string FontBoldName = "Rubik-Bold SDF";
    private const string FontMediumName = "Rubik-Medium SDF";

    // Couleurs vanilla exactes.
    internal static readonly Color PanelColor = Color.white;
    internal static readonly Color White = Color.white;
    // Texte header vanilla (cf. BAUITheme.CardTextDark).
    internal static readonly Color TitleColor = new(0.15f, 0.17f, 0.22f, 1f);

    private static bool _initialized;
    private static bool _wasReady;
    private static bool _rebuildRequested;

    private static Sprite? _panelBg;
    private static Sprite? _headerBg;
    private static Sprite? _iconBg;
    private static Sprite? _btnBlue;
    private static Sprite? _btnGrey;
    private static Sprite? _btnGreen;
    private static Sprite? _steeringIcon;
    private static TMP_FontAsset? _fontRegular;
    private static TMP_FontAsset? _fontBold;
    private static TMP_FontAsset? _fontMedium;

    private static bool _iconResourceFound;
    private static int _iconOpaquePixels = -1;
    private static string _statusLine = "UI: init";

    internal static bool ShouldRebuildHud { get; private set; }

    internal static bool IsReady =>
        _panelBg != null && _headerBg != null && _btnBlue != null && _btnGrey != null && _fontBold != null;

    internal static string StatusLine
    {
        get
        {
            EnsureInitialized();
            return _statusLine;
        }
    }

    internal static Sprite? SteeringIcon
    {
        get
        {
            EnsureInitialized();
            return _steeringIcon;
        }
    }

    internal static void EnsureInitialized()
    {
        if (!_initialized)
        {
            _initialized = true;
            _steeringIcon = LoadSteeringWheelSprite();
        }

        if (IsReady)
            return;

        Discover();
        UpdateStatusLine();

        if (IsReady && !_wasReady)
            RequestRebuild();
        _wasReady = IsReady;
    }

    internal static void MarkRebuildHandled() => ShouldRebuildHud = false;

    private static void RequestRebuild()
    {
        if (_rebuildRequested)
            return;
        _rebuildRequested = true;
        ShouldRebuildHud = true;
    }

    // --- Application du style (sprite + couleur + 9-slice) ---

    internal static void ApplyPanelBg(Image image)
    {
        ApplySliced(image, _panelBg, PanelColor, _panelBg == null ? PanelColor : White);
        image.pixelsPerUnitMultiplier = NavPanelLayout.FramePixelsPerUnit;
    }

    internal static void ApplyHeaderBg(Image image)
    {
        ApplySliced(image, _headerBg, new Color(0.78f, 0.8f, 0.83f, 1f));
        image.pixelsPerUnitMultiplier = NavPanelLayout.FramePixelsPerUnit;
    }

    internal static void ApplyIconBg(Image image) => ApplySliced(image, _iconBg, new Color(0.25f, 0.58f, 0.82f, 1f));

    internal static void ApplyButtonBlue(Image image)
    {
        ApplySliced(image, _btnBlue, new Color(0.25f, 0.58f, 0.82f, 1f));
        image.pixelsPerUnitMultiplier = NavPanelLayout.ButtonPixelsPerUnit;
    }

    internal static void ApplyButtonGrey(Image image)
    {
        ApplySliced(image, _btnGrey, new Color(0.36f, 0.41f, 0.46f, 1f));
        image.pixelsPerUnitMultiplier = NavPanelLayout.ButtonPixelsPerUnit;
    }

    internal static void ApplyButtonGreen(Image image)
    {
        var vanillaContinueGreen = new Color(0.47f, 0.73f, 0.38f, 1f);
        ApplySliced(image, _btnGreen ?? _btnGrey, vanillaContinueGreen);
        image.pixelsPerUnitMultiplier = NavPanelLayout.ButtonPixelsPerUnit;
    }

    internal static void ApplyTitleFont(TextMeshProUGUI text)
    {
        var font = _fontRegular ?? _fontMedium ?? _fontBold;
        if (font != null)
            text.font = font;
    }

    internal static void ApplyButtonFont(TextMeshProUGUI text)
    {
        var font = _fontMedium ?? _fontBold;
        if (font != null)
            text.font = font;
    }

    private static void ApplySliced(Image image, Sprite? sprite, Color fallbackTint)
        => ApplySliced(image, sprite, fallbackTint, White);

    private static void ApplySliced(Image image, Sprite? sprite, Color fallbackTint, Color spriteTint)
    {
        if (sprite != null)
        {
            image.sprite = sprite;
            image.color = spriteTint;
            var b = sprite.border;
            image.type = b.x > 0.01f || b.y > 0.01f || b.z > 0.01f || b.w > 0.01f
                ? Image.Type.Sliced
                : Image.Type.Simple;
        }
        else
        {
            image.color = fallbackTint;
        }

        image.pixelsPerUnitMultiplier = 1f;
        image.preserveAspect = false;
    }

    // --- Découverte des ressources par nom ---

    private static void Discover()
    {
        try
        {
            var sprites = Resources.FindObjectsOfTypeAll<Sprite>();
            for (var i = 0; i < sprites.Length; i++)
            {
                CaptureSprite(sprites[i]);
            }

            // BAUI-style fallback: several game UI sprites are easiest to discover
            // from already-instantiated Image components rather than raw resources.
            var images = Resources.FindObjectsOfTypeAll<Image>();
            for (var i = 0; i < images.Length; i++)
            {
                var image = images[i];
                if (image != null)
                    CaptureSprite(image.sprite);
            }
        }
        catch
        {
            // ressources pas encore prêtes
        }

        try
        {
            if (_fontRegular == null || _fontBold == null || _fontMedium == null)
            {
                var fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
                for (var i = 0; i < fonts.Length; i++)
                {
                    var f = fonts[i];
                    if (f == null)
                        continue;
                    if (f.name == FontRegularName && _fontRegular == null)
                        _fontRegular = f;
                    else if (f.name == FontBoldName && _fontBold == null)
                        _fontBold = f;
                    else if (f.name == FontMediumName && _fontMedium == null)
                        _fontMedium = f;
                }

                // Repli : n'importe quelle police du jeu plutôt que la police par défaut.
                if (_fontBold == null && fonts.Length > 0)
                    _fontBold = fonts[0];
            }
        }
        catch
        {
            // polices pas encore prêtes
        }
    }

    private static void CaptureSprite(Sprite? s)
    {
        if (s == null)
            return;
        if (s.name == PanelBgName && _panelBg == null)
            _panelBg = s;
        if (s.name == HeaderBgName && _headerBg == null)
            _headerBg = s;
        if (s.name == IconBgName && _iconBg == null)
            _iconBg = s;
        if (s.name == BtnBlueName && _btnBlue == null)
            _btnBlue = s;
        if (s.name == BtnGreyName && _btnGrey == null)
            _btnGrey = s;
        if (s.name == BtnGreenName && _btnGreen == null)
            _btnGreen = s;
    }

    private static void UpdateStatusLine()
    {
        _statusLine =
            $"UI {(IsReady ? "ok" : "partiel")} | panel={_panelBg != null} header={_headerBg != null} " +
            $"icon={_iconBg != null} blue={_btnBlue != null} grey={_btnGrey != null} green={_btnGreen != null} " +
            $"font={_fontBold?.name ?? "-"} | wheel(res={_iconResourceFound},px={_iconOpaquePixels})";
    }

    // --- Icône volant (PNG embarqué -> roue blanche sur fond transparent) ---

    private static Sprite LoadSteeringWheelSprite()
    {
        try
        {
            var asm = System.Reflection.Assembly.GetExecutingAssembly();
            using var stream = asm.GetManifestResourceStream("VoogleRoute.Resources.steering_wheel.png");
            if (stream == null)
                return CreateSteeringWheelSprite();

            _iconResourceFound = true;
            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            var bytes = ms.ToArray();

            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            if (!ImageConversion.LoadImage(tex, bytes))
                return CreateSteeringWheelSprite();

            if (!ExtractWhiteShape(tex))
                return CreateSteeringWheelSprite();

            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        }
        catch
        {
            return CreateSteeringWheelSprite();
        }
    }

    private static bool ExtractWhiteShape(Texture2D tex)
    {
        var pixels = tex.GetPixels();
        var white = Color.white;
        var clear = new Color(1f, 1f, 1f, 0f);
        var opaque = 0;
        for (var i = 0; i < pixels.Length; i++)
        {
            var p = pixels[i];
            // Accept either a white icon, a dark icon, or an alpha-only cutout.
            // The output is always a solid vanilla-white icon on transparent.
            var visible = p.a > 0.25f;
            var brightShape = visible && p.r > 0.75f && p.g > 0.75f && p.b > 0.75f;
            var darkShape = visible && p.r < 0.35f && p.g < 0.35f && p.b < 0.35f;
            if (brightShape || darkShape)
            {
                pixels[i] = white;
                opaque++;
            }
            else
            {
                pixels[i] = clear;
            }
        }

        _iconOpaquePixels = opaque;
        tex.SetPixels(pixels);
        tex.Apply(false, false);
        return opaque > 8;
    }

    private static Sprite CreateSteeringWheelSprite()
    {
        const int size = 64;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        var white = Color.white;
        var clear = new Color(0f, 0f, 0f, 0f);
        var cx = size * 0.5f;
        var cy = size * 0.5f;
        var outerR = size * 0.43f;
        var innerR = size * 0.31f;
        var hubR = size * 0.09f;
        var spokeW = size * 0.055f;

        for (var y = 0; y < size; y++)
        for (var x = 0; x < size; x++)
        {
            var dx = x + 0.5f - cx;
            var dy = y + 0.5f - cy;
            var dist = Mathf.Sqrt(dx * dx + dy * dy);
            var isRing = dist <= outerR && dist >= innerR;
            var isHub = dist <= hubR;
            var leftSpoke = DistanceToSegment(dx, dy, 0f, -1f, -17f, -13f) <= spokeW;
            var rightSpoke = DistanceToSegment(dx, dy, 0f, -1f, 17f, -13f) <= spokeW;
            var bottomSpoke = Mathf.Abs(dx) <= spokeW && dy >= -1f && dy <= 19f;
            var isSpoke = (leftSpoke || rightSpoke || bottomSpoke) && dist <= innerR + 2f;
            tex.SetPixel(x, y, isRing || isHub || isSpoke ? white : clear);
        }

        tex.Apply();
        _iconOpaquePixels = 0;
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    private static float DistanceToSegment(float px, float py, float ax, float ay, float bx, float by)
    {
        var vx = bx - ax;
        var vy = by - ay;
        var wx = px - ax;
        var wy = py - ay;
        var lenSq = vx * vx + vy * vy;
        var t = lenSq <= 0.0001f ? 0f : Mathf.Clamp01((wx * vx + wy * vy) / lenSq);
        var cx = ax + t * vx;
        var cy = ay + t * vy;
        var dx = px - cx;
        var dy = py - cy;
        return Mathf.Sqrt(dx * dx + dy * dy);
    }
}
