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
        DeckSpacing = ShipLengths.FromMeters(4f),
        HullMaterial = MaterialId.Steel,
        HullThickness = ShipLengths.FromMeters(0.024f),
        FrameSpacing = ShipLengths.FromMeters(4f),
        PrimaryStructuralMaterial = MaterialId.Steel,
        HullGenerator = HullGeneratorKind.TaperedBox,
        GravitySystem = GravitySystemKind.Plating,
        NominalGravityG = 1f,
        NominalInternalPressureAtm = 1f,
        ExternalEnvironment = ExternalEnvironmentKind.Vacuum,
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
            await Assert.That(loaded.SchemaVersion).IsEqualTo(ShipDesign.CurrentSchemaVersion);
            await Assert.That(loaded.Environment.External).IsEqualTo(ExternalEnvironmentKind.Vacuum);
            await Assert.That(loaded.LoadCases.Count).IsGreaterThanOrEqualTo(4);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Test]
    public async Task Create_seeds_environment_and_load_cases()
    {
        var design = ShipFactory.Create(SampleDefinition());
        await Assert.That(design.Environment.GravitySystem).IsEqualTo(GravitySystemKind.Plating);
        await Assert.That(design.LoadCases.Count).IsEqualTo(4);
        await Assert.That(design.Frames[0].Material.Value).IsEqualTo("steel");
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
    public async Task Blank_has_no_structure_until_create()
    {
        var blank = ShipFactory.CreateBlank();
        await Assert.That(blank.Hull.Geometry.Entities.Count).IsEqualTo(0);
        await Assert.That(blank.Decks.Count).IsEqualTo(0);
        await Assert.That(blank.Frames.Count).IsEqualTo(0);
        await Assert.That(string.IsNullOrEmpty(blank.Ship.Name)).IsTrue();
    }

    [Test]
    public async Task Architect_strokes_bulkhead_room_opening()
    {
        var design = ShipFactory.Create(SampleDefinition());
        var deck = design.Decks[1];
        design = ShipDesignMutations.AddBulkheadPath(
            design, deck.Id, "Wall-A", [[-6f, -4f], [6f, -4f]], 0.08f, 3.2f);
        await Assert.That(design.Bulkheads.Count).IsEqualTo(4);
        design = ShipDesignMutations.AddCompartmentPolygon(
            design, deck.Id, "Room-A", [[-5f, -8f], [5f, -8f], [5f, -1f], [-5f, -1f]]);
        design = ShipDesignMutations.AddCompartmentPolygon(
            design, deck.Id, "Room-B", [[-5f, -1f], [5f, -1f], [5f, 6f], [-5f, 6f]]);
        await Assert.That(design.Compartments.Count).IsEqualTo(2);
        var shared = CompartmentBoundaryResolver.FindSharedEdges(design);
        await Assert.That(shared.Count).IsGreaterThan(0);
        await Assert.That(design.Bulkheads.Count(b => !b.IsPrimary)).IsGreaterThan(0);
        var host = design.Bulkheads.First(b => b.Name == "Wall-A");
        design = ShipDesignMutations.AddOpeningOnHost(
            design, host.Id, "Door-A", OpeningKind.Door, tAlong: 0.5f, clearWidthM: 0.9f, clearHeightM: 2f);
        await Assert.That(design.Openings.Count).IsEqualTo(1);
        design = ShipDesignMutations.AppendBulkheadVertex(design, host.Id, 6f, 0f);
        var path = ShipPlanPaths.ExtractPathXz(design.Bulkheads.First(b => b.Id.Value == host.Id.Value).Geometry);
        await Assert.That(path.Count).IsGreaterThanOrEqualTo(3);
    }

    [Test]
    public async Task Mutations_add_bulkhead_opening_and_equipment()
    {
        var design = ShipFactory.Create(SampleDefinition());
        var deck = design.Decks[1];
        design = ShipDesignMutations.AddBulkhead(
            design, deck.Id, "BH-Extra", [[-6f, 2f], [6f, 2f]], 0.08f, 3.5f);
        await Assert.That(design.Bulkheads.Count).IsEqualTo(4);
        var host = design.Bulkheads[^1].Id.AsObject();
        design = ShipDesignMutations.AddOpening(
            design, host, "Door-1", OpeningKind.Door, 0.9f, 2f, [0f, 4f, 2f]);
        await Assert.That(design.Openings.Count).IsEqualTo(1);
        design = ShipDesignMutations.AddEquipment(
            design, "Pump", [1f, 4f, 0f], [0.5f, 0.5f, 0.5f], 120f);
        await Assert.That(design.Equipment.Count).IsEqualTo(1);
        await Assert.That(design.Equipment[0].MassKg).IsEqualTo(120f);
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

    [Test]
    public async Task Plan_snap_ortho_angle_and_candidates()
    {
        var ortho = ShipPlanPaths.ApplyOrtho(0f, 0f, 4.2f, 0.3f);
        await Assert.That(ortho[0]).IsEqualTo(4.2f);
        await Assert.That(ortho[1]).IsEqualTo(0f);
        var ang = ShipPlanPaths.ApplyAngle15(0f, 0f, 10f, 0.1f);
        await Assert.That(MathF.Abs(ang[1])).IsLessThan(0.05f);

        var design = ShipFactory.Create(SampleDefinition());
        var deck = design.Decks[1];
        design = ShipDesignMutations.AddBulkheadPath(
            design, deck.Id, "SnapWall", [[-4f, 0f], [4f, 0f]], 0.08f, 3f);
        var candidates = ShipPlanPaths.CollectSnapCandidates(design, deck.Id);
        await Assert.That(candidates.Any(c => c.Kind == ShipPlanPaths.PlanSnapKind.Vertex)).IsTrue();
        await Assert.That(candidates.Any(c => c.Kind == ShipPlanPaths.PlanSnapKind.Midpoint)).IsTrue();
        await Assert.That(ShipPlanPaths.TryNearestVertexOrMid(candidates, 4.05f, 0.1f, 0.5f, out var hit)).IsTrue();
        await Assert.That(hit.Kind).IsEqualTo(ShipPlanPaths.PlanSnapKind.Vertex);
        await Assert.That(ShipPlanPaths.TryNearestEdge(design, deck.Id, 0f, 0.2f, 0.5f, out var ex, out var ez, out _))
            .IsTrue();
        await Assert.That(MathF.Abs(ez)).IsLessThan(0.05f);
        await Assert.That(MathF.Abs(ex)).IsLessThan(0.05f);
        var guides = ShipPlanPaths.CollectAlignmentGuides(candidates, 4f, 1f, 0.1f, 50f);
        await Assert.That(guides.Count).IsGreaterThan(0);
    }
}
