using System.Text.Json;
using System.Text.Json.Serialization;
using Novolis.Math.Measure;

namespace Novolis.Ship.Design;

/// <summary>JSON number = meters.</summary>
public sealed class LengthMetersJsonConverter : JsonConverter<Length>
{
    public override Length Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number && reader.TryGetSingle(out var meters))
            return ShipLengths.FromMeters(meters);
        if (reader.TokenType == JsonTokenType.String
            && float.TryParse(reader.GetString(), out var parsed))
            return ShipLengths.FromMeters(parsed);
        throw new JsonException("Expected length in meters.");
    }

    public override void Write(Utf8JsonWriter writer, Length value, JsonSerializerOptions options) =>
        writer.WriteNumberValue(ShipLengths.ToMeters(value));
}
