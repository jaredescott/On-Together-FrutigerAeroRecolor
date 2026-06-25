using UnityEngine;

namespace FrutigerAeroRecolor;

internal enum CliffFaceStyle
{
    CreamStone,
    SoftAqua,
    WarmBrown,
}

internal enum MaterialApplyMode
{
    Standard,
    BuildingForce,
    BuildingStructureForce,
    BuildingGlassSoft,
    FoliageForce,
    LightPreserving,
    PathForce,
}

internal sealed class RecolorSettings
{
    public float BaseStrength { get; set; } = 0.85f;
    public float ShadeStrength { get; set; } = 0.58f;
    public float BatchStrength { get; set; } = 0.75f;
    public float BuildingStrength { get; set; } = 1.0f;
    public float FoliageStrength { get; set; } = 0.82f;
    public float VibrancyMultiplier { get; set; } = 1.05f;
    public bool PreserveToonShading { get; set; } = true;
    public CliffFaceStyle CliffFaceStyle { get; set; } = CliffFaceStyle.CreamStone;
    public Color BuildingGlassBlue { get; set; } = new(0.30f, 0.86f, 0.98f);
    public Color BuildingStructureWhite { get; set; } = new(0.96f, 0.97f, 0.95f);
    public Color PathWhite { get; set; } = new(0.94f, 0.97f, 0.99f);
}
