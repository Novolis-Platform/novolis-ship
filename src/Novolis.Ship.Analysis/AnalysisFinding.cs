namespace Novolis.Ship.Analysis;

public sealed record AnalysisFinding(
    AnalysisCategory Category,
    AnalysisSeverity Severity,
    string Code,
    string Message,
    Guid? ObjectId = null);
