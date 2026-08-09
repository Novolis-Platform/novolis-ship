using System.Text.Json;
using System.Text.Json.Serialization;

namespace Novolis.Ship.Design;

public static class ShipDesignJson
{
    public static JsonSerializerOptions Options { get; } = CreateOptions();

    public static JsonSerializerOptions CreateOptions()
    {
        var o = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNameCaseInsensitive = true,
        };
        o.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        o.Converters.Add(new LengthMetersJsonConverter());
        return o;
    }
}

/// <summary>Read/write <c>.shipjson</c> (<see cref="ShipDesign.FormatId"/>).</summary>
public static class ShipDesignStore
{
    public static void Save(ShipDesign design, string path)
    {
        ArgumentNullException.ThrowIfNull(design);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var stamped = design with
        {
            Format = ShipDesign.FormatId,
            SchemaVersion = ShipDesign.CurrentSchemaVersion,
            ModifiedAt = DateTimeOffset.UtcNow.ToString("O"),
            CreatedAt = design.CreatedAt ?? DateTimeOffset.UtcNow.ToString("O"),
        };
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
        File.WriteAllText(path, JsonSerializer.Serialize(stamped, ShipDesignJson.Options));
    }

    public static ShipDesign Load(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var json = File.ReadAllText(path);
        var design = JsonSerializer.Deserialize<ShipDesign>(json, ShipDesignJson.Options)
            ?? throw new InvalidDataException("Ship design JSON deserialized to null.");
        if (!string.Equals(design.Format, ShipDesign.FormatId, StringComparison.Ordinal))
            throw new InvalidDataException($"Unexpected ship format '{design.Format}'.");
        return design;
    }

    public static string Serialize(ShipDesign design) =>
        JsonSerializer.Serialize(design, ShipDesignJson.Options);

    public static ShipDesign Deserialize(string json) =>
        JsonSerializer.Deserialize<ShipDesign>(json, ShipDesignJson.Options)
        ?? throw new InvalidDataException("Ship design JSON deserialized to null.");
}
