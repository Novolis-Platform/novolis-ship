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
                name, clearWidthM, clearHeightM, center, kind.ToString()),
        };
        var next = design with
        {
            Openings = design.Openings.Append(opening).ToList(),
            ModifiedAt = DateTimeOffset.UtcNow.ToString("O"),
        };
        return StructuralCutoutService.Regenerate(next);
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
            var path = ExtractPathXz(b.Geometry);
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
            var path = ExtractPathXz(p.Geometry);
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

    private static List<float[]> ExtractPathXz(Novolis.Cad.Primitives.CadDocument geometry)
    {
        var path = new List<float[]>();
        foreach (var e in geometry.Entities)
        {
            if (e.A is { Length: >= 3 })
                path.Add([e.A[0], e.A[2]]);
            if (e.B is { Length: >= 3 })
                path.Add([e.B[0], e.B[2]]);
        }

        return path;
    }
}
