using Novolis.Ship.Structure;

namespace Novolis.Ship.Design;

/// <summary>Creates an immediately valid <see cref="ShipDesign"/> from a definition (structure-first).</summary>
public static class ShipFactory
{
    public static ShipDesign Create(ShipDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        if (definition.DeckCount < 1)
            throw new ArgumentOutOfRangeException(nameof(definition), "DeckCount must be >= 1.");
        if (definition.FrameSpacingMeters <= 0f)
            throw new ArgumentOutOfRangeException(nameof(definition), "FrameSpacing must be > 0.");

        var L = definition.LengthMeters;
        var B = definition.BeamMeters;
        var H = definition.HeightMeters;
        var mat = definition.HullMaterial.Value;
        var now = DateTimeOffset.UtcNow.ToString("O");

        var hullGeom = definition.HullGenerator switch
        {
            HullGeneratorKind.Box => ShipGeometryBuilders.BuildBoxHull(L, B, H, mat),
            HullGeneratorKind.TaperedBox => ShipGeometryBuilders.BuildTaperedBoxHull(L, B, H, mat),
            HullGeneratorKind.Faceted => ShipGeometryBuilders.BuildFacetedHull(L, B, H, mat),
            HullGeneratorKind.Cylinder => ShipGeometryBuilders.BuildCylinderHull(L, B, H, mat),
            HullGeneratorKind.Capsule => ShipGeometryBuilders.BuildCapsuleHull(L, B, H, mat),
            HullGeneratorKind.LoftedSections => ShipGeometryBuilders.BuildLoftedSectionsHull(L, B, H, mat),
            _ => ShipGeometryBuilders.BuildTaperedBoxHull(L, B, H, mat),
        };

        var hull = new HullDesign
        {
            Id = HullId.New(),
            Geometry = hullGeom,
            Material = definition.HullMaterial,
            Thickness = definition.HullThickness,
            Generator = definition.HullGenerator,
        };

        var deckSpacing = H / System.Math.Max(1, definition.DeckCount);
        var decks = new List<DeckDesign>(definition.DeckCount);
        for (var i = 0; i < definition.DeckCount; i++)
        {
            var elev = i * deckSpacing;
            var name = i == 0 ? "Tank Top" : i == definition.DeckCount - 1 ? "Weather Deck" : $"Deck {i}";
            decks.Add(new DeckDesign
            {
                Id = DeckId.New(),
                Name = name,
                Index = i,
                Elevation = ShipLengths.FromMeters(elev),
                Geometry = ShipGeometryBuilders.BuildDeckPlate(name, L, B, elev),
            });
        }

        var frames = new List<FrameDesign>();
        var spacing = definition.FrameSpacingMeters;
        var half = L * 0.5f;
        var frameIndex = 0;
        for (var z = -half + spacing; z <= half - spacing * 0.5f + 1e-4f; z += spacing)
        {
            var name = $"F{frameIndex:000}";
            frames.Add(new FrameDesign
            {
                Id = FrameId.New(),
                Name = name,
                Station = ShipLengths.FromMeters(z),
                Material = definition.HullMaterial,
                Geometry = ShipGeometryBuilders.BuildFrameAtStation(name, z, B, H, 0.08f, mat),
            });
            frameIndex++;
        }

        if (frames.Count == 0)
        {
            frames.Add(new FrameDesign
            {
                Id = FrameId.New(),
                Name = "F000",
                Station = ShipLengths.FromMeters(0f),
                Material = definition.HullMaterial,
                Geometry = ShipGeometryBuilders.BuildFrameAtStation("F000", 0f, B, H, 0.08f, mat),
            });
        }

        var longitudinals = new List<LongitudinalDesign>
        {
            new()
            {
                Id = LongitudinalId.New(),
                Name = "Keel",
                Kind = LongitudinalKind.Keel,
                Material = definition.HullMaterial,
                Geometry = ShipGeometryBuilders.BuildLongitudinal("Keel", L, 0.15f, 0f, 0.3f, 0.12f, mat),
            },
            new()
            {
                Id = LongitudinalId.New(),
                Name = "Port Stringer",
                Kind = LongitudinalKind.SideLongitudinal,
                Material = definition.HullMaterial,
                Geometry = ShipGeometryBuilders.BuildLongitudinal("Port Stringer", L, H * 0.5f, -B * 0.4f, 0.2f, 0.08f, mat),
            },
            new()
            {
                Id = LongitudinalId.New(),
                Name = "Starboard Stringer",
                Kind = LongitudinalKind.SideLongitudinal,
                Material = definition.HullMaterial,
                Geometry = ShipGeometryBuilders.BuildLongitudinal("Starboard Stringer", L, H * 0.5f, B * 0.4f, 0.2f, 0.08f, mat),
            },
        };

        var bulkheads = new List<BulkheadDesign>();
        var midDeck = decks[System.Math.Min(1, decks.Count - 1)];
        var deckElev = ShipLengths.ToMeters(midDeck.Elevation);
        var deckH = System.Math.Max(2.2f, deckSpacing * 0.9f);
        var bhThickness = System.Math.Max(0.05f, definition.HullThicknessMeters);
        // Primary watertight planes near third-points.
        foreach (var (name, z) in new[] { ("BH-Fwd", L * 0.2f), ("BH-Mid", 0f), ("BH-Aft", -L * 0.2f) })
        {
            var path = new float[][]
            {
                [-B * 0.45f, z],
                [B * 0.45f, z],
            };
            bulkheads.Add(new BulkheadDesign
            {
                Id = BulkheadId.New(),
                Name = name,
                Material = definition.HullMaterial,
                Thickness = ShipLengths.FromMeters(bhThickness),
                Height = ShipLengths.FromMeters(deckH),
                DeckId = midDeck.Id,
                IsPrimary = true,
                Geometry = ShipGeometryBuilders.BuildBulkheadPath(
                    name, path, bhThickness, deckH, deckElev, mat, midDeck.Index),
            });
        }

        return new ShipDesign
        {
            CreatedAt = now,
            ModifiedAt = now,
            Ship = definition,
            Hull = hull,
            Decks = decks,
            Frames = frames,
            Longitudinals = longitudinals,
            Bulkheads = bulkheads,
            Compartments = [],
            Passages = [],
            Openings = [],
            Equipment = [],
            Cutouts = [],
        };
    }
}
