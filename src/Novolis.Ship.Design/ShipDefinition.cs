using System.Text.Json.Serialization;
using Novolis.Math.Measure;

namespace Novolis.Ship.Design;

/// <summary>Initial creation parameters for a valid spacecraft design.</summary>
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

    /// <summary>Explicit deck spacing; when unset/zero, factory uses Height / DeckCount.</summary>
    [JsonConverter(typeof(LengthMetersJsonConverter))]
    public Length DeckSpacing { get; init; }

    public required MaterialId HullMaterial { get; init; }

    [JsonConverter(typeof(LengthMetersJsonConverter))]
    public required Length HullThickness { get; init; }

    [JsonConverter(typeof(LengthMetersJsonConverter))]
    public required Length FrameSpacing { get; init; }

    public MaterialId PrimaryStructuralMaterial { get; init; } = MaterialId.Steel;

    public HullGeneratorKind HullGenerator { get; init; } = HullGeneratorKind.TaperedBox;

    public GravitySystemKind GravitySystem { get; init; } = GravitySystemKind.Plating;

    public float NominalGravityG { get; init; } = 1f;

    public float NominalInternalPressureAtm { get; init; } = 1f;

    public ExternalEnvironmentKind ExternalEnvironment { get; init; } = ExternalEnvironmentKind.Vacuum;

    public float LengthMeters => ShipLengths.ToMeters(Length);
    public float BeamMeters => ShipLengths.ToMeters(Beam);
    public float HeightMeters => ShipLengths.ToMeters(Height);
    public float HullThicknessMeters => ShipLengths.ToMeters(HullThickness);
    public float FrameSpacingMeters => ShipLengths.ToMeters(FrameSpacing);

    public float DeckSpacingMeters
    {
        get
        {
            var explicitSpacing = ShipLengths.ToMeters(DeckSpacing);
            if (explicitSpacing > 1e-4f)
                return explicitSpacing;
            return HeightMeters / System.Math.Max(1, DeckCount);
        }
    }
}
