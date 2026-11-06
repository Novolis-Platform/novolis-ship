namespace Novolis.Ship.Topology;

/// <summary>Open-hatch cover tour with the hatch openings crossed.</summary>
public sealed record ShipOpenHatchTour(
    IReadOnlyList<ShipWalkWaypoint> Waypoints,
    IReadOnlyCollection<Guid> VisitedOpeningIds);
