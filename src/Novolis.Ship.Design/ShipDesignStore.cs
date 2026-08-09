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
        var stamped = Migrate(design) with
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
        return Deserialize(json);
    }

    public static string Serialize(ShipDesign design) =>
        JsonSerializer.Serialize(Migrate(design) with
        {
            Format = ShipDesign.FormatId,
            SchemaVersion = ShipDesign.CurrentSchemaVersion,
        }, ShipDesignJson.Options);

    public static ShipDesign Deserialize(string json)
    {
        var design = JsonSerializer.Deserialize<ShipDesign>(json, ShipDesignJson.Options)
            ?? throw new InvalidDataException("Ship design JSON deserialized to null.");
        if (!string.Equals(design.Format, ShipDesign.FormatId, StringComparison.Ordinal))
            throw new InvalidDataException($"Unexpected ship format '{design.Format}'.");
        return Migrate(design);
    }

    /// <summary>v1 → v2: fill Environment, LoadCases, structural material / deck spacing defaults.</summary>
    public static ShipDesign Migrate(ShipDesign design)
    {
        ArgumentNullException.ThrowIfNull(design);
        var ship = design.Ship;
        if (string.IsNullOrWhiteSpace(ship.PrimaryStructuralMaterial.Value))
            ship = ship with { PrimaryStructuralMaterial = ship.HullMaterial };

        var environment = design.SchemaVersion < 2
            ? new ShipEnvironment
            {
                External = ship.ExternalEnvironment,
                NominalInternalPressureAtm = ship.NominalInternalPressureAtm > 0
                    ? ship.NominalInternalPressureAtm
                    : 1f,
                GravitySystem = ship.GravitySystem,
                NominalGravityG = ship.NominalGravityG > 0 ? ship.NominalGravityG : 1f,
            }
            : design.Environment;

        var loadCases = design.LoadCases is { Count: > 0 }
            ? design.LoadCases
            : ShipLoadCase.CreateBaseline(environment);

        var cutouts = design.Cutouts;
        if (cutouts.Count == 0 && (design.Passages.Count > 0 || design.Openings.Count > 0 || design.Equipment.Count > 0))
            return StructuralCutoutService.Regenerate(design with
            {
                Ship = ship,
                Environment = environment,
                LoadCases = loadCases,
                SchemaVersion = ShipDesign.CurrentSchemaVersion,
            });

        return design with
        {
            Ship = ship,
            Environment = environment,
            LoadCases = loadCases,
            SchemaVersion = ShipDesign.CurrentSchemaVersion,
        };
    }
}
