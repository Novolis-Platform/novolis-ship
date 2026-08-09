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

/// <summary>Computes skin mass from facet / OML areas and a plate material spec.</summary>
public static class SkinMassRollup
{
    public static SkinMassResult FromFacetAreas(float outerSkinAreaM2, PlateMaterialSpec spec, string? note = null)
    {
        ArgumentNullException.ThrowIfNull(spec);
        if (outerSkinAreaM2 < 0f)
            throw new ArgumentOutOfRangeException(nameof(outerSkinAreaM2));

        var volume = outerSkinAreaM2 * spec.ThicknessM;
        var mass = volume * spec.DensityKgPerM3;
        return new SkinMassResult(
            OuterSkinAreaM2: outerSkinAreaM2,
            ThicknessM: spec.ThicknessM,
            VolumeM3: volume,
            MassKg: mass,
            MassT: mass / 1000f,
            ArealDensityKgPerM2: spec.ArealDensityKgPerM2,
            Note: note ?? "OML loft facet area × t × density only — excludes frames, door gear, apron, coatings, fasteners");
    }

    public static PlateBomLine ToBomLine(SkinMassResult mass, string partId = "OML-SKIN", string group = "outer-skin") =>
        new(
            PartId: partId,
            Group: group,
            AreaM2: mass.OuterSkinAreaM2,
            ThicknessM: mass.ThicknessM,
            Qty: 1,
            MassKg: mass.MassKg);
}
