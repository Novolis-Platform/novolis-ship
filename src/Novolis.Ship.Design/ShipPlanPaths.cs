using Novolis.Cad.Primitives;

namespace Novolis.Ship.Design;

/// <summary>XZ path helpers for architect plan authoring (bulkheads, rooms, passages).</summary>
public static class ShipPlanPaths
{
    public static List<float[]> ExtractPathXz(CadDocument geometry)
    {
        ArgumentNullException.ThrowIfNull(geometry);
        var path = new List<float[]>();
        foreach (var e in geometry.Entities)
        {
            if (e.Points is { Count: >= 2 })
            {
                foreach (var p in e.Points)
                {
                    if (p.Length >= 3)
                        path.Add([p[0], p[2]]);
                    else if (p.Length >= 2)
                        path.Add([p[0], p[1]]);
                }

                continue;
            }

            if (e.A is { Length: >= 3 })
                path.Add([e.A[0], e.A[2]]);
            if (e.B is { Length: >= 3 })
                path.Add([e.B[0], e.B[2]]);
        }

        return Deduplicate(path);
    }

    public static List<float[]> ExtractPolygonXz(CadDocument geometry)
    {
        var path = ExtractPathXz(geometry);
        if (path.Count >= 3)
        {
            var first = path[0];
            var last = path[^1];
            if (MathF.Abs(first[0] - last[0]) > 1e-4f || MathF.Abs(first[1] - last[1]) > 1e-4f)
                path.Add([first[0], first[1]]);
        }

        return path;
    }

    /// <summary>Point along a polyline at normalized t in [0,1].</summary>
    public static float[] PointAlong(IReadOnlyList<float[]> pathXz, float t)
    {
        if (pathXz.Count == 0)
            return [0f, 0f];
        if (pathXz.Count == 1)
            return [pathXz[0][0], pathXz[0][1]];
        t = Clamp01(t);
        var lengths = new float[pathXz.Count - 1];
        var total = 0f;
        for (var i = 0; i < lengths.Length; i++)
        {
            var dx = pathXz[i + 1][0] - pathXz[i][0];
            var dz = pathXz[i + 1][1] - pathXz[i][1];
            lengths[i] = MathF.Sqrt(dx * dx + dz * dz);
            total += lengths[i];
        }

        if (total < 1e-6f)
            return [pathXz[0][0], pathXz[0][1]];
        var target = t * total;
        var acc = 0f;
        for (var i = 0; i < lengths.Length; i++)
        {
            if (acc + lengths[i] >= target || i == lengths.Length - 1)
            {
                var segT = lengths[i] < 1e-6f ? 0f : (target - acc) / lengths[i];
                segT = Clamp01(segT);
                return
                [
                    pathXz[i][0] + (pathXz[i + 1][0] - pathXz[i][0]) * segT,
                    pathXz[i][1] + (pathXz[i + 1][1] - pathXz[i][1]) * segT,
                ];
            }

            acc += lengths[i];
        }

        return [pathXz[^1][0], pathXz[^1][1]];
    }

    public static float DistancePointToSegment(float px, float pz, float ax, float az, float bx, float bz)
    {
        var dx = bx - ax;
        var dz = bz - az;
        var len2 = dx * dx + dz * dz;
        if (len2 < 1e-12f)
            return MathF.Sqrt((px - ax) * (px - ax) + (pz - az) * (pz - az));
        var t = Clamp01(((px - ax) * dx + (pz - az) * dz) / len2);
        var qx = ax + t * dx;
        var qz = az + t * dz;
        return MathF.Sqrt((px - qx) * (px - qx) + (pz - qz) * (pz - qz));
    }

    public static bool TryHitBulkhead(
        ShipDesign design,
        DeckId deckId,
        float x,
        float z,
        float toleranceM,
        out BulkheadDesign? bulkhead,
        out float tAlong)
    {
        bulkhead = null;
        tAlong = 0.5f;
        var best = toleranceM;
        BulkheadDesign? hit = null;
        var hitT = 0.5f;
        foreach (var b in design.Bulkheads.Where(b => b.DeckId?.Value == deckId.Value || b.IsPrimary))
        {
            var path = ExtractPathXz(b.Geometry);
            if (path.Count < 2)
                continue;
            var lengths = new float[path.Count - 1];
            var total = 0f;
            for (var i = 0; i < lengths.Length; i++)
            {
                var dx = path[i + 1][0] - path[i][0];
                var dz = path[i + 1][1] - path[i][1];
                lengths[i] = MathF.Sqrt(dx * dx + dz * dz);
                total += lengths[i];
            }

            var acc = 0f;
            for (var i = 0; i < path.Count - 1; i++)
            {
                var d = DistancePointToSegment(x, z, path[i][0], path[i][1], path[i + 1][0], path[i + 1][1]);
                if (d <= best)
                {
                    best = d;
                    hit = b;
                    var segLen = lengths[i];
                    var dx = path[i + 1][0] - path[i][0];
                    var dz = path[i + 1][1] - path[i][1];
                    var localT = segLen < 1e-6f
                        ? 0f
                        : Clamp01(((x - path[i][0]) * dx + (z - path[i][1]) * dz) / (segLen * segLen));
                    hitT = total < 1e-6f ? 0.5f : (acc + localT * segLen) / total;
                }

                acc += lengths[i];
            }
        }

        if (hit is null)
            return false;
        bulkhead = hit;
        tAlong = hitT;
        return true;
    }

    /// <summary>Normalized parameter of the closest point on a polyline (0..1).</summary>
    public static float NearestParameter(IReadOnlyList<float[]> pathXz, float x, float z)
    {
        if (pathXz.Count < 2)
            return 0.5f;
        var lengths = new float[pathXz.Count - 1];
        var total = 0f;
        for (var i = 0; i < lengths.Length; i++)
        {
            var dx = pathXz[i + 1][0] - pathXz[i][0];
            var dz = pathXz[i + 1][1] - pathXz[i][1];
            lengths[i] = MathF.Sqrt(dx * dx + dz * dz);
            total += lengths[i];
        }

        if (total < 1e-6f)
            return 0.5f;

        var bestD = float.MaxValue;
        var bestT = 0.5f;
        var acc = 0f;
        for (var i = 0; i < pathXz.Count - 1; i++)
        {
            var ax = pathXz[i][0];
            var az = pathXz[i][1];
            var bx = pathXz[i + 1][0];
            var bz = pathXz[i + 1][1];
            var dx = bx - ax;
            var dz = bz - az;
            var len2 = dx * dx + dz * dz;
            var localT = len2 < 1e-12f ? 0f : Clamp01(((x - ax) * dx + (z - az) * dz) / len2);
            var qx = ax + localT * dx;
            var qz = az + localT * dz;
            var d = MathF.Sqrt((x - qx) * (x - qx) + (z - qz) * (z - qz));
            if (d < bestD)
            {
                bestD = d;
                bestT = (acc + localT * lengths[i]) / total;
            }

            acc += lengths[i];
        }

        return bestT;
    }

    private static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;

    private static List<float[]> Deduplicate(List<float[]> path)
    {
        if (path.Count <= 1)
            return path;
        var result = new List<float[]> { path[0] };
        for (var i = 1; i < path.Count; i++)
        {
            var prev = result[^1];
            var cur = path[i];
            if (MathF.Abs(prev[0] - cur[0]) > 1e-4f || MathF.Abs(prev[1] - cur[1]) > 1e-4f)
                result.Add(cur);
        }

        return result;
    }
}
