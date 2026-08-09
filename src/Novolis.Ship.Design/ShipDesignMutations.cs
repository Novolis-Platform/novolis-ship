using Novolis.Cad.Primitives;
using Novolis.Ship.Structure;

namespace Novolis.Ship.Design;

/// <summary>Object-first authoring helpers (passages, openings, compartments).</summary>
public static class ShipDesignMutations
{
    public static ShipDesign AddPassage(
        ShipDesign design,
        DeckId deckId,
        string name,
        IReadOnlyList<float[]> pathXz,
        float widthM,
        float heightM,
        string clearanceClass = "personnel")
    {
        ArgumentNullException.ThrowIfNull(design);
        var deck = design.Decks.FirstOrDefault(d => d.Id.Value == deckId.Value)
            ?? throw new ArgumentException("Deck not found.", nameof(deckId));
        var elev = ShipLengths.ToMeters(deck.Elevation);
        var passage = new PassageDesign
        {
            Id = PassageId.New(),
            Name = name,
            DeckId = deckId,
            Width = ShipLengths.FromMeters(widthM),
            Height = ShipLengths.FromMeters(heightM),
            ClearanceClass = clearanceClass,
            Geometry = ShipGeometryBuilders.BuildPassageVolume(name, pathXz, widthM, heightM, elev, deck.Index),
        };
        var next = design with
        {
            Passages = design.Passages.Append(passage).ToList(),
            ModifiedAt = DateTimeOffset.UtcNow.ToString("O"),
        };
        return StructuralCutoutService.Regenerate(next);
    }

    public static ShipDesign AddCompartment(
        ShipDesign design,
        DeckId deckId,
        string name,
        IReadOnlyList<float[]> closedPolyXz,
        CompartmentKind kind = CompartmentKind.General)
    {
        ArgumentNullException.ThrowIfNull(design);
        var deck = design.Decks.FirstOrDefault(d => d.Id.Value == deckId.Value)
            ?? throw new ArgumentException("Deck not found.", nameof(deckId));
        var elev = ShipLengths.ToMeters(deck.Elevation);
        var height = design.Ship.HeightMeters / System.Math.Max(1, design.Ship.DeckCount) * 0.9f;
        var compartment = new CompartmentDesign
        {
            Id = CompartmentId.New(),
            Name = name,
            DeckId = deckId,
            Kind = kind,
            Geometry = ShipGeometryBuilders.BuildCompartmentBoundary(name, closedPolyXz, height, elev, deck.Index),
        };
        return design with
        {
            Compartments = design.Compartments.Append(compartment).ToList(),
            ModifiedAt = DateTimeOffset.UtcNow.ToString("O"),
        };
    }

    public static ShipDesign AddOpening(
        ShipDesign design,
        ShipObjectId hostId,
        string name,
        OpeningKind kind,
        float clearWidthM,
        float clearHeightM,
        float[] center)
    {
        ArgumentNullException.ThrowIfNull(design);
        var opening = new OpeningDesign
        {
            Id = OpeningId.New(),
            Name = name,
            HostId = hostId,
            Kind = kind,
            Geometry = ShipGeometryBuilders.BuildOpeningAperture(
                name,
                clearWidthM,
                clearHeightM,
                center,
                kind.ToString(),
                deckIndex: design.Bulkheads.FirstOrDefault(b => b.Id.Value == hostId.Value)?.DeckId is { } did
                    ? design.Decks.FirstOrDefault(d => d.Id.Value == did.Value)?.Index ?? 0
                    : 0),
        };
        var next = design with
        {
            Openings = design.Openings.Append(opening).ToList(),
            ModifiedAt = DateTimeOffset.UtcNow.ToString("O"),
        };
        return StructuralCutoutService.Regenerate(next);
    }

    public static ShipDesign AddBulkhead(
        ShipDesign design,
        DeckId deckId,
        string name,
        IReadOnlyList<float[]> pathXz,
        float thicknessM,
        float heightM,
        bool isPrimary = false) =>
        AddBulkheadPath(design, deckId, name, pathXz, thicknessM, heightM, isPrimary);

