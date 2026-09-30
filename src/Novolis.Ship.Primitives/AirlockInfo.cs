using System.Text.Json;
using Novolis.Cad.Primitives;

namespace Novolis.Ship.Primitives;

/// <summary>Airlock pair view over an <c>airlock</c> entity.</summary>
public sealed record AirlockInfo(
    Guid Id,
    string? Name,
    Guid VestibuleSpaceId,
    Guid OuterOpeningId,
    Guid InnerOpeningId);
