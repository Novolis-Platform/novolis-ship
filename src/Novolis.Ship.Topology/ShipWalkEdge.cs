using System.Numerics;
using Novolis.Cad.Primitives;
using Novolis.Ship.Primitives;
using Math = System.Math;

namespace Novolis.Ship.Topology;

/// <summary>Portal between two spaces through an open hatch.</summary>
public sealed record ShipWalkEdge(
    Guid FromSpaceId,
    Guid ToSpaceId,
    Guid OpeningId,
    Vector3 Portal,
    string? OpeningName);
