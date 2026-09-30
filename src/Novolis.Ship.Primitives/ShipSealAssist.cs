namespace Novolis.Ship.Primitives;

/// <summary>
/// How differential pressure assists hatch sealing.
/// <see cref="PressureAssist"/> seats the leaf harder onto the frame when the outboard side loses pressure.
/// </summary>
public enum ShipSealAssist
{
    None = 0,
    PressureAssist = 1,
}
