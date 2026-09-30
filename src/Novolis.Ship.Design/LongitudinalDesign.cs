using System.Text.Json.Serialization;
using Novolis.Cad.Primitives;
using Novolis.Math.Measure;

namespace Novolis.Ship.Design;

public sealed record LongitudinalDesign
{
    public required LongitudinalId Id { get; init; }
    public required string Name { get; init; }
    public required LongitudinalKind Kind { get; init; }
    public required CadDocument Geometry { get; init; }
    public required MaterialId Material { get; init; }
}
