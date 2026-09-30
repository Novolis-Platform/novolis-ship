using System.Text.Json.Serialization;
using Novolis.Cad.Primitives;
using Novolis.Math.Measure;

namespace Novolis.Ship.Design;

public sealed record HullDesign
{
    public required HullId Id { get; init; }
    public required CadDocument Geometry { get; init; }
    public required MaterialId Material { get; init; }

    [JsonConverter(typeof(LengthMetersJsonConverter))]
    public required Length Thickness { get; init; }

    public HullGeneratorKind Generator { get; init; } = HullGeneratorKind.TaperedBox;
}
