using System.Numerics;
using Novolis.Cad.Primitives;
using Novolis.Ship.Primitives;

namespace Novolis.Ship.Topology;

/// <summary>Result of an airtightness analysis pass.</summary>
public sealed class ShipTopologyResult
{
    public required IReadOnlyList<Guid> SpaceIds { get; init; }

    /// <summary>Space ids that can reach exterior through open/non-airtight openings or missing hosts.</summary>
    public required IReadOnlySet<Guid> VentingToExterior { get; init; }

    /// <summary>Connected components of spaces that do not reach exterior (sealed clusters).</summary>
    public required IReadOnlyList<IReadOnlyList<Guid>> SealedComponents { get; init; }

    public required IReadOnlyList<Guid> OrphanOpeningIds { get; init; }

    public bool IsSpaceSealed(Guid spaceId) =>
        !VentingToExterior.Contains(spaceId);
}
