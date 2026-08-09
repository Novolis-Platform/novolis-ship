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
        var hullMat = definition.HullMaterial.Value;
        var structMat = (definition.PrimaryStructuralMaterial.Value is { Length: > 0 } m
            ? m
            : hullMat);
        var now = DateTimeOffset.UtcNow.ToString("O");
        var environment = ShipEnvironment.FromDefinition(definition);

        var hullGeom = definition.HullGenerator switch
        {
            HullGeneratorKind.Box => ShipGeometryBuilders.BuildBoxHull(L, B, H, hullMat),
            HullGeneratorKind.TaperedBox => ShipGeometryBuilders.BuildTaperedBoxHull(L, B, H, hullMat),
            HullGeneratorKind.Faceted => ShipGeometryBuilders.BuildFacetedHull(L, B, H, hullMat),
            HullGeneratorKind.Cylinder => ShipGeometryBuilders.BuildCylinderHull(L, B, H, hullMat),
            HullGeneratorKind.Capsule => ShipGeometryBuilders.BuildCapsuleHull(L, B, H, hullMat),
            HullGeneratorKind.LoftedSections => ShipGeometryBuilders.BuildLoftedSectionsHull(L, B, H, hullMat),
            _ => ShipGeometryBuilders.BuildTaperedBoxHull(L, B, H, hullMat),
        };

        var hull = new HullDesign
        {
            Id = HullId.New(),
            Geometry = hullGeom,
            Material = definition.HullMaterial,
            Thickness = definition.HullThickness,
            Generator = definition.HullGenerator,
        };

        var deckSpacing = definition.DeckSpacingMeters;
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
                Material = new MaterialId(structMat),
                Geometry = ShipGeometryBuilders.BuildFrameAtStation(name, z, B, H, 0.08f, structMat),
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
                Material = new MaterialId(structMat),
                Geometry = ShipGeometryBuilders.BuildFrameAtStation("F000", 0f, B, H, 0.08f, structMat),
            });
        }

        var longitudinals = new List<LongitudinalDesign>
        {
            new()
            {
                Id = LongitudinalId.New(),
                Name = "Keel",
                Kind = LongitudinalKind.Keel,
                Material = new MaterialId(structMat),
                Geometry = ShipGeometryBuilders.BuildLongitudinal("Keel", L, 0.15f, 0f, 0.3f, 0.12f, structMat),
            },
            new()
            {
                Id = LongitudinalId.New(),
                Name = "Port Stringer",
                Kind = LongitudinalKind.SideLongitudinal,
                Material = new MaterialId(structMat),
                Geometry = ShipGeometryBuilders.BuildLongitudinal("Port Stringer", L, H * 0.5f, -B * 0.4f, 0.2f, 0.08f, structMat),
            },
            new()
            {
                Id = LongitudinalId.New(),
                Name = "Starboard Stringer",
                Kind = LongitudinalKind.SideLongitudinal,
                Material = new MaterialId(structMat),
                Geometry = ShipGeometryBuilders.BuildLongitudinal("Starboard Stringer", L, H * 0.5f, B * 0.4f, 0.2f, 0.08f, structMat),
            },
        };

        var bulkheads = new List<BulkheadDesign>();
        var midDeck = decks[System.Math.Min(1, decks.Count - 1)];
        var deckElev = ShipLengths.ToMeters(midDeck.Elevation);
        var deckH = System.Math.Max(2.2f, deckSpacing * 0.9f);
        var bhThickness = System.Math.Max(0.05f, definition.HullThicknessMeters);
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
                Material = new MaterialId(structMat),
                Thickness = ShipLengths.FromMeters(bhThickness),
                Height = ShipLengths.FromMeters(deckH),
                DeckId = midDeck.Id,
                IsPrimary = true,
                Geometry = ShipGeometryBuilders.BuildBulkheadPath(
                    name, path, bhThickness, deckH, deckElev, structMat, midDeck.Index),
            });
        }

        return new ShipDesign
        {
            SchemaVersion = ShipDesign.CurrentSchemaVersion,
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
            Environment = environment,
            LoadCases = ShipLoadCase.CreateBaseline(environment),
            Cutouts = [],
        };
    }
}
