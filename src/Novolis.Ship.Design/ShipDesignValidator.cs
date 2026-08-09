using Novolis.Ship.Topology;
using Novolis.Ship.Validation;

namespace Novolis.Ship.Design;

/// <summary>Semantic + projected validation for <see cref="ShipDesign"/> (baseline §23).</summary>
public static class ShipDesignValidator
{
    public static ShipValidationResult Validate(ShipDesign design)
    {
        ArgumentNullException.ThrowIfNull(design);
        var issues = new List<ShipValidationIssue>();

        // Closed hull
        if (design.Hull.Geometry.Entities.Count == 0)
        {
            issues.Add(new ShipValidationIssue(
                "SHIP_HULL_EMPTY",
                ShipValidationSeverity.Error,
                "Hull has no geometry entities."));
        }
        else if (!HasClosedHullIntent(design))
        {
            issues.Add(new ShipValidationIssue(
                "SHIP_HULL_OPEN",
                ShipValidationSeverity.Warning,
                "Hull envelope may not be closed (no exterior-tagged solids)."));
        }

        // Deck inside hull
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

        // Opening host validity
        foreach (var opening in design.Openings)
        {
            if (!HostExists(design, opening.HostId))
            {
                issues.Add(new ShipValidationIssue(
                    "SHIP_OPENING_HOST",
                    ShipValidationSeverity.Error,
                    $"Opening '{opening.Name}' host {opening.HostId} is missing."));
            }

            // Hull opening validity
            if (opening.HostId.Value == design.Hull.Id.Value
                && opening.Kind is OpeningKind.Viewport or OpeningKind.CargoDoor or OpeningKind.Airlock)
            {
                if (opening.Geometry.Entities.Count == 0)
                {
                    issues.Add(new ShipValidationIssue(
                        "SHIP_HULL_OPENING",
                        ShipValidationSeverity.Error,
                        $"Hull opening '{opening.Name}' has empty aperture geometry."));
                }
            }
        }

        // Passage clearance
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

        // Equipment clearance
        foreach (var equip in design.Equipment)
        {
            var clearance = ShipLengths.ToMeters(equip.ServiceClearance);
            if (clearance < 0f)
            {
                issues.Add(new ShipValidationIssue(
                    "SHIP_EQUIPMENT_CLEARANCE",
                    ShipValidationSeverity.Error,
                    $"Equipment '{equip.Name}' has negative service clearance."));
            }
            else if (equip.Geometry.Entities.Count == 0)
            {
                issues.Add(new ShipValidationIssue(
                    "SHIP_EQUIPMENT_CLEARANCE",
                    ShipValidationSeverity.Warning,
                    $"Equipment '{equip.Name}' has no bounding geometry."));
            }
        }

        // Structural cutout intersections / frame cutout severity
        var cutoutsByHost = design.Cutouts.GroupBy(c => c.HostId.Value);
        foreach (var g in cutoutsByHost)
        {
            if (g.Count() >= 6)
            {
                var frame = design.Frames.FirstOrDefault(f => f.Id.Value == g.Key);
                if (frame is not null)
                {
                    issues.Add(new ShipValidationIssue(
                        "SHIP_FRAME_CUTOUT_SEVERITY",
                        ShipValidationSeverity.Warning,
                        $"Frame '{frame.Name}' has {g.Count()} structural cutouts."));
                }
            }

            if (g.Count() >= 2)
            {
                issues.Add(new ShipValidationIssue(
                    "SHIP_CUTOUT_INTERSECTION",
                    ShipValidationSeverity.Info,
                    $"Host {g.Key:N} has {g.Count()} intersecting structural cutouts."));
            }
        }

        // Shared compartment boundaries should not imply duplicate bulkheads (info).
        var shared = CompartmentBoundaryResolver.FindSharedEdges(design);
        if (shared.Count > 0)
        {
            issues.Add(new ShipValidationIssue(
                "SHIP_SHARED_BOUNDARY",
                ShipValidationSeverity.Info,
                $"{shared.Count} shared compartment edge(s) — represent as one physical bulkhead."));
        }

        // Airtight compartment topology via flat CAD projector.
        var flat = ShipCadProjector.ToCadDocument(design);
        var topo = ShipTopology.Analyze(flat);
        var projected = ShipValidator.Validate(flat, topo);
        issues.AddRange(projected.Issues);

        return new ShipValidationResult { Issues = issues };
    }

    private static bool HasClosedHullIntent(ShipDesign design)
    {
        foreach (var e in design.Hull.Geometry.Entities)
        {
            if (e.Properties is not null
                && e.Properties.TryGetValue(Ship.Primitives.ShipPropertyKeys.Exterior, out _))
                return true;
            if (!string.IsNullOrWhiteSpace(e.Name)
                && e.Name.StartsWith("ext-", StringComparison.OrdinalIgnoreCase))
                return true;
            if (e.Kind is "box" or "cylinder" or "wall")
                return true;
        }

        return false;
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
