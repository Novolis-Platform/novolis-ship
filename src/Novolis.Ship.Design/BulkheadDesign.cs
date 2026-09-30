using System.Text.Json.Serialization;
using Novolis.Cad.Primitives;
using Novolis.Math.Measure;

namespace Novolis.Ship.Design;

public sealed record BulkheadDesign
{
    public required BulkheadId Id { get; init; }
    public required string Name { get; init; }
    public required CadDocument Geometry { get; init; }
    public required MaterialId Material { get; init; }

    [JsonConverter(typeof(LengthMetersJsonConverter))]
    public required Length Thickness { get; init; }

    [JsonConverter(typeof(LengthMetersJsonConverter))]
    public required Length Height { get; init; }

    public DeckId? DeckId { get; init; }
    public bool IsPrimary { get; init; } = true;
}
