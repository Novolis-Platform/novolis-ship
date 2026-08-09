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
}
