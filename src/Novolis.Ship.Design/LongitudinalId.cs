using System.Text.Json;
using System.Text.Json.Serialization;

namespace Novolis.Ship.Design;

public readonly record struct LongitudinalId(Guid Value)
{
    public static LongitudinalId New() => new(Guid.NewGuid());
    public ShipObjectId AsObject() => ShipObjectId.From(Value);
}
