namespace Novolis.Ship.Structure;

/// <summary>Structural plate / sheet stock specification (not visual PBR).</summary>
public sealed record PlateMaterialSpec(
    string Designation,
    string Uns,
    float DensityKgPerM3,
    float ThicknessM,
    float StockSheetWidthM,
    float StockSheetLengthM,
    string? En = null,
    string? Product = null,
    string? WeldFiller = null,
    string? WeldProcess = null)
{
    /// <summary>AISI 316L 8 mm flat plate — Calypso outer-hull lock.</summary>
    public static PlateMaterialSpec Aisi316L_8mm { get; } = new(
        Designation: "AISI 316L",
        Uns: "S31603",
        DensityKgPerM3: 8000f,
        ThicknessM: 0.008f,
        StockSheetWidthM: 2f,
        StockSheetLengthM: 6f,
        En: "1.4404 / X2CrNiMo17-12-2",
        Product: "Annealed flat plate / sheet (cut to facet nets)",
        WeldFiller: "ER316L / ISO 14343 19 12 3 L",
        WeldProcess: "GTAW / GMAW per ECSS-Q-ST-70-39 class welding (or NASA-STD-5006 analogue)");

    public float ArealDensityKgPerM2 => DensityKgPerM3 * ThicknessM;
}
