namespace Novolis.Ship.Structure;

public sealed class FabWeldSeam
{
    public required string SeamId { get; init; }

    public required string FaceA { get; init; }

    public required string FaceB { get; init; }

    public string Process { get; init; } = "GTAW";

    public string Filler { get; init; } = "ER316L";
}
