using System.Text.Json.Serialization;
using Novolis.Cad.Primitives;
using Novolis.Math.Measure;

namespace Novolis.Ship.Design;

public sealed record PassageDesign
{
    public required PassageId Id { get; init; }
    public required string Name { get; init; }
    public required DeckId DeckId { get; init; }
    public required CadDocument Geometry { get; init; }

    [JsonConverter(typeof(LengthMetersJsonConverter))]
    public required Length Width { get; init; }

    [JsonConverter(typeof(LengthMetersJsonConverter))]
    public required Length Height { get; init; }

    public string ClearanceClass { get; init; } = "personnel";
}
