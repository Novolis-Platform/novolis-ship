using System.Text.Json;
using System.Text.Json.Serialization;

namespace Novolis.Ship.Design;

public readonly record struct OpeningId(Guid Value)
{
    public static OpeningId New() => new(Guid.NewGuid());
    public ShipObjectId AsObject() => ShipObjectId.From(Value);
}