    /// <summary>Architect wall stroke → bulkhead on a deck.</summary>
    public static ShipDesign AddBulkheadPath(
        ShipDesign design,
        DeckId deckId,
        string name,
        IReadOnlyList<float[]> pathXz,
        float thicknessM,
        float heightM,
        bool isPrimary = false)
    {
        ArgumentNullException.ThrowIfNull(design);
        ArgumentNullException.ThrowIfNull(pathXz);
        if (pathXz.Count < 2)
            throw new ArgumentException("Bulkhead path needs at least 2 points.", nameof(pathXz));
        var deck = RequireDeck(design, deckId);
        var elev = ShipLengths.ToMeters(deck.Elevation);
        var material = StructuralMaterial(design);
        var bulkhead = new BulkheadDesign
        {
            Id = BulkheadId.New(),
            Name = name,
            Material = new MaterialId(material),
            Thickness = ShipLengths.FromMeters(thicknessM),
            Height = ShipLengths.FromMeters(heightM),
            DeckId = deckId,
            IsPrimary = isPrimary,
            Geometry = ShipGeometryBuilders.BuildBulkheadPath(
                name, pathXz, thicknessM, heightM, elev, material, deck.Index),
        };
        return design with
        {
            Bulkheads = design.Bulkheads.Append(bulkhead).ToList(),
            ModifiedAt = Now(),
        };
    }

    public static ShipDesign UpdateBulkheadPath(ShipDesign design, BulkheadId bulkheadId, IReadOnlyList<float[]> pathXz)
    {
        ArgumentNullException.ThrowIfNull(design);
        ArgumentNullException.ThrowIfNull(pathXz);
        if (pathXz.Count < 2)
            throw new ArgumentException("Bulkhead path needs at least 2 points.", nameof(pathXz));
        var bulkheads = design.Bulkheads.Select(b =>
        {
            if (b.Id.Value != bulkheadId.Value)
                return b;
            var deck = b.DeckId is { } did ? design.Decks.FirstOrDefault(d => d.Id.Value == did.Value) : null;
            var elev = deck is null ? 0f : ShipLengths.ToMeters(deck.Elevation);
            var deckIndex = deck?.Index ?? 0;
            return b with
            {
                Geometry = ShipGeometryBuilders.BuildBulkheadPath(
                    b.Name,
                    pathXz,
                    ShipLengths.ToMeters(b.Thickness),
                    ShipLengths.ToMeters(b.Height),
                    elev,
                    b.Material.Value,
                    deckIndex),
            };
        }).ToList();
        return design with { Bulkheads = bulkheads, ModifiedAt = Now() };
    }

    public static ShipDesign AppendBulkheadVertex(ShipDesign design, BulkheadId bulkheadId, float x, float z)
    {
        ArgumentNullException.ThrowIfNull(design);
        var bh = design.Bulkheads.FirstOrDefault(b => b.Id.Value == bulkheadId.Value)
            ?? throw new ArgumentException("Bulkhead not found.", nameof(bulkheadId));
        var path = ShipPlanPaths.ExtractPathXz(bh.Geometry);
        path.Add([x, z]);
        return UpdateBulkheadPath(design, bulkheadId, path);
    }

    /// <summary>Architect room polygon → compartment; ensures shared-edge bulkheads exist.</summary>
    public static ShipDesign AddCompartmentPolygon(
        ShipDesign design,
        DeckId deckId,
        string name,
        IReadOnlyList<float[]> closedPolyXz,
        CompartmentKind kind = CompartmentKind.General)
    {
        ArgumentNullException.ThrowIfNull(closedPolyXz);
        if (closedPolyXz.Count < 3)
            throw new ArgumentException("Compartment needs at least 3 points.", nameof(closedPolyXz));
        var next = AddCompartment(design, deckId, name, closedPolyXz, kind);
        return EnsureSharedBulkheads(next, deckId);
    }

