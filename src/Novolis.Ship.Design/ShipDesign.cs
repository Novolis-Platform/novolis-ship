using Novolis.Cad.Primitives;

namespace Novolis.Ship.Design;

/// <summary>Semantic ship design object graph (not a flattened CAD document or scene).</summary>
public sealed record ShipDesign
{
    public const string FormatId = "novolis.ship";
    public const int CurrentSchemaVersion = 1;

    public string Format { get; init; } = FormatId;
    public int SchemaVersion { get; init; } = CurrentSchemaVersion;
    public string? CreatedAt { get; init; }
    public string? ModifiedAt { get; init; }

    public required ShipDefinition Ship { get; init; }
    public required HullDesign Hull { get; init; }

    public required IReadOnlyList<DeckDesign> Decks { get; init; }
    public required IReadOnlyList<FrameDesign> Frames { get; init; }
    public required IReadOnlyList<LongitudinalDesign> Longitudinals { get; init; }

    public required IReadOnlyList<BulkheadDesign> Bulkheads { get; init; }
    public required IReadOnlyList<CompartmentDesign> Compartments { get; init; }
    public required IReadOnlyList<PassageDesign> Passages { get; init; }
    public required IReadOnlyList<OpeningDesign> Openings { get; init; }
    public required IReadOnlyList<EquipmentDesign> Equipment { get; init; }

    public IReadOnlyList<StructuralCutout> Cutouts { get; init; } = [];

    public IEnumerable<(ShipObjectId Id, CadDocument Geometry, string Kind)> GeometricObjects()
    {
        yield return (Hull.Id.AsObject(), Hull.Geometry, "hull");
        foreach (var d in Decks)
            yield return (d.Id.AsObject(), d.Geometry, "deck");
        foreach (var f in Frames)
            yield return (f.Id.AsObject(), f.Geometry, "frame");
        foreach (var l in Longitudinals)
            yield return (l.Id.AsObject(), l.Geometry, "longitudinal");
        foreach (var b in Bulkheads)
            yield return (b.Id.AsObject(), b.Geometry, "bulkhead");
        foreach (var c in Compartments)
            yield return (c.Id.AsObject(), c.Geometry, "compartment");
        foreach (var p in Passages)
            yield return (p.Id.AsObject(), p.Geometry, "passage");
        foreach (var o in Openings)
            yield return (o.Id.AsObject(), o.Geometry, "opening");
        foreach (var e in Equipment)
            yield return (e.Id.AsObject(), e.Geometry, "equipment");
    }
}
