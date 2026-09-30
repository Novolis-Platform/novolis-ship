using System.Text.Json;
using System.Text.Json.Serialization;

namespace Novolis.Ship.Design;

[JsonConverter(typeof(ShipObjectIdJsonConverter))]
public readonly record struct ShipObjectId(Guid Value)
{
    public static ShipObjectId New() => new(Guid.NewGuid());
    public static ShipObjectId From(Guid value) => new(value);
    public override string ToString() => Value.ToString("N");
}
