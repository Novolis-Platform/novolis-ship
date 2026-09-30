using System.Text.Json;
using System.Text.Json.Serialization;

namespace Novolis.Ship.Design;

public readonly record struct StructuralCutoutId(Guid Value)
{
    public static StructuralCutoutId New() => new(Guid.NewGuid());
}
