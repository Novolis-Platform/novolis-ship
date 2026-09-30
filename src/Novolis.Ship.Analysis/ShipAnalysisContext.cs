namespace Novolis.Ship.Analysis;

public sealed class ShipAnalysisContext
{
    public string? ActiveLoadCaseId { get; init; }
    public Guid? BreachCompartmentId { get; init; }
    public IReadOnlyDictionary<Guid, (float MinX, float MinY, float MinZ, float MaxX, float MaxY, float MaxZ)>? MeshBoundsByObjectId { get; init; }
}
