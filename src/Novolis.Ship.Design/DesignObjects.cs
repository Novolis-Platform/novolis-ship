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

public sealed record DeckDesign
{
    public required DeckId Id { get; init; }
    public required string Name { get; init; }

    [JsonConverter(typeof(LengthMetersJsonConverter))]
    public required Length Elevation { get; init; }

    public required CadDocument Geometry { get; init; }
    public int Index { get; init; }
}

public sealed record FrameDesign
{
    public required FrameId Id { get; init; }
    public required string Name { get; init; }

    [JsonConverter(typeof(LengthMetersJsonConverter))]
    public required Length Station { get; init; }

    public required CadDocument Geometry { get; init; }
    public required MaterialId Material { get; init; }
}

public sealed record LongitudinalDesign
{
    public required LongitudinalId Id { get; init; }
    public required string Name { get; init; }
    public required LongitudinalKind Kind { get; init; }
    public required CadDocument Geometry { get; init; }
    public required MaterialId Material { get; init; }
}

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

public sealed record CompartmentDesign
{
    public required CompartmentId Id { get; init; }
    public required string Name { get; init; }
    public required DeckId DeckId { get; init; }
    public required CadDocument Geometry { get; init; }
    public required CompartmentKind Kind { get; init; }
}

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

public sealed record OpeningDesign
{
    public required OpeningId Id { get; init; }
    public required string Name { get; init; }
    public required ShipObjectId HostId { get; init; }
    public required CadDocument Geometry { get; init; }
    public required OpeningKind Kind { get; init; }
}

public sealed record EquipmentDesign
{
    public required EquipmentId Id { get; init; }
    public required string Name { get; init; }
    public required CadDocument Geometry { get; init; }
    public float MassKg { get; init; }

    [JsonConverter(typeof(LengthMetersJsonConverter))]
    public Length ServiceClearance { get; init; }

    public float YawRadians { get; init; }
}

public sealed record StructuralCutout
{
    public required StructuralCutoutId Id { get; init; }
    public required ShipObjectId SourceId { get; init; }
    public required ShipObjectId HostId { get; init; }
    public required CutoutPurpose Purpose { get; init; }
}
