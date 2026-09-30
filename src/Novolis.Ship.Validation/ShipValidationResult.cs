using Novolis.Cad.Primitives;
using Novolis.Ship.Primitives;
using Novolis.Ship.Topology;

namespace Novolis.Ship.Validation;

public sealed class ShipValidationResult
{
    public required IReadOnlyList<ShipValidationIssue> Issues { get; init; }
    public bool Ok => Issues.All(i => i.Severity != ShipValidationSeverity.Error);
}
