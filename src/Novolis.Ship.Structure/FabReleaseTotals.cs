namespace Novolis.Ship.Structure;

public sealed class FabReleaseTotals
{
    public int PartCount { get; init; }

    public int CutPartCount { get; init; }

    public int InstallPartCount { get; init; }

    public int SheetCount { get; init; }

    public float TotalMassKg { get; init; }

    public float HullSkinAreaM2 { get; init; }
}
