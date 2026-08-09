using Novolis.Cad.Primitives;
using Novolis.Ship.Primitives;
using Novolis.Ship.Topology;

namespace Novolis.Ship.Validation;

public enum ShipValidationSeverity
{
    Info,
    Warning,
    Error,
}

public sealed record ShipValidationIssue(
    string Code,
    ShipValidationSeverity Severity,
    string Message,
    Guid? EntityId = null);

public sealed class ShipValidationResult
{
    public required IReadOnlyList<ShipValidationIssue> Issues { get; init; }
    public bool Ok => Issues.All(i => i.Severity != ShipValidationSeverity.Error);
}

/// <summary>Ship document validation rules.</summary>
public static class ShipValidator
{
    public const float MinPersonnelClearWidthMeters = 1.0f;

    public static ShipValidationResult Validate(CadDocument document, ShipTopologyResult? topology = null)
    {
        ArgumentNullException.ThrowIfNull(document);
        topology ??= ShipTopology.Analyze(document);
        var issues = new List<ShipValidationIssue>();
        var byId = document.Entities.ToDictionary(e => e.Id);

        foreach (var opening in ShipCad.Openings(document))
        {
            var type = opening.OpeningType ?? "door";
            if (type is "door" or "hatch")
            {
                var clear = ShipCad.GetClearWidth(opening, fallback: opening.Thickness > 0 ? opening.Thickness : 0f);
                if (clear < MinPersonnelClearWidthMeters)
                {
                    issues.Add(new ShipValidationIssue(
                        "SHIP_CLEAR_WIDTH",
                        ShipValidationSeverity.Error,
                        $"Personnel {type} clear width {clear:0.###} m < {MinPersonnelClearWidthMeters:0.0} m.",
                        opening.Id));
                }
            }

            if (opening.HostWallId is null || !byId.ContainsKey(opening.HostWallId.Value))
            {
                issues.Add(new ShipValidationIssue(
                    "SHIP_ORPHAN_OPENING",
                    ShipValidationSeverity.Error,
                    $"Opening '{opening.Name ?? opening.Id.ToString("N")[..8]}' has no host wall.",
                    opening.Id));
            }
        }

        foreach (var oid in topology.OrphanOpeningIds)
        {
            if (issues.Any(i => i.EntityId == oid && i.Code == "SHIP_ORPHAN_OPENING"))
                continue;
            issues.Add(new ShipValidationIssue(
                "SHIP_ORPHAN_OPENING",
                ShipValidationSeverity.Error,
                "Opening missing host wall (topology).",
                oid));
        }

        foreach (var airlockEntity in document.Entities.Where(e =>
                     string.Equals(e.Kind, ShipEntityKinds.Airlock, StringComparison.OrdinalIgnoreCase)))
        {
            if (!ShipCad.TryReadAirlock(airlockEntity, out var info))
            {
                issues.Add(new ShipValidationIssue(
                    "SHIP_AIRLOCK_MALFORMED",
                    ShipValidationSeverity.Error,
                    $"Airlock '{airlockEntity.Name}' is missing vestibule/outer/inner ids.",
                    airlockEntity.Id));
                continue;
            }

            if (!byId.ContainsKey(info.VestibuleSpaceId))
                issues.Add(new ShipValidationIssue("SHIP_AIRLOCK_VESTIBULE", ShipValidationSeverity.Error,
                    "Airlock vestibule space missing.", airlockEntity.Id));
            if (!byId.ContainsKey(info.OuterOpeningId) || !byId.ContainsKey(info.InnerOpeningId))
                issues.Add(new ShipValidationIssue("SHIP_AIRLOCK_HATCHES", ShipValidationSeverity.Error,
                    "Airlock outer/inner opening missing.", airlockEntity.Id));
            else if (info.OuterOpeningId == info.InnerOpeningId)
                issues.Add(new ShipValidationIssue("SHIP_AIRLOCK_SAME_HATCH", ShipValidationSeverity.Error,
                    "Airlock outer and inner openings must differ.", airlockEntity.Id));
        }

        var loa = ShipDocumentMetrics.GetLoaMeters(document);
        var beam = ShipDocumentMetrics.GetBeamMeters(document);
        if (loa <= 0f || beam <= 0f)
        {
            issues.Add(new ShipValidationIssue(
                "SHIP_ENVELOPE",
                ShipValidationSeverity.Warning,
                "Ship LOA/beam document properties missing or non-positive.",
                null));
        }

        foreach (var space in ShipCad.Spaces(document))
        {
            if (topology.IsSpaceSealed(space.Id))
                continue;
            issues.Add(new ShipValidationIssue(
                "SHIP_SPACE_VENTS",
                ShipValidationSeverity.Info,
                $"Space '{space.Name}' vents to exterior.",
                space.Id));
        }

        return new ShipValidationResult { Issues = issues };
    }
}
