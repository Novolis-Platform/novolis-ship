using System.Text.Json.Serialization;
using Novolis.Cad.Primitives;
using Novolis.Math.Measure;

namespace Novolis.Ship.Design;

public sealed record OpeningDesign
{
    public required OpeningId Id { get; init; }
    public required string Name { get; init; }
    public required ShipObjectId HostId { get; init; }
    public required CadDocument Geometry { get; init; }
    public required OpeningKind Kind { get; init; }
}
