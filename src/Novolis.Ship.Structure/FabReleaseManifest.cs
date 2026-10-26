namespace Novolis.Ship.Structure;

/// <summary>CAL-MFG-001 machine-readable manufacturing release.</summary>
public sealed class FabReleaseManifest
{
    public string PackageId { get; init; } = "CAL-MFG-001";

    public string Revision { get; init; } = "001";

    public string GeneratedAtUtc { get; init; } = "";

    public string AuthorityLock { get; init; } = "CAL-INT-GA-001.json";

    public string AuthorityHull { get; init; } = "CAL-HULL-CAD-001.json";

    public List<FabPart> Parts { get; init; } = [];

    public List<FabCutProfile> Cuts { get; init; } = [];

    public List<FabNestPlacement> Nests { get; init; } = [];

    public List<FabWeldSeam> Welds { get; init; } = [];

    public List<FabAssemblyInstance> Sequence { get; init; } = [];

    public FabReleaseTotals? Totals { get; init; }

    public Dictionary<string, string> ContentHashes { get; init; } = new(StringComparer.Ordinal);
}
