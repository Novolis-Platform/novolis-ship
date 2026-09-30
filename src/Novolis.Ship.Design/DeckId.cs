using System.Text.Json;
using System.Text.Json.Serialization;

namespace Novolis.Ship.Design;

public readonly record struct DeckId(Guid Value)
{
    public static DeckId New() => new(Guid.NewGuid());
    public ShipObjectId AsObject() => ShipObjectId.From(Value);
}
