using System.Text.Json;
using System.Text.Json.Serialization;

namespace Novolis.Ship.Design;

public readonly record struct MaterialId(string Value)
{
    public static MaterialId Steel => new("steel");
    public static MaterialId Aluminum => new("aluminum");
    public override string ToString() => Value;
}
