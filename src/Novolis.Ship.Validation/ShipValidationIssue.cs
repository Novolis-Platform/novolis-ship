using Novolis.Cad.Primitives;
using Novolis.Ship.Primitives;
using Novolis.Ship.Topology;

namespace Novolis.Ship.Validation;

public sealed record ShipValidationIssue(
    string Code,
    ShipValidationSeverity Severity,
    string Message,
    Guid? EntityId = null);
