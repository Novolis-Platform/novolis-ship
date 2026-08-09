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

/// <summary>Standing-eye waypoint for a walk tour.</summary>
public sealed record ShipWalkWaypoint(
    Vector3 Eye,
    Vector3 Look,
    Guid SpaceId,
    Guid? OpeningId,
    string Label);

/// <summary>
/// Walkable circulation: spaces linked by <b>open</b> hatches (not airtight topology).
/// Waypoints stay inside space footprints so the eye does not pass through walls.
/// </summary>
public static class ShipWalk
{
    public const float DefaultEyeHeight = 1.55f;
    public const float DefaultInset = 0.4f;
    public const float DefaultStepMeters = 0.8f;

    public static IReadOnlyList<ShipWalkEdge> BuildOpenEdges(CadDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        var spaces = ShipCad.Spaces(document).ToList();
        var byNameDeck = spaces
            .Where(s => s.Name is not null)
            .GroupBy(s => (Name: s.Name!, s.Deck))
            .ToDictionary(g => g.Key, g => g.First());

        var edges = new List<ShipWalkEdge>();
        foreach (var opening in ShipCad.Openings(document))
        {
            if (ShipCad.GetLeafState(opening) != ShipLeafState.Open)
                continue;
            if (ShipCad.GetPressureClass(opening) == ShipPressureClass.Vacuum
                || ShipCad.IsVacuumAssisted(opening))
                continue; // never walk through vacuum / exterior shell hatches

            var connects = ShipCad.GetOpeningConnects(opening);
            if (connects.Count < 2)
                continue;

            var aName = connects[0];
            var bName = connects[1];
            if (string.Equals(aName, "SPACE", StringComparison.OrdinalIgnoreCase)
                || string.Equals(bName, "SPACE", StringComparison.OrdinalIgnoreCase))
                continue;

            byNameDeck.TryGetValue((aName, opening.Deck), out var a);
            byNameDeck.TryGetValue((bName, opening.Deck), out var b);
            // Continuous voids (HOLD/ENG atrium) may live on a different deck index.
            a ??= spaces.FirstOrDefault(s =>
                string.Equals(s.Name, aName, StringComparison.OrdinalIgnoreCase));
            b ??= spaces.FirstOrDefault(s =>
                string.Equals(s.Name, bName, StringComparison.OrdinalIgnoreCase));

            if (a is null || b is null)
                continue;

            var portal = FootprintCenter(opening);
            portal = new Vector3(portal.X, FloorY(a) + DefaultEyeHeight * 0.15f, portal.Z);
            edges.Add(new ShipWalkEdge(a.Id, b.Id, opening.Id, portal, opening.Name));
            edges.Add(new ShipWalkEdge(b.Id, a.Id, opening.Id, portal, opening.Name));
        }

        return edges;
    }

    public static bool TryFindSpacePath(
        CadDocument document,
        Guid startSpaceId,
        Guid goalSpaceId,
        out List<Guid> spacePath,
        out List<ShipWalkEdge> usedEdges)
    {
        spacePath = [];
        usedEdges = [];
        var edges = BuildOpenEdges(document);
        var adj = new Dictionary<Guid, List<ShipWalkEdge>>();
        foreach (var e in edges)
        {
            if (!adj.TryGetValue(e.FromSpaceId, out var list))
            {
                list = [];
                adj[e.FromSpaceId] = list;
            }

            list.Add(e);
        }

        var prev = new Dictionary<Guid, (Guid Prev, ShipWalkEdge Edge)>();
        var q = new Queue<Guid>();
        q.Enqueue(startSpaceId);
        var seen = new HashSet<Guid> { startSpaceId };
        while (q.Count > 0)
        {
            var n = q.Dequeue();
            if (n == goalSpaceId)
                break;
            if (!adj.TryGetValue(n, out var outs))
                continue;
            foreach (var e in outs)
            {
                if (!seen.Add(e.ToSpaceId))
                    continue;
                prev[e.ToSpaceId] = (n, e);
                q.Enqueue(e.ToSpaceId);
            }
        }

        if (startSpaceId != goalSpaceId && !prev.ContainsKey(goalSpaceId))
            return false;

        var path = new List<Guid> { goalSpaceId };
        var edgePath = new List<ShipWalkEdge>();
        var cur = goalSpaceId;
        while (cur != startSpaceId)
        {
            var (p, e) = prev[cur];
            edgePath.Add(e);
            path.Add(p);
            cur = p;
        }

        path.Reverse();
        edgePath.Reverse();
        spacePath = path;
        usedEdges = edgePath;
        return true;
    }

