using Novolis.Ship.Design;

namespace Novolis.Ship.Unit;

public sealed class ShipDesignTests
{
    private static ShipDefinition SampleDefinition() => new()
    {
        Name = "Smoke Freighter",
        Length = ShipLengths.FromMeters(60f),
        Beam = ShipLengths.FromMeters(16f),
        Height = ShipLengths.FromMeters(12f),
        DeckCount = 3,
        HullMaterial = MaterialId.Steel,
        HullThickness = ShipLengths.FromMeters(0.02f),
        FrameSpacing = ShipLengths.FromMeters(4f),
        HullGenerator = HullGeneratorKind.TaperedBox,
    };

    [Test]
    public async Task Create_produces_hull_decks_frames_and_primary_bulkheads()
    {
        var design = ShipFactory.Create(SampleDefinition());
        await Assert.That(design.Hull.Geometry.Entities.Count).IsGreaterThan(0);
        await Assert.That(design.Decks.Count).IsEqualTo(3);
        await Assert.That(design.Frames.Count).IsGreaterThan(0);
        await Assert.That(design.Longitudinals.Count).IsGreaterThanOrEqualTo(3);
        await Assert.That(design.Bulkheads.Count).IsEqualTo(3);
        await Assert.That(design.Frames[0].Name).IsEqualTo("F000");
    }

    [Test]
    public async Task Shipjson_round_trip()
    {
        var design = ShipFactory.Create(SampleDefinition());
        var path = Path.Combine(Path.GetTempPath(), "ship-design-" + Guid.NewGuid().ToString("N") + ".shipjson");
        try
        {
            ShipDesignStore.Save(design, path);
            var loaded = ShipDesignStore.Load(path);
            await Assert.That(loaded.Ship.Name).IsEqualTo(design.Ship.Name);
            await Assert.That(loaded.Frames.Count).IsEqualTo(design.Frames.Count);
            await Assert.That(loaded.Hull.Geometry.Entities.Count).IsEqualTo(design.Hull.Geometry.Entities.Count);
            await Assert.That(ShipLengths.ToMeters(loaded.Ship.Length)).IsEqualTo(60f);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Test]
    public async Task Passage_regenerates_structural_cutouts()
    {
        var design = ShipFactory.Create(SampleDefinition());
        var deck = design.Decks[1];
        var withPassage = ShipDesignMutations.AddPassage(
            design,
            deck.Id,
            "Main Corridor",
            [[-4f, -10f], [-4f, 10f]],
            widthM: 1.2f,
            heightM: 2.2f);
        await Assert.That(withPassage.Passages.Count).IsEqualTo(1);
        await Assert.That(withPassage.Cutouts.Count).IsGreaterThan(0);
        await Assert.That(withPassage.Cutouts.Any(c => c.Purpose == CutoutPurpose.Passage)).IsTrue();
    }

    [Test]
    public async Task Projector_to_cad_and_back_keeps_envelope()
    {
        var design = ShipFactory.Create(SampleDefinition());
        var cad = ShipCadProjector.ToCadDocument(design);
        await Assert.That(cad.Entities.Count).IsGreaterThan(0);
        var back = ShipCadProjector.FromCadDocument(cad);
        await Assert.That(back.Ship.LengthMeters).IsEqualTo(60f);
        await Assert.That(back.Hull.Geometry.Entities.Count).IsGreaterThan(0);
    }

    [Test]
    public async Task Validate_new_ship_is_ok()
    {
        var design = ShipFactory.Create(SampleDefinition());
        var result = ShipDesignValidator.Validate(design);
        await Assert.That(result.Issues.Any(i => i.Code == "SHIP_HULL_EMPTY")).IsFalse();
    }

    [Test]
    public async Task Hull_generators_produce_geometry()
    {
        foreach (var kind in new[]
                 {
                     HullGeneratorKind.Box,
                     HullGeneratorKind.TaperedBox,
                     HullGeneratorKind.Faceted,
                     HullGeneratorKind.Cylinder,
                     HullGeneratorKind.Capsule,
                     HullGeneratorKind.LoftedSections,
                 })
        {
            var def = SampleDefinition() with { HullGenerator = kind, Name = kind.ToString() };
            var design = ShipFactory.Create(def);
            await Assert.That(design.Hull.Geometry.Entities.Count).IsGreaterThan(0);
            await Assert.That(design.Hull.Generator).IsEqualTo(kind);
        }
    }

    [Test]
    public async Task Shared_compartment_edges_are_detected()
    {
        var design = ShipFactory.Create(SampleDefinition());
        var deck = design.Decks[1];
        design = ShipDesignMutations.AddCompartment(
            design, deck.Id, "A", [[-5f, -5f], [0f, -5f], [0f, 5f], [-5f, 5f]]);
        design = ShipDesignMutations.AddCompartment(
            design, deck.Id, "B", [[0f, -5f], [5f, -5f], [5f, 5f], [0f, 5f]]);
        var shared = CompartmentBoundaryResolver.FindSharedEdges(design);
        await Assert.That(shared.Count).IsGreaterThan(0);
    }
}
