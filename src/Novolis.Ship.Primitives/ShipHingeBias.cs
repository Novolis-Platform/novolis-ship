namespace Novolis.Ship.Primitives;

/// <summary>Which way the leaf swings relative to the pressurized volume.</summary>
public enum ShipHingeBias
{
    Neutral = 0,
    /// <summary>Leaf opens into the higher-pressure (inboard) side.</summary>
    OpensInboard = 1,
}
