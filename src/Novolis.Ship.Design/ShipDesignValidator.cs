using Novolis.Ship.Topology;
using Novolis.Ship.Validation;

namespace Novolis.Ship.Design;

/// <summary>Semantic + projected validation for <see cref="ShipDesign"/>.</summary>
public static class ShipDesignValidator
{
    public static ShipValidationResult Validate(ShipDesign design)
    {
        ArgumentNullException.ThrowIfNull(design);
        var issues = new List<ShipValidationIssue>();

        if (design.Hull.Geometry.Entities.Count == 0)
        {
            issues.Add(new ShipValidationIssue(
                "SHIP_HULL_EMPTY",
                ShipValidationSeverity.Error,
                "Hull has no geometry entities."));
        }

        foreach (var deck in design.Decks)
        {
            var elev = ShipLengths.ToMeters(deck.Elevation);
            if (elev < -0.01f || elev > design.Ship.HeightMeters + 0.01f)
            {
                issues.Add(new ShipValidationIssue(
                    "SHIP_DECK_OUTSIDE_HULL",
                    ShipValidationSeverity.Error,
                    $"Deck '{deck.Name}' elevation {elev:0.###} m is outside hull height."));
            }
        }

        foreach (var opening in design.Openings)
        {
            if (!HostExists(design, opening.HostId))
            {
                issues.Add(new ShipValidationIssue(
                    "SHIP_OPENING_HOST",
                    ShipValidationSeverity.Error,
                    $"Opening '{opening.Name}' host {opening.HostId} is missing."));
            }
        }

        foreach (var passage in design.Passages)
        {
            var clear = ShipLengths.ToMeters(passage.Width);
            if (string.Equals(passage.ClearanceClass, "personnel", StringComparison.OrdinalIgnoreCase)
                && clear < ShipValidator.MinPersonnelClearWidthMeters)
            {
                issues.Add(new ShipValidationIssue(
                    "SHIP_PASSAGE_CLEARANCE",
                    ShipValidationSeverity.Error,
                    $"Passage '{passage.Name}' width {clear:0.###} m < {ShipValidator.MinPersonnelClearWidthMeters:0.0} m."));
            }
        }

        // Frame cutout severity: warn when a frame has many cutouts.
        var cutoutsByHost = design.Cutouts.GroupBy(c => c.HostId.Value);
        foreach (var g in cutoutsByHost)
        {
            if (g.Count() < 6)
                continue;
            var frame = design.Frames.FirstOrDefault(f => f.Id.Value == g.Key);
            if (frame is null)
                continue;
            issues.Add(new ShipValidationIssue(
                "SHIP_FRAME_CUTOUT_SEVERITY",
                ShipValidationSeverity.Warning,
                $"Frame '{frame.Name}' has {g.Count()} structural cutouts."));
        }

        // Projected airtight / hatch rules via flat CAD.
        var flat = ShipCadProjector.ToCadDocument(design);
        var topo = ShipTopology.Analyze(flat);
        var projected = ShipValidator.Validate(flat, topo);
        issues.AddRange(projected.Issues);

        return new ShipValidationResult { Issues = issues };
    }

    private static bool HostExists(ShipDesign design, ShipObjectId hostId)
    {
        if (design.Hull.Id.Value == hostId.Value)
            return true;
        if (design.Decks.Any(d => d.Id.Value == hostId.Value))
            return true;
        if (design.Frames.Any(f => f.Id.Value == hostId.Value))
            return true;
        if (design.Bulkheads.Any(b => b.Id.Value == hostId.Value))
            return true;
        if (design.Longitudinals.Any(l => l.Id.Value == hostId.Value))
            return true;
        return false;
    }
}