    public static ShipDesign UpdateCompartmentPolygon(
        ShipDesign design,
        CompartmentId compartmentId,
        IReadOnlyList<float[]> closedPolyXz)
    {
        ArgumentNullException.ThrowIfNull(design);
        ArgumentNullException.ThrowIfNull(closedPolyXz);
        if (closedPolyXz.Count < 3)
            throw new ArgumentException("Compartment needs at least 3 points.", nameof(closedPolyXz));
        DeckId? deckId = null;
        var compartments = design.Compartments.Select(c =>
        {
            if (c.Id.Value != compartmentId.Value)
                return c;
            deckId = c.DeckId;
            var deck = RequireDeck(design, c.DeckId);
            var elev = ShipLengths.ToMeters(deck.Elevation);
            var height = design.Ship.HeightMeters / System.Math.Max(1, design.Ship.DeckCount) * 0.9f;
            return c with
            {
                Geometry = ShipGeometryBuilders.BuildCompartmentBoundary(
                    c.Name, closedPolyXz, height, elev, deck.Index),
            };
        }).ToList();
        var next = design with { Compartments = compartments, ModifiedAt = Now() };
        return deckId is { } d ? EnsureSharedBulkheads(next, d) : next;
    }

    /// <summary>Create missing non-primary bulkheads for shared compartment edges on a deck.</summary>
    public static ShipDesign EnsureSharedBulkheads(ShipDesign design, DeckId deckId)
    {
        ArgumentNullException.ThrowIfNull(design);
        var deck = RequireDeck(design, deckId);
        var elev = ShipLengths.ToMeters(deck.Elevation);
        var deckH = System.Math.Max(2.2f, design.Ship.HeightMeters / System.Math.Max(1, design.Ship.DeckCount) * 0.9f);
        var thickness = System.Math.Max(0.05f, design.Ship.HullThicknessMeters);
        var material = StructuralMaterial(design);
        var existing = design.Bulkheads
            .Where(b => b.DeckId?.Value == deckId.Value)
            .Select(b => ShipPlanPaths.ExtractPathXz(b.Geometry))
            .Where(p => p.Count >= 2)
            .ToList();
        var bulkheads = design.Bulkheads.ToList();
        foreach (var edge in CompartmentBoundaryResolver.FindSharedEdges(design))
        {
            var a = design.Compartments.FirstOrDefault(c => c.Id.Value == edge.A.Value);
            var b = design.Compartments.FirstOrDefault(c => c.Id.Value == edge.B.Value);
            if (a is null || b is null)
                continue;
            if (a.DeckId.Value != deckId.Value || b.DeckId.Value != deckId.Value)
                continue;
            float[][] path = [edge.FromXz, edge.ToXz];
            if (existing.Any(p => PathsMatch(p, path)))
                continue;
            var name = $"BH-Shared-{bulkheads.Count + 1}";
            bulkheads.Add(new BulkheadDesign
            {
                Id = BulkheadId.New(),
                Name = name,
                Material = new MaterialId(material),
                Thickness = ShipLengths.FromMeters(thickness),
                Height = ShipLengths.FromMeters(deckH),
                DeckId = deckId,
                IsPrimary = false,
                Geometry = ShipGeometryBuilders.BuildBulkheadPath(
                    name, path, thickness, deckH, elev, material, deck.Index),
            });
            existing.Add(path.ToList());
        }

        return design with { Bulkheads = bulkheads, ModifiedAt = Now() };
    }

    /// <summary>Door/opening on a bulkhead at normalized parameter t along its path.</summary>
    public static ShipDesign AddOpeningOnHost(
        ShipDesign design,
        BulkheadId hostId,
        string name,
        OpeningKind kind,
        float tAlong,
        float clearWidthM,
        float clearHeightM)
    {
        ArgumentNullException.ThrowIfNull(design);
        var host = design.Bulkheads.FirstOrDefault(b => b.Id.Value == hostId.Value)
            ?? throw new ArgumentException("Host bulkhead not found.", nameof(hostId));
        var path = ShipPlanPaths.ExtractPathXz(host.Geometry);
        var xz = ShipPlanPaths.PointAlong(path, tAlong);
        var deck = host.DeckId is { } did
            ? design.Decks.FirstOrDefault(d => d.Id.Value == did.Value)
            : null;
        var elev = deck is null ? 0f : ShipLengths.ToMeters(deck.Elevation);
        return AddOpening(
            design,
            host.Id.AsObject(),
            name,
            kind,
            clearWidthM,
            clearHeightM,
            [xz[0], elev + clearHeightM * 0.5f, xz[1]]);
    }

