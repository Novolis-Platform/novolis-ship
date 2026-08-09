namespace Novolis.Ship.Design;

public enum ShipLoadCaseKind
{
    NominalCruise = 0,
    Maneuver = 1,
    GravityOffline = 2,
    Docked = 3,
}

/// <summary>Named spacecraft load case consumed by analysis (persisted intent).</summary>
public sealed record ShipLoadCase
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required ShipLoadCaseKind Kind { get; init; }

    /// <summary>Artificial gravity multiplier relative to Environment.NominalGravityG (0 = offline).</summary>
    public float ArtificialGravityFactor { get; init; } = 1f;

    /// <summary>Design thrust / acceleration in g.</summary>
    public float ThrustAccelerationG { get; init; }

    /// <summary>External gravity in g (docked / planetary).</summary>
    public float ExternalGravityG { get; init; }

    public static IReadOnlyList<ShipLoadCase> CreateBaseline(ShipEnvironment? environment = null)
    {
        var designAccel = 0.2f;
        return
        [
            new ShipLoadCase
            {
                Id = "nominal-cruise",
                Name = "Nominal Cruise",
                Kind = ShipLoadCaseKind.NominalCruise,
                ArtificialGravityFactor = 1f,
                ThrustAccelerationG = 0f,
                ExternalGravityG = 0f,
            },
            new ShipLoadCase
            {
                Id = "maneuver",
                Name = "Maneuver",
                Kind = ShipLoadCaseKind.Maneuver,
                ArtificialGravityFactor = 1f,
                ThrustAccelerationG = designAccel,
                ExternalGravityG = 0f,
            },
            new ShipLoadCase
            {
                Id = "gravity-offline",
                Name = "Gravity Offline",
                Kind = ShipLoadCaseKind.GravityOffline,
                ArtificialGravityFactor = 0f,
                ThrustAccelerationG = designAccel,
                ExternalGravityG = 0f,
            },
            new ShipLoadCase
            {
                Id = "docked",
                Name = "Docked",
                Kind = ShipLoadCaseKind.Docked,
                ArtificialGravityFactor = environment?.GravitySystem == GravitySystemKind.Plating ? 1f : 0f,
                ThrustAccelerationG = 0f,
                ExternalGravityG = environment?.ExternalAccelerationG ?? 0f,
            },
        ];
    }
}
