using System.Text.Json.Serialization;
using Novolis.Cad.Primitives;
using Novolis.Math.Measure;

namespace Novolis.Ship.Design;

public sealed record StructuralCutout
{
    public required StructuralCutoutId Id { get; init; }
    public required ShipObjectId SourceId { get; init; }
    public required ShipObjectId HostId { get; init; }
    public required CutoutPurpose Purpose { get; init; }
}