    private static DeckDesign RequireDeck(ShipDesign design, DeckId deckId) =>
        design.Decks.FirstOrDefault(d => d.Id.Value == deckId.Value)
        ?? throw new ArgumentException("Deck not found.", nameof(deckId));

    private static string StructuralMaterial(ShipDesign design) =>
        design.Ship.PrimaryStructuralMaterial.Value is { Length: > 0 } m
            ? m
            : design.Ship.HullMaterial.Value;

    private static bool PathsMatch(IReadOnlyList<float[]> a, IReadOnlyList<float[]> b, float tol = 0.08f)
    {
        if (a.Count < 2 || b.Count < 2)
            return false;
        var a0 = a[0];
        var a1 = a[^1];
        var b0 = b[0];
        var b1 = b[^1];
        return (Near(a0, b0, tol) && Near(a1, b1, tol)) || (Near(a0, b1, tol) && Near(a1, b0, tol));
    }

    private static bool Near(float[] a, float[] b, float tol) =>
        MathF.Abs(a[0] - b[0]) <= tol && MathF.Abs(a[1] - b[1]) <= tol;

    public static ShipDesign AddEquipment(
        ShipDesign design,
        string name,
        float[] center,
        float[] halfExtents,
        float massKg = 500f,
        int deckIndex = 0)
    {
        ArgumentNullException.ThrowIfNull(design);
        var equipment = new EquipmentDesign
        {
            Id = EquipmentId.New(),
            Name = name,
            MassKg = massKg,
            ServiceClearance = ShipLengths.FromMeters(0.6f),
            Geometry = ShipGeometryBuilders.BuildEquipmentEnvelope(name, center, halfExtents, massKg, deckIndex),
        };
        return design with
        {
            Equipment = design.Equipment.Append(equipment).ToList(),
            ModifiedAt = DateTimeOffset.UtcNow.ToString("O"),
        };
    }

    public static ShipDesign ReplaceObjectGeometry(ShipDesign design, ShipObjectId id, CadDocument geometry)
    {
        ArgumentNullException.ThrowIfNull(design);
        ArgumentNullException.ThrowIfNull(geometry);
        var clone = CloneDocument(geometry);
        var guid = id.Value;
        if (design.Hull.Id.Value == guid)
            return design with { Hull = design.Hull with { Geometry = clone }, ModifiedAt = Now() };
        if (design.Decks.Any(d => d.Id.Value == guid))
            return design with
            {
                Decks = design.Decks.Select(d => d.Id.Value == guid ? d with { Geometry = clone } : d).ToList(),
                ModifiedAt = Now(),
            };
        if (design.Frames.Any(f => f.Id.Value == guid))
            return design with
            {
                Frames = design.Frames.Select(f => f.Id.Value == guid ? f with { Geometry = clone } : f).ToList(),
                ModifiedAt = Now(),
            };
        if (design.Longitudinals.Any(l => l.Id.Value == guid))
            return design with
            {
                Longitudinals = design.Longitudinals.Select(l => l.Id.Value == guid ? l with { Geometry = clone } : l).ToList(),
                ModifiedAt = Now(),
            };
        if (design.Bulkheads.Any(b => b.Id.Value == guid))
            return design with
            {
                Bulkheads = design.Bulkheads.Select(b => b.Id.Value == guid ? b with { Geometry = clone } : b).ToList(),
                ModifiedAt = Now(),
            };
        if (design.Compartments.Any(c => c.Id.Value == guid))
            return design with
            {
                Compartments = design.Compartments.Select(c => c.Id.Value == guid ? c with { Geometry = clone } : c).ToList(),
                ModifiedAt = Now(),
            };
        if (design.Passages.Any(p => p.Id.Value == guid))
        {
            var next = design with
            {
                Passages = design.Passages.Select(p => p.Id.Value == guid ? p with { Geometry = clone } : p).ToList(),
                ModifiedAt = Now(),
            };
            return StructuralCutoutService.Regenerate(next);
        }

        if (design.Openings.Any(o => o.Id.Value == guid))
        {
            var next = design with
            {
                Openings = design.Openings.Select(o => o.Id.Value == guid ? o with { Geometry = clone } : o).ToList(),
                ModifiedAt = Now(),
            };
            return StructuralCutoutService.Regenerate(next);
        }

        if (design.Equipment.Any(e => e.Id.Value == guid))
            return design with
            {
                Equipment = design.Equipment.Select(e => e.Id.Value == guid ? e with { Geometry = clone } : e).ToList(),
                ModifiedAt = Now(),
            };

        return design;
    }

