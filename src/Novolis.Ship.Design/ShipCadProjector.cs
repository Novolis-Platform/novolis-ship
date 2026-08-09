using System.Text.Json;
using Novolis.Cad.Primitives;
using Novolis.Ship.Primitives;

namespace Novolis.Ship.Design;

/// <summary>Project between object-first <see cref="ShipDesign"/> and flat legacy <see cref="CadDocument"/>.</summary>
public static class ShipCadProjector
{
    public static CadDocument ToCadDocument(ShipDesign design)
    {
        ArgumentNullException.ThrowIfNull(design);
        var now = DateTimeOffset.UtcNow.ToString("O");
        var doc = new CadDocument
        {
            Name = design.Ship.Name,
            CreatedAt = design.CreatedAt ?? now,
            ModifiedAt = now,
            Generator = new CadGenerator { Name = "Novolis.Ship.Design", Version = "2026.1.0" },
            Layers = [new CadLayer { Name = "0" }],
        };
        ShipDocumentMetrics.SetShipEnvelope(
            doc,
            design.Ship.LengthMeters,
            design.Ship.BeamMeters,
            design.Ship.HeightMeters,
            design.Decks.Count > 1
                ? ShipLengths.ToMeters(design.Decks[^1].Elevation) / System.Math.Max(1, design.Decks.Count - 1)
                : design.Ship.HeightMeters / System.Math.Max(1, design.Ship.DeckCount));

        foreach (var (_, geom, _) in design.GeometricObjects())
        {
            foreach (var e in geom.Entities)
                doc.Entities.Add(CloneEntity(e));
        }

        return doc;
    }

    public static ShipDesign FromCadDocument(CadDocument document, string? name = null)
    {
        ArgumentNullException.ThrowIfNull(document);
        var loa = ShipDocumentMetrics.GetLoaMeters(document);
        var beam = ShipDocumentMetrics.GetBeamMeters(document);
        var height = ShipDocumentMetrics.GetHeightMeters(document);
        var deckSpacing = ShipDocumentMetrics.GetDeckSpacingMeters(document);
        var deckCount = System.Math.Max(1, document.Entities
            .Where(e => e.Kind is "wall" or "space")
            .Select(e => e.Deck)
            .DefaultIfEmpty(0)
            .Distinct()
            .Count());

        var definition = new ShipDefinition
        {
            Name = name ?? document.Name,
            Length = ShipLengths.FromMeters(loa),
            Beam = ShipLengths.FromMeters(beam),
            Height = ShipLengths.FromMeters(height),
            DeckCount = deckCount,
            DeckSpacing = ShipLengths.FromMeters(deckSpacing),
            HullMaterial = MaterialId.Steel,
            HullThickness = ShipLengths.FromMeters(0.02f),
            FrameSpacing = ShipLengths.FromMeters(System.Math.Max(1f, loa / 12f)),
            PrimaryStructuralMaterial = MaterialId.Steel,
            HullGenerator = HullGeneratorKind.Box,
            GravitySystem = GravitySystemKind.Plating,
            NominalGravityG = 1f,
            NominalInternalPressureAtm = 1f,
            ExternalEnvironment = ExternalEnvironmentKind.Vacuum,
        };

        // Best-effort: wrap exterior solids as hull geometry; remaining entities as a single compartment bag per deck.
        var hullDoc = new CadDocument
        {
            Name = "Hull",
            Layers = [new CadLayer { Name = "0" }],
            Generator = document.Generator,
        };
        var interiorDoc = new CadDocument
        {
            Name = "ImportedInterior",
            Layers = [new CadLayer { Name = "0" }],
            Generator = document.Generator,
        };

        foreach (var e in document.Entities)
        {
            var clone = CloneEntity(e);
            if (IsExterior(e))
                hullDoc.Entities.Add(clone);
            else
                interiorDoc.Entities.Add(clone);
        }

        if (hullDoc.Entities.Count == 0)
        {
            hullDoc = Novolis.Ship.Structure.ShipGeometryBuilders.BuildBoxHull(loa, beam, height, "steel");
        }

        var decks = new List<DeckDesign>();
        for (var i = 0; i < deckCount; i++)
        {
            decks.Add(new DeckDesign
            {
                Id = DeckId.New(),
                Name = $"Deck {i}",
                Index = i,
                Elevation = ShipLengths.FromMeters(i * deckSpacing),
                Geometry = Novolis.Ship.Structure.ShipGeometryBuilders.BuildDeckPlate($"Deck {i}", loa, beam, i * deckSpacing),
            });
        }

        var compartments = new List<CompartmentDesign>();
        if (interiorDoc.Entities.Count > 0 && decks.Count > 0)
        {
            compartments.Add(new CompartmentDesign
            {
                Id = CompartmentId.New(),
                Name = "Imported",
                DeckId = decks[0].Id,
                Kind = CompartmentKind.General,
                Geometry = interiorDoc,
            });
        }

        var environment = ShipEnvironment.FromDefinition(definition);
        return new ShipDesign
        {
            SchemaVersion = ShipDesign.CurrentSchemaVersion,
            CreatedAt = document.CreatedAt ?? DateTimeOffset.UtcNow.ToString("O"),
            ModifiedAt = DateTimeOffset.UtcNow.ToString("O"),
            Ship = definition,
            Hull = new HullDesign
            {
                Id = HullId.New(),
                Geometry = hullDoc,
                Material = MaterialId.Steel,
                Thickness = definition.HullThickness,
                Generator = HullGeneratorKind.Box,
            },
            Decks = decks,
            Frames = [],
            Longitudinals = [],
            Bulkheads = [],
            Compartments = compartments,
            Passages = [],
            Openings = [],
            Equipment = [],
            Environment = environment,
            LoadCases = ShipLoadCase.CreateBaseline(environment),
            Cutouts = [],
        };
    }

    private static bool IsExterior(CadEntity e)
    {
        if (e.Name is not null
            && (e.Name.StartsWith("ext-", StringComparison.OrdinalIgnoreCase)
                || e.Name.StartsWith("nacelle-", StringComparison.OrdinalIgnoreCase)))
            return true;
        if (e.Properties is null || !e.Properties.TryGetValue(ShipPropertyKeys.Exterior, out var el))
            return false;
        if (el.ValueKind == JsonValueKind.True)
            return true;
        return el.ValueKind == JsonValueKind.String
            && string.Equals(el.GetString(), "true", StringComparison.OrdinalIgnoreCase);
    }

    private static CadEntity CloneEntity(CadEntity e)
    {
        var json = JsonSerializer.Serialize(e);
        return JsonSerializer.Deserialize<CadEntity>(json) ?? new CadEntity { Kind = e.Kind, Id = e.Id };
    }
}
