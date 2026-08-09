namespace Novolis.Ship.Design;

/// <summary>
/// Shared compartment boundaries map to one physical bulkhead (baseline §12).
/// Detects coincident path edges so authoring does not duplicate bulkheads.
/// </summary>
public static class CompartmentBoundaryResolver
{
    public sealed record SharedEdge(CompartmentId A, CompartmentId B, float[] FromXz, float[] ToXz);

    public static IReadOnlyList<SharedEdge> FindSharedEdges(ShipDesign design, float toleranceM = 0.05f)
    {
        ArgumentNullException.ThrowIfNull(design);
        var edges = new List<(CompartmentId Id, float[] A, float[] B)>();
        foreach (var c in design.Compartments)
        {
            foreach (var (a, b) in ExtractEdges(c.Geometry))
                edges.Add((c.Id, a, b));
        }

        var shared = new List<SharedEdge>();
        for (var i = 0; i < edges.Count; i++)
        {
            for (var j = i + 1; j < edges.Count; j++)
            {
                if (edges[i].Id.Value == edges[j].Id.Value)
                    continue;
                if (!SameUndirectedEdge(edges[i].A, edges[i].B, edges[j].A, edges[j].B, toleranceM))
                    continue;
                shared.Add(new SharedEdge(edges[i].Id, edges[j].Id, edges[i].A, edges[i].B));
            }
        }

        return shared;
    }

    private static IEnumerable<(float[] A, float[] B)> ExtractEdges(Novolis.Cad.Primitives.CadDocument geometry)
    {
        foreach (var e in geometry.Entities)
        {
            if (e.A is { Length: >= 3 } && e.B is { Length: >= 3 })
                yield return ([e.A[0], e.A[2]], [e.B[0], e.B[2]]);
            if (e.Points is { Count: >= 2 })
            {
                for (var i = 0; i < e.Points.Count - 1; i++)
                {
                    var a = e.Points[i];
                    var b = e.Points[i + 1];
                    if (a.Length >= 3 && b.Length >= 3)
                        yield return ([a[0], a[2]], [b[0], b[2]]);
                }

                // Close the boundary loop for enclosed compartment polygons.
                var first = e.Points[0];
                var last = e.Points[^1];
                if (first.Length >= 3 && last.Length >= 3
                    && (MathF.Abs(first[0] - last[0]) > 1e-4f || MathF.Abs(first[2] - last[2]) > 1e-4f))
                {
                    yield return ([last[0], last[2]], [first[0], first[2]]);
                }
            }
        }
    }

    private static bool SameUndirectedEdge(float[] a0, float[] a1, float[] b0, float[] b1, float tol)
    {
        return (Near(a0, b0, tol) && Near(a1, b1, tol)) || (Near(a0, b1, tol) && Near(a1, b0, tol));
    }

    private static bool Near(float[] a, float[] b, float tol) =>
        MathF.Abs(a[0] - b[0]) <= tol && MathF.Abs(a[1] - b[1]) <= tol;
}