    private static string Now() => DateTimeOffset.UtcNow.ToString("O");

    private static CadDocument CloneDocument(CadDocument source)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(source);
        return System.Text.Json.JsonSerializer.Deserialize<CadDocument>(json)
               ?? new CadDocument { Name = source.Name };
    }

    public static ShipDesign SetDeckElevation(ShipDesign design, DeckId deckId, float elevationM)
    {
        ArgumentNullException.ThrowIfNull(design);
        var decks = design.Decks.Select(d =>
        {
            if (d.Id.Value != deckId.Value)
                return d;
            var L = design.Ship.LengthMeters;
            var B = design.Ship.BeamMeters;
            return d with
            {
                Elevation = ShipLengths.FromMeters(elevationM),
                Geometry = ShipGeometryBuilders.BuildDeckPlate(d.Name, L, B, elevationM),
            };
        }).ToList();
        return design with { Decks = decks, ModifiedAt = DateTimeOffset.UtcNow.ToString("O") };
    }

    public static ShipDesign SetFrameStation(ShipDesign design, FrameId frameId, float stationM)
    {
        ArgumentNullException.ThrowIfNull(design);
        var frames = design.Frames.Select(f =>
        {
            if (f.Id.Value != frameId.Value)
                return f;
            return f with
            {
                Station = ShipLengths.FromMeters(stationM),
                Geometry = ShipGeometryBuilders.BuildFrameAtStation(
                    f.Name,
                    stationM,
                    design.Ship.BeamMeters,
                    design.Ship.HeightMeters,
                    0.08f,
                    f.Material.Value),
            };
        }).ToList();
        var next = design with { Frames = frames, ModifiedAt = DateTimeOffset.UtcNow.ToString("O") };
        return StructuralCutoutService.Regenerate(next);
    }

    public static ShipDesign SetBulkheadThickness(ShipDesign design, BulkheadId bulkheadId, float thicknessM)
    {
        ArgumentNullException.ThrowIfNull(design);
        thicknessM = System.Math.Clamp(thicknessM, 0.02f, 1f);
        var bulkheads = design.Bulkheads.Select(b =>
        {
            if (b.Id.Value != bulkheadId.Value)
                return b;
            var path = ShipPlanPaths.ExtractPathXz(b.Geometry);
            var elev = b.DeckId is { } deckId
                ? ShipLengths.ToMeters(design.Decks.FirstOrDefault(d => d.Id.Value == deckId.Value)?.Elevation ?? ShipLengths.FromMeters(0f))
                : 0f;
            var deckIndex = b.DeckId is { } did
                ? design.Decks.FirstOrDefault(d => d.Id.Value == did.Value)?.Index ?? 0
                : 0;
            return b with
            {
                Thickness = ShipLengths.FromMeters(thicknessM),
                Geometry = path.Count >= 2
                    ? ShipGeometryBuilders.BuildBulkheadPath(
                        b.Name, path, thicknessM, ShipLengths.ToMeters(b.Height), elev, b.Material.Value, deckIndex)
                    : b.Geometry,
            };
        }).ToList();
        return design with { Bulkheads = bulkheads, ModifiedAt = DateTimeOffset.UtcNow.ToString("O") };
    }

    public static ShipDesign SetPassageWidth(ShipDesign design, PassageId passageId, float widthM)
    {
        ArgumentNullException.ThrowIfNull(design);
        widthM = System.Math.Max(0.6f, widthM);
        var passages = design.Passages.Select(p =>
        {
            if (p.Id.Value != passageId.Value)
                return p;
            var deck = design.Decks.FirstOrDefault(d => d.Id.Value == p.DeckId.Value);
            var elev = deck is null ? 0f : ShipLengths.ToMeters(deck.Elevation);
            var path = ShipPlanPaths.ExtractPathXz(p.Geometry);
            return p with
            {
                Width = ShipLengths.FromMeters(widthM),
                Geometry = path.Count >= 2 && deck is not null
                    ? ShipGeometryBuilders.BuildPassageVolume(
                        p.Name, path, widthM, ShipLengths.ToMeters(p.Height), elev, deck.Index)
                    : p.Geometry,
            };
        }).ToList();
        var next = design with { Passages = passages, ModifiedAt = DateTimeOffset.UtcNow.ToString("O") };
        return StructuralCutoutService.Regenerate(next);
    }

    public static ShipDesign UpdatePassagePath(ShipDesign design, PassageId passageId, IReadOnlyList<float[]> pathXz)
    {
        ArgumentNullException.ThrowIfNull(design);
        ArgumentNullException.ThrowIfNull(pathXz);
        if (pathXz.Count < 2)
            return design;
        var passages = design.Passages.Select(p =>
        {
            if (p.Id.Value != passageId.Value)
                return p;
            var deck = design.Decks.FirstOrDefault(d => d.Id.Value == p.DeckId.Value);
            var elev = deck is null ? 0f : ShipLengths.ToMeters(deck.Elevation);
            return p with
            {
                Geometry = deck is null
                    ? p.Geometry
                    : ShipGeometryBuilders.BuildPassageVolume(
                        p.Name, pathXz, ShipLengths.ToMeters(p.Width), ShipLengths.ToMeters(p.Height), elev, deck.Index),
            };
        }).ToList();
        var next = design with { Passages = passages, ModifiedAt = Now() };
        return StructuralCutoutService.Regenerate(next);
    }

    /// <summary>Slide an opening along its host bulkhead to normalized parameter t.</summary>
    public static ShipDesign MoveOpeningAlongHost(ShipDesign design, OpeningId openingId, float tAlong)
    {
        ArgumentNullException.ThrowIfNull(design);
        var opening = design.Openings.FirstOrDefault(o => o.Id.Value == openingId.Value);
        if (opening is null)
            return design;
        var host = design.Bulkheads.FirstOrDefault(b => b.Id.Value == opening.HostId.Value);
        if (host is null)
            return design;
        var path = ShipPlanPaths.ExtractPathXz(host.Geometry);
        var xz = ShipPlanPaths.PointAlong(path, tAlong);
        var deck = host.DeckId is { } did
            ? design.Decks.FirstOrDefault(d => d.Id.Value == did.Value)
            : null;
        var elev = deck is null ? 0f : ShipLengths.ToMeters(deck.Elevation);
        var ent = opening.Geometry.Entities.FirstOrDefault();
        var clearW = ent?.HalfExtents is { Length: >= 1 } he && he[0] > 0.05f ? he[0] * 2f : 0.9f;
        var clearH = ent?.HalfExtents is { Length: >= 2 } hy && hy[1] > 0.05f ? hy[1] * 2f : 2f;
        var deckIndex = deck?.Index ?? 0;
        var openings = design.Openings.Select(o =>
        {
            if (o.Id.Value != openingId.Value)
                return o;
            return o with
            {
                Geometry = ShipGeometryBuilders.BuildOpeningAperture(
                    o.Name, clearW, clearH, [xz[0], elev + clearH * 0.5f, xz[1]], o.Kind.ToString(), deckIndex),
            };
        }).ToList();
        var next = design with { Openings = openings, ModifiedAt = Now() };
        return StructuralCutoutService.Regenerate(next);
    }
}
