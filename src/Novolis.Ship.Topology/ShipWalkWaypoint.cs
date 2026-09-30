using System.Numerics;
using Novolis.Cad.Primitives;
using Novolis.Ship.Primitives;
using Math = System.Math;

namespace Novolis.Ship.Topology;

/// <summary>Standing-eye waypoint for a walk tour.</summary>
public sealed record ShipWalkWaypoint(
    Vector3 Eye,
    Vector3 Look,
    Guid SpaceId,
    Guid? OpeningId,
    string Label);
