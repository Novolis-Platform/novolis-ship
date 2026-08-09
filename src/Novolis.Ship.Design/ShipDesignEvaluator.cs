using Novolis._3D;
using Novolis.Cad.Evaluation;
using Novolis.Cad.Primitives;
using Novolis.Cad.SceneBridge;
using Novolis.Cad.SceneBridge.Tessellation;
using Novolis.Math.Geometry;

namespace Novolis.Ship.Design;

public sealed record ShipObjectMesh(ShipObjectId ObjectId, string Kind, string Name, EditableMesh Mesh);

public sealed class ShipDesignEvaluationResult
{
    public required IReadOnlyList<ShipObjectMesh> ObjectMeshes { get; init; }
    public required SceneDocument Scene { get; init; }
}

/// <summary>
/// Evaluates per-object CadDocuments, applies structural cutouts via mesh boolean,
/// and composes a <see cref="SceneDocument"/>.
/// </summary>
public static class ShipDesignEvaluator
{
    public static ShipDesignEvaluationResult Evaluate(ShipDesign design)
    {
        ArgumentNullException.ThrowIfNull(design);
        var evaluator = new CadModelEvaluator();
        var meshes = new Dictionary<Guid, EditableMesh>();
        var meta = new Dictionary<Guid, (string Kind, string Name)>();

        foreach (var (id, geom, kind) in design.GeometricObjects())
        {
            var mesh = EvaluateObjectMesh(evaluator, geom);
            if (mesh is null)
                continue;
            meshes[id.Value] = mesh;
            meta[id.Value] = (kind, geom.Name);
        }

        // Apply cutouts: host = host − source volume.
        foreach (var cut in design.Cutouts)
        {
            if (!meshes.TryGetValue(cut.HostId.Value, out var host))
                continue;
            if (!meshes.TryGetValue(cut.SourceId.Value, out var source))
            {
                // Source may be an opening/passage with only aperture geometry — still try.
                continue;
            }

            meshes[cut.HostId.Value] = MeshBoolean.Apply(host, source, MeshBooleanKind.Difference);
        }

        var scene = new SceneDocument
        {
            Name = design.Ship.Name,
            Generator = "Novolis.Ship.Design",
            CreatedAt = DateTimeOffset.UtcNow,
            ModifiedAt = DateTimeOffset.UtcNow,
        };
        var root = new GroupNode { Name = "Ship" };
        scene.Nodes.Add(root);

        var objectMeshes = new List<ShipObjectMesh>();
        foreach (var (guid, mesh) in meshes)
        {
            var (kind, name) = meta.TryGetValue(guid, out var m) ? m : ("object", guid.ToString("N")[..8]);
            var node = new MeshNode
            {
                Name = $"{kind}:{name}",
                ParentId = root.Id,
                Primitive = MeshPrimitiveKind.Box,
            };
            MeshEditBake.WriteBaked(node, mesh);
            scene.Nodes.Add(node);
            objectMeshes.Add(new ShipObjectMesh(ShipObjectId.From(guid), kind, name, mesh));
        }

        return new ShipDesignEvaluationResult
        {
            ObjectMeshes = objectMeshes,
            Scene = scene,
        };
    }

    /// <summary>Evaluate a single object geometry, preferring CadModelEvaluator then SceneBridge tessellation.</summary>
    public static EditableMesh? EvaluateObjectMesh(CadModelEvaluator evaluator, CadDocument geometry)
    {
        ArgumentNullException.ThrowIfNull(evaluator);
        ArgumentNullException.ThrowIfNull(geometry);

        var cache = evaluator.Evaluate(geometry);
        if (cache.ModeledMeshes.Count > 0)
            return ConcatAll(cache.ModeledMeshes.Values);
        if (cache.CadMeshes.Count > 0)
            return ConcatAll(cache.CadMeshes.Values);
        if (cache.Instances.Count > 0)
        {
            EditableMesh? acc = null;
            foreach (var inst in cache.Instances)
            {
                if (inst.Mesh is null)
                    continue;
                acc = acc is null ? inst.Mesh.Clone() : MeshBoolean.Concat(acc, inst.Mesh);
            }

            if (acc is not null)
                return acc;
        }

        // Fallback: leaf tessellation via SceneBridge (matches CadSceneBridge path).
        EditableMesh? leaf = null;
        foreach (var entity in geometry.Entities)
        {
            var mesh = entity.Kind.Equals("space", StringComparison.OrdinalIgnoreCase)
                ? CadSpaceTessellator.TryTessellate(entity)
                : CadEntityTessellator.TryTessellate(entity);
            if (mesh is null)
                continue;
            leaf = leaf is null ? mesh : MeshBoolean.Concat(leaf, mesh);
        }

        return leaf;
    }

    public static SceneDocument ToSceneDocumentViaBridge(ShipDesign design)
    {
        var flat = ShipCadProjector.ToCadDocument(design);
        return CadSceneBridge.ToSceneDocument(flat);
    }

    private static EditableMesh ConcatAll(IEnumerable<EditableMesh> meshes)
    {
        EditableMesh? acc = null;
        foreach (var m in meshes)
            acc = acc is null ? m.Clone() : MeshBoolean.Concat(acc, m);
        return acc ?? new EditableMesh();
    }
}
