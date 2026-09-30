namespace Novolis.Ship.Primitives;

/// <summary>Which face of the coaming carries the primary seal contact.</summary>
public enum ShipSealFace
{
    Neutral = 0,
    /// <summary>Seal contact on the vacuum / outboard side of the coaming.</summary>
    Outboard = 1,
    Inboard = 2,
}
