using System.Text.Json.Serialization;
using Novolis.Cad.Primitives;
using Novolis.Math.Measure;

namespace Novolis.Ship.Design;

public sealed record FrameDesign
{
    public required FrameId Id { get; init; }
    public required string Name { get; init; }

    [JsonConverter(typeof(LengthMetersJsonConverter))]
    public required Length Station { get; init; }

    public required CadDocument Geometry { get; init; }
    public required MaterialId Material { get; init; }
}
