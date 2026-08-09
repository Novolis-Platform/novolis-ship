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

/// <summary>Builds airtight topology from a Cad ship document.</summary>
public static class ShipTopology
{
    /// <summary>
    /// Graph: spaces are nodes. Closed airtight openings between two spaces (via host wall sides)
    /// are sealed edges. Open or non-airtight openings, or openings without a host wall, vent
    /// touching spaces to exterior. A closed airtight hatch on a wall touched by only one space
    /// is treated as an exterior hatch (vents that space).
    /// </summary>
    public static ShipTopologyResult Analyze(CadDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        var spaces = ShipCad.Spaces(document).ToList();
        var spaceIds = spaces.Select(s => s.Id).ToList();
        var byId = document.Entities.ToDictionary(e => e.Id);

        var sealedAdj = spaceIds.ToDictionary(id => id, _ => new HashSet<Guid>());
        var venting = new HashSet<Guid>();
        var orphans = new List<Guid>();
        var wallSpaces = BuildWallSpaceTouchMap(document, spaces);

        foreach (var opening in ShipCad.Openings(document))
        {
            if (opening.HostWallId is not { } wallId || !byId.ContainsKey(wallId))
            {
                orphans.Add(opening.Id);
                foreach (var s in spaces.Where(s => s.Deck == opening.Deck))
                    venting.Add(s.Id);
                continue;
            }

            wallSpaces.TryGetValue(wallId, out var touching);
            touching ??= [];

            var airtightClosed = ShipCad.IsAirtightWhenClosed(opening)
                                 && ShipCad.GetLeafState(opening) == ShipLeafState.Closed;

            if (!airtightClosed)
            {
                foreach (var sid in touching)
                    venting.Add(sid);
                if (touching.Count == 0)
                {
                    foreach (var s in spaces.Where(s => s.Deck == opening.Deck))
                        venting.Add(s.Id);
                }

                continue;
            }

            var list = touching.ToList();
            for (var i = 0; i < list.Count; i++)
            {
                for (var j = i + 1; j < list.Count; j++)
                {
                    sealedAdj[list[i]].Add(list[j]);
                    sealedAdj[list[j]].Add(list[i]);
                }
            }

            // Exterior hatch: only one compartment on the host wall.
            if (list.Count <= 1)
            {
                foreach (var sid in list)
                    venting.Add(sid);
            }
        }

        var sealedIds = spaceIds.Where(id => !venting.Contains(id)).ToHashSet();
        var visited = new HashSet<Guid>();
        var components = new List<IReadOnlyList<Guid>>();
        foreach (var start in sealedIds)
        {
            if (!visited.Add(start))
                continue;
            var comp = new List<Guid>();
            var q = new Queue<Guid>();
            q.Enqueue(start);
            while (q.Count > 0)
            {
                var n = q.Dequeue();
                comp.Add(n);
                foreach (var next in sealedAdj[n])
                {
                    if (!sealedIds.Contains(next) || !visited.Add(next))
                        continue;
                    q.Enqueue(next);
                }
            }

            components.Add(comp);
        }

        return new ShipTopologyResult
        {
            SpaceIds = spaceIds,
            VentingToExterior = venting,
            SealedComponents = components,
            OrphanOpeningIds = orphans,
        };
    }

    /// <summary>Refresh <see cref="CadSpaceFlags.Enclosed"/> from topology (hollow left as-is).</summary>
    public static void ApplySpaceFlags(CadDocument document, ShipTopologyResult? result = null)
    {
        ArgumentNullException.ThrowIfNull(document);
        result ??= Analyze(document);
        foreach (var space in ShipCad.Spaces(document))
        {
            space.Flags ??= new CadSpaceFlags();
            space.Flags.Enclosed = result.IsSpaceSealed(space.Id);
        }
    }

    private static Dictionary<Guid, HashSet<Guid>> BuildWallSpaceTouchMap(
        CadDocument document,
        List<CadEntity> spaces)
    {
        var map = new Dictionary<Guid, HashSet<Guid>>();
        foreach (var wall in ShipCad.Walls(document))
        {
            var set = new HashSet<Guid>();
            foreach (var space in spaces)
            {
                if (space.Deck != wall.Deck)
                    continue;
                if (WallTouchesSpace(wall, space))
                    set.Add(space.Id);
            }

            map[wall.Id] = set;
        }

        return map;
    }

    private static bool WallTouchesSpace(CadEntity wall, CadEntity space)
    {
        if (space.Points is not { Count: >= 3 } pts)
            return false;
        BoundsOf(pts, out var sMin, out var sMax);
        const float eps = 0.35f;
        sMin = new Vector3(sMin.X - eps, sMin.Y, sMin.Z - eps);
        sMax = new Vector3(sMax.X + eps, sMax.Y, sMax.Z + eps);

        if (wall.A is { Length: >= 3 } && wall.B is { Length: >= 3 })
        {
            var a = CadVec.To(wall.A);
            var b = CadVec.To(wall.B);
            return PointInAabbXZ(a, sMin, sMax) || PointInAabbXZ(b, sMin, sMax)
                   || SegmentHitsAabbXZ(a, b, sMin, sMax);
        }

        if (wall.Points is { Count: >= 2 } wpts)
        {
            foreach (var wp in wpts)
            {
                if (PointInAabbXZ(CadVec.To(wp), sMin, sMax))
                    return true;
            }
        }

        return false;
    }

    private static void BoundsOf(List<float[]> pts, out Vector3 min, out Vector3 max)
    {
        min = new Vector3(float.MaxValue);
        max = new Vector3(float.MinValue);
        foreach (var p in pts)
        {
            var v = CadVec.To(p);
            min = Vector3.Min(min, v);
            max = Vector3.Max(max, v);
        }
    }

    private static bool PointInAabbXZ(Vector3 p, Vector3 min, Vector3 max) =>
        p.X >= min.X && p.X <= max.X && p.Z >= min.Z && p.Z <= max.Z;

    private static bool SegmentHitsAabbXZ(Vector3 a, Vector3 b, Vector3 min, Vector3 max)
    {
        for (var i = 0; i <= 8; i++)
        {
            var p = Vector3.Lerp(a, b, i / 8f);
            if (PointInAabbXZ(p, min, max))
                return true;
        }

        return false;
    }
}
