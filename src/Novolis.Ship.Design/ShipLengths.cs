using System.Text.Json;
using System.Text.Json.Serialization;
using Novolis.Math.Measure;

namespace Novolis.Ship.Design;

/// <summary>Ship lengths persist as meters; in-memory type is <see cref="Length"/> (points via mm).</summary>
public static class ShipLengths
{
    public static Length FromMeters(float meters) => LengthUnits.FromMillimeters(meters * 1000f);

    public static float ToMeters(Length length) => length.Millimeters / 1000f;
}

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
