namespace Novolis.Ship.Analysis;

public enum AnalysisSeverity
{
    Green = 0,
    Yellow = 1,
    Red = 2,
}

public enum AnalysisCategory
{
    Mass = 0,
    Cg = 1,
    Pressure = 2,
    Structure = 3,
    Clearance = 4,
    GravityLoad = 5,
}

public sealed record AnalysisFinding(
    AnalysisCategory Category,
    AnalysisSeverity Severity,
    string Code,
    string Message,
    Guid? ObjectId = null);

public sealed record CategoryStatus(AnalysisCategory Category, AnalysisSeverity Severity, int FindingCount);

public sealed class ShipAnalysisContext
{
    public string? ActiveLoadCaseId { get; init; }
    public Guid? BreachCompartmentId { get; init; }
    public IReadOnlyDictionary<Guid, (float MinX, float MinY, float MinZ, float MaxX, float MaxY, float MaxZ)>? MeshBoundsByObjectId { get; init; }
}

public sealed class ShipAnalysisReport
{
    public required IReadOnlyList<CategoryStatus> Categories { get; init; }
    public required IReadOnlyList<AnalysisFinding> Findings { get; init; }
    public required float TotalMassKg { get; init; }
    public required float CenterOfMassX { get; init; }
    public required float CenterOfMassY { get; init; }
    public required float CenterOfMassZ { get; init; }
    public IReadOnlyList<string> DecompressionCascade { get; init; } = [];
    public string? ActiveLoadCaseId { get; init; }

    public AnalysisSeverity Worst =>
        Categories.Count == 0
            ? AnalysisSeverity.Green
            : Categories.Max(c => c.Severity);

    public AnalysisSeverity StatusOf(AnalysisCategory category) =>
        Categories.FirstOrDefault(c => c.Category == category)?.Severity ?? AnalysisSeverity.Green;
}