    /// <summary>
    /// Standing path through <paramref name="spacePath"/> / open portals.
    /// Eye samples are clamped inside each space footprint (inset) so they never clip walls.
    /// </summary>
    public static List<ShipWalkWaypoint> BuildStandingPath(
        CadDocument document,
        IReadOnlyList<Guid> spacePath,
        IReadOnlyList<ShipWalkEdge> usedEdges,
        float eyeHeight = DefaultEyeHeight,
        float stepMeters = DefaultStepMeters,
        float inset = DefaultInset)
    {
        ArgumentNullException.ThrowIfNull(document);
        var byId = document.Entities.ToDictionary(e => e.Id);
        var waypoints = new List<ShipWalkWaypoint>();
        if (spacePath.Count == 0)
            return waypoints;

        Vector3 StandingIn(CadEntity space, Vector3 xzHint)
        {
            var floor = FloorY(space);
            var p = new Vector3(xzHint.X, floor + eyeHeight, xzHint.Z);
            return ClampToSpace(space, p, inset);
        }

        var start = byId[spacePath[0]];
        var cursor = StandingIn(start, SpaceCentroid(start));
        waypoints.Add(new ShipWalkWaypoint(cursor, cursor + ForwardHint(start), start.Id, null, start.Name ?? "start"));

        for (var i = 0; i < usedEdges.Count; i++)
        {
            var edge = usedEdges[i];
            var from = byId[edge.FromSpaceId];
            var to = byId[edge.ToSpaceId];
            var portalFrom = StandingIn(from, edge.Portal);
            AppendSegment(waypoints, cursor, portalFrom, from, edge.OpeningId, $"to:{edge.OpeningName}", stepMeters);
            cursor = portalFrom;

            var portalTo = StandingIn(to, edge.Portal);
            // Cross the hatch — short lerp between clamped portals (gap is the opening clear).
            AppendSegment(waypoints, cursor, portalTo, to, edge.OpeningId, $"thru:{edge.OpeningName}", stepMeters * 0.5f);
            cursor = portalTo;

            var roomAim = StandingIn(to, SpaceCentroid(to));
            AppendSegment(waypoints, cursor, roomAim, to, null, to.Name ?? "room", stepMeters);
            cursor = roomAim;
        }

        return waypoints;
    }

    /// <summary>
    /// Enter at HOLD aft (rear), then walk every reachable <b>open</b> hatch (edge-covering tour).
    /// Eye samples stay clamped inside space footprints.
    /// </summary>
    public static List<ShipWalkWaypoint> BuildDeckMinusOneAftTour(
        CadDocument document,
        float eyeHeight = DefaultEyeHeight,
        float stepMeters = DefaultStepMeters)
    {
        ArgumentNullException.ThrowIfNull(document);
        var hold = ShipCad.Spaces(document).FirstOrDefault(s =>
            string.Equals(s.Name, "HOLD", StringComparison.OrdinalIgnoreCase));
        if (hold is null)
            return [];

        return BuildOpenHatchCoverTour(document, hold.Id, eyeHeight, stepMeters, startAtAft: true);
    }

