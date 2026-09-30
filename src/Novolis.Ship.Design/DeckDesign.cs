using System.Text.Json.Serialization;
using Novolis.Cad.Primitives;
using Novolis.Math.Measure;

namespace Novolis.Ship.Design;

public sealed record DeckDesign
{
    public required DeckId Id { get; init; }
    public required string Name { get; init; }

    [JsonConverter(typeof(LengthMetersJsonConverter))]
    public required Length Elevation { get; init; }

    public required CadDocument Geometry { get; init; }
    public int Index { get; init; }
}
