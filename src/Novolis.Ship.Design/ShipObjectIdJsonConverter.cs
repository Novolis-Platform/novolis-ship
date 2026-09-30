using System.Text.Json;
using System.Text.Json.Serialization;

namespace Novolis.Ship.Design;

sealed class ShipObjectIdJsonConverter : JsonConverter<ShipObjectId>
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
