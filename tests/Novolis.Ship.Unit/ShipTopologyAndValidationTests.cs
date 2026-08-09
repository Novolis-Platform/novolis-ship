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
}
