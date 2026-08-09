namespace Novolis.Ship.Structure;

/// <summary>Bill of materials for structural plate work.</summary>
public sealed class ShipBom
{
    public string Drawing { get; init; } = "";

    public string Rev { get; init; } = "";

    public PlateMaterialSpec? Material { get; init; }

    public List<PlateBomLine> Lines { get; init; } = [];

    public SkinMassResult? SkinMass { get; init; }

    public float TotalMassKg =>
        Lines.Count > 0
            ? Lines.Sum(l => l.MassKg * System.Math.Max(1, l.Qty))
            : SkinMass?.MassKg ?? 0f;
}
