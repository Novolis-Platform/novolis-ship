using System.Text.Json.Serialization;
using Novolis.Cad.Primitives;
using Novolis.Math.Measure;

namespace Novolis.Ship.Design;

public sealed record CompartmentDesign
{
    public required CompartmentId Id { get; init; }
    public required string Name { get; init; }
    public required DeckId DeckId { get; init; }
    public required CadDocument Geometry { get; init; }
    public required CompartmentKind Kind { get; init; }
}
