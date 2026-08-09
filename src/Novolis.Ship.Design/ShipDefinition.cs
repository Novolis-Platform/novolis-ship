using System.Text.Json.Serialization;
using Novolis.Math.Measure;

namespace Novolis.Ship.Design;

/// <summary>Initial creation parameters for a valid ship design.</summary>
public sealed record ShipDefinition
{
    public required string Name { get; init; }

    [JsonConverter(typeof(LengthMetersJsonConverter))]
    public required Length Length { get; init; }

    [JsonConverter(typeof(LengthMetersJsonConverter))]
    public required Length Beam { get; init; }

    [JsonConverter(typeof(LengthMetersJsonConverter))]
    public required Length Height { get; init; }

    public required int DeckCount { get; init; }

    public required MaterialId HullMaterial { get; init; }

    [JsonConverter(typeof(LengthMetersJsonConverter))]
    public required Length HullThickness { get; init; }

    [JsonConverter(typeof(LengthMetersJsonConverter))]
    public required Length FrameSpacing { get; init; }

    public HullGeneratorKind HullGenerator { get; init; } = HullGeneratorKind.TaperedBox;

    public float LengthMeters => ShipLengths.ToMeters(Length);
    public float BeamMeters => ShipLengths.ToMeters(Beam);
    public float HeightMeters => ShipLengths.ToMeters(Height);
    public float HullThicknessMeters => ShipLengths.ToMeters(HullThickness);
    public float FrameSpacingMeters => ShipLengths.ToMeters(FrameSpacing);
}
