namespace Novolis.Ship.Structure;

public sealed class FabNestPlacement
{
    public required string PartId { get; init; }

    public required string SheetId { get; init; }

    public float OriginX { get; init; }

    public float OriginY { get; init; }

    public float RotationDeg { get; init; }
}
