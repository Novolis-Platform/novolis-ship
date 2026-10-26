namespace Novolis.Ship.Structure;

/// <summary>2D cutting profile in meters (outer loop CCW, holes CW).</summary>
public sealed class FabCutProfile
{
    public required string PartId { get; init; }

    public List<(float X, float Y)> OuterLoop { get; init; } = [];

    public List<List<(float X, float Y)>> Holes { get; init; } = [];

    public bool BendAllowed { get; init; }

    public float AreaM2 { get; init; }
}