    /// <summary>
    /// Cover every open hatch edge reachable from <paramref name="startSpaceId"/> (greedy nearest unused edge).
    /// </summary>
    public static List<ShipWalkWaypoint> BuildOpenHatchCoverTour(
        CadDocument document,
        Guid startSpaceId,
        float eyeHeight = DefaultEyeHeight,
        float stepMeters = DefaultStepMeters,
        bool startAtAft = false)
    {
        ArgumentNullException.ThrowIfNull(document);
        var byId = document.Entities.ToDictionary(e => e.Id);
        if (!byId.TryGetValue(startSpaceId, out var startSpace))
            return [];

        var allEdges = BuildOpenEdges(document);
        var undirected = new HashSet<Guid>();
        foreach (var e in allEdges)
            undirected.Add(e.OpeningId);

        var visitedOpenings = new HashSet<Guid>();
        var waypoints = new List<ShipWalkWaypoint>();

        BoundsOf(startSpace, out var smin, out var smax);
        var startHint = startAtAft
            ? new Vector3((smin.X + smax.X) * 0.5f, FloorY(startSpace) + eyeHeight, System.Math.Min(smin.Z, smax.Z) + DefaultInset)
            : SpaceCentroid(startSpace);
        var cursor = ClampToSpace(startSpace, startHint, DefaultInset);
        waypoints.Add(new ShipWalkWaypoint(
            cursor,
            cursor + new Vector3(0f, 0f, 3f),
            startSpace.Id,
            null,
            startSpace.Name ?? "start"));

        var currentId = startSpaceId;
        // Safety bound: each opening crossed at most twice (there-and-back).
        for (var guard = 0; guard < undirected.Count * 4 + 8; guard++)
        {
            ShipWalkEdge? next = null;
            // Prefer unused opening from current space.
            foreach (var e in allEdges.Where(e => e.FromSpaceId == currentId))
            {
                if (visitedOpenings.Contains(e.OpeningId))
                    continue;
                next = e;
                break;
            }

            if (next is null)
            {
                // BFS to a space that still has an unused open hatch.
                if (!TryFindNearestUnused(allEdges, currentId, visitedOpenings, out var viaPath, out var viaEdges)
                    || viaEdges.Count == 0)
                    break;

                var hop = BuildStandingPath(document, viaPath, viaEdges, eyeHeight, stepMeters);
                if (hop.Count > 1)
                    waypoints.AddRange(hop.Skip(1));
                currentId = viaPath[^1];
                continue;
            }

            var edge = next;
            visitedOpenings.Add(edge.OpeningId);
            var from = byId[edge.FromSpaceId];
            var to = byId[edge.ToSpaceId];
            var segment = BuildStandingPath(document, [from.Id, to.Id], [edge], eyeHeight, stepMeters);
            if (segment.Count > 1)
                waypoints.AddRange(segment.Skip(1));
            currentId = to.Id;

            if (visitedOpenings.Count >= undirected.Count)
                break;
        }

        // Finish with a corridor stroll toward bow on CORR_P @ −1 when present.
        var corr = ShipCad.Spaces(document)
            .FirstOrDefault(s => s.Name == "CORR_P" && s.Deck == -1)
            ?? ShipCad.Spaces(document).FirstOrDefault(s => s.Name == "CORR_P");
        if (corr is not null)
        {
            if (currentId != corr.Id && TryFindSpacePath(document, currentId, corr.Id, out var toCorr, out var corrEdges))
            {
                var hop = BuildStandingPath(document, toCorr, corrEdges, eyeHeight, stepMeters);
                if (hop.Count > 1)
                    waypoints.AddRange(hop.Skip(1));
                currentId = corr.Id;
            }

            if (currentId == corr.Id)
                AppendCorridorStroll(waypoints, corr, eyeHeight, stepMeters);
        }

        return waypoints;
    }

    private static bool TryFindNearestUnused(
        IReadOnlyList<ShipWalkEdge> allEdges,
        Guid fromSpaceId,
        HashSet<Guid> visitedOpenings,
        out List<Guid> spacePath,
        out List<ShipWalkEdge> usedEdges)
    {
        spacePath = [];
        usedEdges = [];
        var adj = new Dictionary<Guid, List<ShipWalkEdge>>();
        foreach (var e in allEdges)
        {
            if (!adj.TryGetValue(e.FromSpaceId, out var list))
            {
                list = [];
                adj[e.FromSpaceId] = list;
            }

            list.Add(e);
        }

        var q = new Queue<Guid>();
        var prev = new Dictionary<Guid, (Guid Prev, ShipWalkEdge Edge)>();
        q.Enqueue(fromSpaceId);
        var seen = new HashSet<Guid> { fromSpaceId };
        Guid? goal = null;
        while (q.Count > 0)
        {
            var n = q.Dequeue();
            if (adj.TryGetValue(n, out var outs))
            {
                if (outs.Any(e => !visitedOpenings.Contains(e.OpeningId)))
                {
                    goal = n;
                    break;
                }

                foreach (var e in outs)
                {
                    if (!seen.Add(e.ToSpaceId))
                        continue;
                    prev[e.ToSpaceId] = (n, e);
                    q.Enqueue(e.ToSpaceId);
                }
            }
        }

        if (goal is null)
            return false;

        var path = new List<Guid> { goal.Value };
        var edges = new List<ShipWalkEdge>();
        var cur = goal.Value;
        while (cur != fromSpaceId)
        {
            var (p, e) = prev[cur];
            edges.Add(e);
            path.Add(p);
            cur = p;
        }

        path.Reverse();
        edges.Reverse();
        spacePath = path;
        usedEdges = edges;
        return true;
    }

