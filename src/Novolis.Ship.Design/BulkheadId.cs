using System.Text.Json;
using System.Text.Json.Serialization;

namespace Novolis.Ship.Design;

public readonly record struct BulkheadId(Guid Value)
{
    public static BulkheadId New() => new(Guid.NewGuid());
    public ShipObjectId AsObject() => ShipObjectId.From(Value);
}
