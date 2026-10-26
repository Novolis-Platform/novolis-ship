namespace Novolis.Ship.Structure;

/// <summary>One numbered manufacturing part.</summary>
public sealed class FabPart
{
    public required string PartId { get; init; }

    public string Revision { get; init; } = "001";

    public FabPartProcess Process { get; init; }

    public string MaterialDesignation { get; init; } = "AISI 316L";

    public float ThicknessM { get; init; }

    public string SourceDrawing { get; init; } = "";

    public string? LockRef { get; init; }

    public string? CutFileRelative { get; init; }

    public float MassKg { get; init; }

    public string? CadEntityName { get; init; }
}
