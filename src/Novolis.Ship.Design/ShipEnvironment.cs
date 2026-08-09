namespace Novolis.Ship.Design;

public enum ExternalEnvironmentKind
{
    Vacuum = 0,
    Atmosphere = 1,
    Underwater = 2,
}

public enum GravitySystemKind
{
    None = 0,
    Plating = 1,
}

/// <summary>Operating environment for a spacecraft design (persisted intent).</summary>
public sealed record ShipEnvironment
{
    public ExternalEnvironmentKind External { get; init; } = ExternalEnvironmentKind.Vacuum;

    /// <summary>Nominal internal pressure in atmospheres (1.0 = 1 atm).</summary>
    public float NominalInternalPressureAtm { get; init; } = 1f;

    public GravitySystemKind GravitySystem { get; init; } = GravitySystemKind.Plating;

    /// <summary>Nominal artificial gravity in g when plating enabled.</summary>
    public float NominalGravityG { get; init; } = 1f;

    /// <summary>Optional external acceleration magnitude in g (load-case overlay may override).</summary>
    public float ExternalAccelerationG { get; init; }

    public static ShipEnvironment CreateDefault() => new();

    public static ShipEnvironment FromDefinition(ShipDefinition definition) => new()
    {
        External = definition.ExternalEnvironment,
        NominalInternalPressureAtm = definition.NominalInternalPressureAtm,
        GravitySystem = definition.GravitySystem,
        NominalGravityG = definition.NominalGravityG,
        ExternalAccelerationG = 0f,
    };

    /// <summary>Effective gravity for structural load heuristics under the given plating state.</summary>
    public float EffectiveGravityG(bool platingEnabled) =>
        GravitySystem == GravitySystemKind.None || !platingEnabled ? 0f : NominalGravityG;
}
