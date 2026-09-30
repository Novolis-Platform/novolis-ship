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
