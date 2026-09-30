namespace Novolis.Ship.Structure;

/// <summary>Outer-skin area × thickness × density mass rollup.</summary>
public sealed record SkinMassResult(
    float OuterSkinAreaM2,
    float ThicknessM,
    float VolumeM3,
    float MassKg,
    float MassT,
    float ArealDensityKgPerM2,
    string? Note = null);
