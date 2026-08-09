using Novolis.Cad.Primitives;
using Novolis.Ship.Primitives;
using Novolis.Ship.Topology;
using Novolis.Ship.Validation;

namespace Novolis.Ship.Unit;

public sealed class ShipTopologyAndValidationTests
{
    [Test]
    public async Task Sealed_pair_of_rooms_with_closed_airtight_door()
    {
        var doc = BuildTwoRoomFixture(doorClearWidth: 1.1f, leafOpen: false);
        var topo = ShipTopology.Analyze(doc);
        await Assert.That(topo.VentingToExterior.Count).IsEqualTo(0);
        await Assert.That(topo.SealedComponents.Count).IsEqualTo(1);
        await Assert.That(topo.SealedComponents[0].Count).IsEqualTo(2);

        ShipTopology.ApplySpaceFlags(doc, topo);
        foreach (var space in ShipCad.Spaces(doc))
            await Assert.That(space.Flags?.Enclosed).IsTrue();

        var result = ShipValidator.Validate(doc, topo);
        await Assert.That(result.Ok).IsTrue();
        await Assert.That(result.Issues.Any(i => i.Code == "SHIP_CLEAR_WIDTH")).IsFalse();
    }

    [Test]
    public async Task Open_door_vents_both_rooms()
    {
        var doc = BuildTwoRoomFixture(doorClearWidth: 1.1f, leafOpen: true);
        var topo = ShipTopology.Analyze(doc);
        await Assert.That(topo.VentingToExterior.Count).IsEqualTo(2);
        await Assert.That(topo.SealedComponents.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Narrow_door_fails_validation()
    {
        var doc = BuildTwoRoomFixture(doorClearWidth: 0.8f, leafOpen: false);
        var result = ShipValidator.Validate(doc);
        await Assert.That(result.Ok).IsFalse();
        await Assert.That(result.Issues.Any(i => i.Code == "SHIP_CLEAR_WIDTH")).IsTrue();
    }

    [Test]
    public async Task Airlock_requires_distinct_hatches()
    {
        var doc = BuildTwoRoomFixture(doorClearWidth: 1.0f, leafOpen: false);
        var spaces = ShipCad.Spaces(doc).ToList();
        var opening = ShipCad.Openings(doc).First();
        doc.Entities.Add(ShipCad.CreateAirlock("AL", spaces[0].Id, opening.Id, opening.Id));
        var result = ShipValidator.Validate(doc);
        await Assert.That(result.Issues.Any(i => i.Code == "SHIP_AIRLOCK_SAME_HATCH")).IsTrue();
    }

    [Test]
    public async Task Closed_vacuum_assisted_exterior_hatch_keeps_space_sealed()
    {
        var doc = BuildExteriorHatchFixture(leafOpen: false, vacuumAssist: true);
        var topo = ShipTopology.Analyze(doc);
        await Assert.That(topo.VentingToExterior.Count).IsEqualTo(0);
        await Assert.That(topo.IsSpaceSealed(ShipCad.Spaces(doc).First().Id)).IsTrue();

        var hatch = ShipCad.Openings(doc).First();
        await Assert.That(ShipCad.IsVacuumAssisted(hatch)).IsTrue();
        await Assert.That(ShipCad.SealContactPressureKPa(101.3f, 0f)).IsGreaterThan(100f);

        var result = ShipValidator.Validate(doc, topo);
        await Assert.That(result.Issues.Any(i => i.Code == "SHIP_VACUUM_SEAL")).IsFalse();
    }

    [Test]
    public async Task Open_exterior_hatch_vents_space()
    {
        var doc = BuildExteriorHatchFixture(leafOpen: true, vacuumAssist: true);
        var topo = ShipTopology.Analyze(doc);
        await Assert.That(topo.VentingToExterior.Count).IsEqualTo(1);
    }

    [Test]
    public async Task L_airlock_open_D3_with_D2_closed_vents_only_outer_chamber()
    {
        var doc = BuildLAirlockFixture(d3Open: true, d2Open: false, d1Open: false);
        var topo = ShipTopology.Analyze(doc);
        var byName = ShipCad.Spaces(doc).ToDictionary(s => s.Name!);

        await Assert.That(topo.IsSpaceSealed(byName["Crossing"].Id)).IsTrue();
        await Assert.That(topo.IsSpaceSealed(byName["ChamberA"].Id)).IsTrue();
        await Assert.That(topo.IsSpaceSealed(byName["ChamberB"].Id)).IsFalse();
        await Assert.That(topo.VentingToExterior.Contains(byName["ChamberB"].Id)).IsTrue();
    }

    [Test]
    public async Task Vacuum_class_without_assist_warns()
    {
        var doc = BuildExteriorHatchFixture(leafOpen: false, vacuumAssist: false);
        var hatch = ShipCad.Openings(doc).First();
        ShipCad.TagOpeningPressure(hatch, ShipPressureClass.Vacuum, 1.0f, 2.1f);
        var result = ShipValidator.Validate(doc);
        await Assert.That(result.Issues.Any(i => i.Code == "SHIP_VACUUM_SEAL")).IsTrue();
    }

    /// <summary>
    /// Two rooms share a wall on X=0. Left room [-4,-1]×[0,4], right [1,4]×[0,4], wall along X=0.
    /// </summary>
    private static CadDocument BuildTwoRoomFixture(float doorClearWidth, bool leafOpen)
    {
        var wall = new CadEntity
        {
            Kind = "wall",
            Name = "Bulkhead",
            Deck = 0,
            A = [0f, 0f, 0f],
            B = [0f, 0f, 4f],
            Thickness = 0.15f,
            Height = 2.4f,
        };

        var left = new CadEntity
        {
            Kind = "space",
            Name = "Port",
            Deck = 0,
            Height = 2.4f,
            Points =
            [
                [-4f, 0f, 0f],
                [0f, 0f, 0f],
                [0f, 0f, 4f],
                [-4f, 0f, 4f],
            ],
        };

        var right = new CadEntity
        {
            Kind = "space",
            Name = "Starboard",
            Deck = 0,
            Height = 2.4f,
            Points =
            [
                [0f, 0f, 0f],
                [4f, 0f, 0f],
                [4f, 0f, 4f],
                [0f, 0f, 4f],
            ],
        };

        var door = new CadEntity
        {
            Kind = "opening",
            Name = "Door",
            OpeningType = "door",
            Deck = 0,
            HostWallId = wall.Id,
            Height = 2.2f,
            Footprint =
            [
                [-0.5f, 0f, 1.5f],
                [0.5f, 0f, 1.5f],
                [0.5f, 0f, 2.5f],
                [-0.5f, 0f, 2.5f],
            ],
        };
        ShipCad.TagOpeningPressure(
            door,
            ShipPressureClass.Habitable,
            clearWidth: doorClearWidth,
            clearHeight: 2.2f,
            leafState: leafOpen ? ShipLeafState.Open : ShipLeafState.Closed);

        var doc = new CadDocument
        {
            Name = "Fixture two-room",
            Entities = [wall, left, right, door],
        };
        ShipDocumentMetrics.SetShipEnvelope(doc, loa: 20f, beam: 8f, height: 4f, deckSpacing: 4f);
        return doc;
    }

    /// <summary>Single cabin with an exterior shell hatch (wall touched by one space only).</summary>
    private static CadDocument BuildExteriorHatchFixture(bool leafOpen, bool vacuumAssist)
    {
        var shell = new CadEntity
        {
            Kind = "wall",
            Name = "Shell",
            Deck = 0,
            A = [0f, 0f, 0f],
            B = [0f, 0f, 4f],
            Thickness = 0.15f,
            Height = 2.4f,
        };

        var cabin = new CadEntity
        {
            Kind = "space",
            Name = "Cabin",
            Deck = 0,
            Height = 2.4f,
            Points =
            [
                [-4f, 0f, 0f],
                [0f, 0f, 0f],
                [0f, 0f, 4f],
                [-4f, 0f, 4f],
            ],
        };

        var hatch = new CadEntity
        {
            Kind = "opening",
            Name = "D3",
            OpeningType = "hatch",
            Deck = 0,
            HostWallId = shell.Id,
            Height = 2.1f,
            Footprint =
            [
                [-0.5f, 0f, 1.5f],
                [0.5f, 0f, 1.5f],
                [0.5f, 0f, 2.5f],
                [-0.5f, 0f, 2.5f],
            ],
        };

        var leaf = leafOpen ? ShipLeafState.Open : ShipLeafState.Closed;
        if (vacuumAssist)
            ShipCad.TagVacuumAssistedHatch(hatch, clearWidth: 1.0f, clearHeight: 2.1f, leafState: leaf);
        else
            ShipCad.TagOpeningPressure(hatch, ShipPressureClass.Habitable, 1.0f, 2.1f, leafState: leaf);

        var doc = new CadDocument
        {
            Name = "Fixture exterior hatch",
            Entities = [shell, cabin, hatch],
        };
        ShipDocumentMetrics.SetShipEnvelope(doc, loa: 20f, beam: 8f, height: 4f, deckSpacing: 4f);
        return doc;
    }

    /// <summary>
    /// Crossing |D1| ChamberA |D2| ChamberB |D3| exterior — Calypso L-airlock two-barrier layout.
    /// Walls at X=0, X=4, X=8. Spaces: Crossing [-4,0], A [0,4], B [4,8].
    /// </summary>
    private static CadDocument BuildLAirlockFixture(bool d3Open, bool d2Open, bool d1Open)
    {
        var w1 = Wall("D1-host", x: 0f);
        var w2 = Wall("D2-host", x: 4f);
        var w3 = Wall("D3-host", x: 8f);

        var crossing = Space("Crossing", x0: -4f, x1: 0f);
        var chamberA = Space("ChamberA", x0: 0f, x1: 4f);
        var chamberB = Space("ChamberB", x0: 4f, x1: 8f);

        var d1 = Hatch("D1", w1.Id, d1Open, vacuum: false);
        var d2 = Hatch("D2", w2.Id, d2Open, vacuum: false);
        var d3 = Hatch("D3", w3.Id, d3Open, vacuum: true);

        var airlock = ShipCad.CreateAirlock("AL-port", chamberA.Id, outerOpeningId: d3.Id, innerOpeningId: d1.Id);

        var doc = new CadDocument
        {
            Name = "Fixture L-airlock",
            Entities = [w1, w2, w3, crossing, chamberA, chamberB, d1, d2, d3, airlock],
        };
        ShipDocumentMetrics.SetShipEnvelope(doc, loa: 20f, beam: 10f, height: 4f, deckSpacing: 4f);
        return doc;

        static CadEntity Wall(string name, float x) => new()
        {
            Kind = "wall",
            Name = name,
            Deck = 0,
            A = [x, 0f, 0f],
            B = [x, 0f, 4f],
            Thickness = 0.15f,
            Height = 2.4f,
        };

        static CadEntity Space(string name, float x0, float x1) => new()
        {
            Kind = "space",
            Name = name,
            Deck = 0,
            Height = 2.4f,
            Points =
            [
                [x0, 0f, 0f],
                [x1, 0f, 0f],
                [x1, 0f, 4f],
                [x0, 0f, 4f],
            ],
        };

        static CadEntity Hatch(string name, Guid hostWallId, bool open, bool vacuum)
        {
            var h = new CadEntity
            {
                Kind = "opening",
                Name = name,
                OpeningType = "hatch",
                Deck = 0,
                HostWallId = hostWallId,
                Height = 2.1f,
                Footprint =
                [
                    [-0.5f, 0f, 1.5f],
                    [0.5f, 0f, 1.5f],
                    [0.5f, 0f, 2.5f],
                    [-0.5f, 0f, 2.5f],
                ],
            };
            var leaf = open ? ShipLeafState.Open : ShipLeafState.Closed;
            if (vacuum)
                ShipCad.TagVacuumAssistedHatch(h, 1.0f, 2.1f, leafState: leaf);
            else
                ShipCad.TagOpeningPressure(h, ShipPressureClass.Habitable, 1.0f, 2.1f, leafState: leaf);
            return h;
        }
    }
}
