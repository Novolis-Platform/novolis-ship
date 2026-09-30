namespace Novolis.Ship.Analysis;

public sealed record CategoryStatus(AnalysisCategory Category, AnalysisSeverity Severity, int FindingCount);