    private static void AppendCorridorStroll(
        List<ShipWalkWaypoint> waypoints,
        CadEntity corr,
        float eyeHeight,
        float stepMeters)
    {
        var floor = FloorY(corr);
        BoundsOf(corr, out var min, out var max);
        var yCl = System.Math.Clamp((min.X + max.X) * 0.5f, min.X + DefaultInset, max.X - DefaultInset);
        var zAft = System.Math.Min(min.Z, max.Z) + DefaultInset;
        var zBow = System.Math.Max(min.Z, max.Z) - DefaultInset;
        var z = waypoints.Count > 0
            ? System.Math.Clamp(waypoints[^1].Eye.Z, System.Math.Min(zAft, zBow), System.Math.Max(zAft, zBow))
            : zAft;
        var dir = zBow >= z ? 1 : -1;
        var targetZ = dir > 0 ? zBow : zAft;
        while ((dir > 0 && z < targetZ - 0.05f) || (dir < 0 && z > targetZ + 0.05f))
        {
            z += dir * stepMeters;
            if (dir > 0 && z > targetZ)
                z = targetZ;
            if (dir < 0 && z < targetZ)
                z = targetZ;
            var eye = ClampToSpace(corr, new Vector3(yCl, floor + eyeHeight, z), DefaultInset);
            waypoints.Add(new ShipWalkWaypoint(eye, eye + new Vector3(0f, 0f, dir * 3f), corr.Id, null, "CORR_P"));
        }
    }

    public static Vector3 FootprintCenter(CadEntity opening)
    {
        if (opening.Footprint is not { Count: > 0 } fp)
            return Vector3.Zero;
        var sum = Vector3.Zero;
        var n = 0;
        foreach (var p in fp)
        {
            if (p.Length < 3)
                continue;
            sum += new Vector3(p[0], p[1], p[2]);
            n++;
        }

        return n == 0 ? Vector3.Zero : sum / n;
    }

    public static Vector3 SpaceCentroid(CadEntity space)
    {
        if (space.Points is not { Count: > 0 } pts)
            return Vector3.Zero;
        var sum = Vector3.Zero;
        foreach (var p in pts)
            sum += new Vector3(p[0], p[1], p[2]);
        return sum / pts.Count;
    }

    public static float FloorY(CadEntity space)
    {
        if (space.Points is not { Count: > 0 } pts)
            return 0f;
        return pts.Min(p => p.Length >= 2 ? p[1] : 0f);
    }

    public static void BoundsOf(CadEntity space, out Vector3 min, out Vector3 max)
    {
        min = new Vector3(float.MaxValue);
        max = new Vector3(float.MinValue);
        if (space.Points is not { Count: > 0 } pts)
        {
            min = max = Vector3.Zero;
            return;
        }

        foreach (var p in pts)
        {
            var v = new Vector3(p[0], p.Length > 1 ? p[1] : 0f, p.Length > 2 ? p[2] : 0f);
            min = Vector3.Min(min, v);
            max = Vector3.Max(max, v);
        }
    }

    public static Vector3 ClampToSpace(CadEntity space, Vector3 p, float inset)
    {
        BoundsOf(space, out var min, out var max);
        var loX = min.X + inset;
        var hiX = max.X - inset;
        var loZ = min.Z + inset;
        var hiZ = max.Z - inset;
        if (loX > hiX)
            loX = hiX = (min.X + max.X) * 0.5f;
        if (loZ > hiZ)
            loZ = hiZ = (min.Z + max.Z) * 0.5f;
        var floor = min.Y;
        var ceil = floor + System.Math.Max(1.8f, space.Height > 0 ? space.Height : 3.2f);
        return new Vector3(
            System.Math.Clamp(p.X, loX, hiX),
            System.Math.Clamp(p.Y, floor + 1.2f, ceil - 0.35f),
            System.Math.Clamp(p.Z, loZ, hiZ));
    }

    private static Vector3 ForwardHint(CadEntity space)
    {
        BoundsOf(space, out var min, out var max);
        var alongZ = MathF.Abs(max.Z - min.Z) >= MathF.Abs(max.X - min.X);
        return alongZ ? new Vector3(0f, 0f, 3f) : new Vector3(3f, 0f, 0f);
    }

    private static void AppendSegment(
        List<ShipWalkWaypoint> waypoints,
        Vector3 from,
        Vector3 to,
        CadEntity space,
        Guid? openingId,
        string label,
        float step)
    {
        var delta = to - from;
        var dist = delta.Length();
        if (dist < 1e-3f)
        {
            waypoints.Add(new ShipWalkWaypoint(to, to + ForwardHint(space), space.Id, openingId, label));
            return;
        }

        var steps = System.Math.Max(1, (int)MathF.Ceiling(dist / System.Math.Max(0.25f, step)));
        for (var i = 1; i <= steps; i++)
        {
            var t = i / (float)steps;
            var eye = ClampToSpace(space, Vector3.Lerp(from, to, t), DefaultInset);
            var look = eye + Vector3.Normalize(delta) * 2.5f;
            look.Y = eye.Y;
            waypoints.Add(new ShipWalkWaypoint(eye, look, space.Id, openingId, label));
        }
    }
}
