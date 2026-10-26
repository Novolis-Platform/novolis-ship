namespace Novolis.Ship.Structure;

/// <summary>Installed pose in manufacturer frame (meters, X forward / Y starboard / Z up).</summary>
public sealed class FabAssemblyInstance
{
    public required string PartId { get; init; }

    public int SequenceIndex { get; init; }

    public string? ParentAssembly { get; init; }

    public float PosX { get; init; }

    public float PosY { get; init; }

    public float PosZ { get; init; }

    public float RotDegX { get; init; }

    public float RotDegY { get; init; }

    public float RotDegZ { get; init; }
}
