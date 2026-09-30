using System.Text.Json;
using Novolis.Cad.Primitives;

namespace Novolis.Ship.Primitives;

/// <summary>Pressure-volume view over a <c>pressureVolume</c> entity.</summary>
public sealed record PressureVolumeInfo(
    Guid Id,
    string? Name,
    string AtmosphereClass,
    float PressureKPa,
    IReadOnlyList<Guid> MemberSpaceIds,
    IReadOnlyList<Guid> HullEntityIds);
