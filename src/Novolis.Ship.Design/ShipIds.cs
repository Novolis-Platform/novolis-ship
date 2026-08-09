using System.Text.Json;
using System.Text.Json.Serialization;

namespace Novolis.Ship.Design;

[JsonConverter(typeof(ShipObjectIdJsonConverter))]
public readonly record struct ShipObjectId(Guid Value)
{
    public static ShipObjectId New() => new(Guid.NewGuid());
    public static ShipObjectId From(Guid value) => new(value);
    public override string ToString() => Value.ToString("N");
}

public readonly record struct HullId(Guid Value)
{
    public static HullId New() => new(Guid.NewGuid());
    public ShipObjectId AsObject() => ShipObjectId.From(Value);
}

public readonly record struct DeckId(Guid Value)
{
    public static DeckId New() => new(Guid.NewGuid());
    public ShipObjectId AsObject() => ShipObjectId.From(Value);
}

public readonly record struct FrameId(Guid Value)
{
    public static FrameId New() => new(Guid.NewGuid());
    public ShipObjectId AsObject() => ShipObjectId.From(Value);
}

public readonly record struct LongitudinalId(Guid Value)
{
    public static LongitudinalId New() => new(Guid.NewGuid());
    public ShipObjectId AsObject() => ShipObjectId.From(Value);
}

public readonly record struct BulkheadId(Guid Value)
{
    public static BulkheadId New() => new(Guid.NewGuid());
    public ShipObjectId AsObject() => ShipObjectId.From(Value);
}

public readonly record struct CompartmentId(Guid Value)
{
    public static CompartmentId New() => new(Guid.NewGuid());
    public ShipObjectId AsObject() => ShipObjectId.From(Value);
}

public readonly record struct PassageId(Guid Value)
{
    public static PassageId New() => new(Guid.NewGuid());
    public ShipObjectId AsObject() => ShipObjectId.From(Value);
}

public readonly record struct OpeningId(Guid Value)
{
    public static OpeningId New() => new(Guid.NewGuid());
    public ShipObjectId AsObject() => ShipObjectId.From(Value);
}

public readonly record struct EquipmentId(Guid Value)
{
    public static EquipmentId New() => new(Guid.NewGuid());
    public ShipObjectId AsObject() => ShipObjectId.From(Value);
}

public readonly record struct StructuralCutoutId(Guid Value)
{
    public static StructuralCutoutId New() => new(Guid.NewGuid());
}

public readonly record struct MaterialId(string Value)
{
    public static MaterialId Steel => new("steel");
    public static MaterialId Aluminum => new("aluminum");
    public override string ToString() => Value;
}

file sealed class ShipObjectIdJsonConverter : JsonConverter<ShipObjectId>
{
    public override ShipObjectId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String && Guid.TryParse(reader.GetString(), out var g))
            return ShipObjectId.From(g);
        throw new JsonException("Expected GUID ship object id.");
    }

    public override void Write(Utf8JsonWriter writer, ShipObjectId value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.Value);
}
