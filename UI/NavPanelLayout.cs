using UnityEngine;

namespace VoogleRoute.UI;

/// <summary>
/// Gabarit unique du panneau VOOGLE ROUTE (recette MoreByUs).
/// Toutes les constantes et le positionnement du cadre / titre / boutons passent par ici
/// pour éviter les décalages entre header et fond lors des mises à jour.
/// </summary>
internal static class NavPanelLayout
{
    // --- Dimensions logiques du panneau ---
    public const float PanelWidth = 370f;
    public const float ContentInset = 18f;
    public const float HeaderHeight = 48f;
    public const float HeaderTextPaddingX = 18f;
    public const float HeaderTextPaddingY = 7f;
    public const float BodyTopPadding = 5f;
    public const float BodyBottomPadding = 8f;
    public const float ButtonHeight = 40f;
    public const float ButtonGap = 8f;
    public const float ButtonTextPaddingX = 12f;
    public const float TitleFontSize = 18f;
    public const float ButtonFontSize = 16f;
    public const float ButtonLabelBottomInset = 0f;
    public const float ButtonPixelsPerUnit = 2.5f;
    /// <summary>Pixels supplémentaires sous le graphic 9-slice (layout inchangé).</summary>
    public const float ButtonGraphicBleedBottom = 2f;

    /// <summary>Graphic enfant : même cadre que le bouton + quelques px vers le bas pour les coins arrondis.</summary>
    public static void StretchButtonGraphic(RectTransform rt, float scale, float bleedBottomMultiplier = 1f)
    {
        var bleed = ButtonGraphicBleedBottom * bleedBottomMultiplier * scale;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(0f, -bleed);
        rt.offsetMax = Vector2.zero;
    }

    // --- Extension 9-slice du cadre (partagée par le seul fond du panneau) ---
    public const float FrameBleedWidth = 24f;
    public const float FrameBleedHeight = 26f;
    public const float FrameOffsetX = -2f;
    public const float FrameOffsetY = -13f;
    public const float FramePixelsPerUnit = 2.45f;

    // --- Header : ancres haut (pivot 0.5,1), width = panelWidth - trim, left ≈ trim/2 + offset ---
    // Référence droite validée : trim 11, offset -0.5 (fill x≈26..374, +1 px à droite vs corps).
    public const float HeaderTrimWidthBase = 11f;
    public const float HeaderTrimOffsetXBase = -0.5f;
    /// <summary>
    /// Lip 9-slice du corps (FrameOffsetX + grey-round-bordered) visible à gauche du fill header.
    /// Élargir de N px sans bouger le bord droit : trim -= N et offset -= N/2 (ancres centrées).
    /// </summary>
    public const float HeaderLeftExtend = 2f;

    /// <summary>Bord visible gauche du corps (grey-round-bordered), panel ref 370px @ scale 1.</summary>
    public const float BodyVisibleLeft = 26f;

    /// <summary>Bord visible droit du corps (374 fill HUD ; −1 px vs lip pour flush parfait).</summary>
    public const float BodyVisibleRight = 373f;

    /// <summary>Tranche 9-slice gauche du header (fill x=26 → rect left x=3 @ scale 1).</summary>
    public const float HeaderSliceBorderLeft = BodyVisibleLeft - 3f;

    /// <summary>Tranche 9-slice droite du header (rect right x=364 → fill x≈374 @ scale 1).</summary>
    public const float HeaderSliceBorderRight = 10f;

    /// <summary>Trim HUD + resserrement modale (px ref panel 370, par côté).</summary>
    public const float SettingsHeaderTightenPerSide = 2f;

    /// <summary>
    /// Trim HUD validé ; bordures 9-slice scalent avec le panneau (sinon header trop large @ scale &gt; 1).
    /// </summary>
    public static void ComputeHeaderRectHudTrim(
        float panelWidth,
        float scale,
        float extraTrimWidth,
        out float sizeDeltaX,
        out float anchoredPositionX)
    {
        var trimW = (HeaderTrimWidthBase - HeaderLeftExtend + extraTrimWidth) * scale;
        var trimOffset = (HeaderTrimOffsetXBase - HeaderLeftExtend * 0.5f) * scale;
        sizeDeltaX = -trimW;
        anchoredPositionX = trimOffset;
    }

    // --- Position à l'écran ---
    public const float ScreenMarginX = 16f;
    public const float ScreenMarginMinY = 36f;

    internal readonly struct Metrics
    {
        public float Scale { get; }
        public float PanelWidth { get; }
        public float PanelHeight { get; }
        public float HeaderHeight { get; }
        public float ContentInset { get; }
        public float ContentWidth { get; }
        public float BodyTopPadding { get; }
        public float BodyBottomPadding { get; }
        public float ButtonHeight { get; }
        public float ButtonGap { get; }
        public float HalfButtonWidth { get; }

        public Metrics(float scale)
        {
            Scale = scale;
            PanelWidth = NavPanelLayout.PanelWidth * scale;
            ContentInset = NavPanelLayout.ContentInset * scale;
            ContentWidth = PanelWidth - ContentInset * 2f;
            HeaderHeight = NavPanelLayout.HeaderHeight * scale;
            BodyTopPadding = NavPanelLayout.BodyTopPadding * scale;
            BodyBottomPadding = NavPanelLayout.BodyBottomPadding * scale;
            ButtonHeight = NavPanelLayout.ButtonHeight * scale;
            ButtonGap = NavPanelLayout.ButtonGap * scale;
            HalfButtonWidth = (ContentWidth - ButtonGap) * 0.5f;
            var bodyH = BodyTopPadding + ButtonHeight + BodyBottomPadding;
            PanelHeight = HeaderHeight + bodyH;
        }

        public float ButtonTopY => -(HeaderHeight + BodyTopPadding);
        public float LeftButtonX => -(HalfButtonWidth + ButtonGap) * 0.5f;
        public float RightButtonX => (HalfButtonWidth + ButtonGap) * 0.5f;
    }

    public static Metrics CreateMetrics(float scale) => new(scale);

    /// <summary>Cadre 9-slice du corps foncé (grey-round-bordered), plein panneau.</summary>
    public static void ApplyBodyFrame(RectTransform rect, float scale) => ApplyFrame(rect, scale);

    /// <summary>Cadre 9-slice du bandeau header clair (darkgreybox-header), aligné sur le corps.</summary>
    public static void ApplyHeaderFrame(RectTransform rect, in Metrics m) =>
        GameStylePanelChrome.ApplyHeaderFrameAligned(rect, m);

    private static void ApplyFrame(RectTransform rect, float scale)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(FrameOffsetX * scale, FrameOffsetY * scale);
        rect.sizeDelta = new Vector2(FrameBleedWidth * scale, FrameBleedHeight * scale);
    }

    public static void ApplyHeaderTitleInsets(RectTransform rect, in Metrics m)
    {
        var padX = HeaderTextPaddingX * m.Scale;
        var padY = HeaderTextPaddingY * m.Scale;
        rect.offsetMin = new Vector2(padX, padY);
        rect.offsetMax = new Vector2(-padX, -padY);
    }

    public static Vector2 GetScreenPosition(float offsetY)
    {
        var bottomMargin = offsetY > 0f ? Mathf.Max(ScreenMarginMinY, offsetY) : ScreenMarginMinY;
        return new Vector2(ScreenMarginX, bottomMargin);
    }
}
