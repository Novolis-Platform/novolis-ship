namespace Novolis.Ship.Analysis;

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
