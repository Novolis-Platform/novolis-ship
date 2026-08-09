namespace Novolis.Ship.Structure;

/// <summary>One BOM line for a plate nest / facet group.</summary>
public sealed record PlateBomLine(
    string PartId,
    string Group,
    float AreaM2,
    float ThicknessM,
    int Qty,
    float MassKg,
    string? MaterialDesignation = null);
