using System.Text.Json.Serialization;
using Novolis.Cad.Primitives;
using Novolis.Math.Measure;

namespace Novolis.Ship.Design;

public sealed record EquipmentDesign
{
    public required EquipmentId Id { get; init; }
    public required string Name { get; init; }
    public required CadDocument Geometry { get; init; }
    public float MassKg { get; init; }

    [JsonConverter(typeof(LengthMetersJsonConverter))]
    public Length ServiceClearance { get; init; }

    /// <summary>Yaw / pitch / roll (radians).</summary>
    public float[] Orientation { get; init; } = [0f, 0f, 0f];

    /// <summary>Connection points as XYZ triples (baseline equipment envelope).</summary>
    public IReadOnlyList<float[]> ConnectionPoints { get; init; } = [];

    public float YawRadians { get; init; }
}
