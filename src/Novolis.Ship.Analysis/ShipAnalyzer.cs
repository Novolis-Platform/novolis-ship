using Novolis.Ship.Design;
using Novolis.Ship.Structure;
using Novolis.Ship.Topology;
using Novolis.Ship.Validation;

namespace Novolis.Ship.Analysis;

/// <summary>
/// Continuous spacecraft plausibility analysis (not certification).
/// Results use GREEN / YELLOW / RED heuristics from semantic model + topology.
/// </summary>
public static class ShipAnalyzer
{
    public static ShipAnalysisReport Analyze(ShipDesign design, ShipAnalysisContext? context = null)
    {
        ArgumentNullException.ThrowIfNull(design);
        context ??= new ShipAnalysisContext();
        var findings = new List<AnalysisFinding>();

        var loadCase = ResolveLoadCase(design, context.ActiveLoadCaseId);
        var mass = AnalyzeMass(design, findings);
        AnalyzeGravityLoads(design, loadCase, mass, findings);
        AnalyzePressure(design, findings);
        var cascade = AnalyzeDecompression(design, context.BreachCompartmentId, findings);
        AnalyzeStructure(design, findings);
        AnalyzeClearance(design, context, findings);

        var categories = Enum.GetValues<AnalysisCategory>()
            .Select(cat =>
            {
                var catFindings = findings.Where(f => f.Category == cat).ToList();
                var severity = catFindings.Count == 0
                    ? AnalysisSeverity.Green
                    : catFindings.Max(f => f.Severity);
                return new CategoryStatus(cat, severity, catFindings.Count);
            })
            .ToList();

        return new ShipAnalysisReport
        {
            Categories = categories,
            Findings = findings,
            TotalMassKg = mass.TotalKg,
            CenterOfMassX = mass.CgX,
            CenterOfMassY = mass.CgY,
            CenterOfMassZ = mass.CgZ,
            DecompressionCascade = cascade,
            ActiveLoadCaseId = loadCase.Id,
        };
    }

    private static ShipLoadCase ResolveLoadCase(ShipDesign design, string? id)
    {
        if (!string.IsNullOrWhiteSpace(id))
        {
            var match = design.LoadCases.FirstOrDefault(c =>
                string.Equals(c.Id, id, StringComparison.OrdinalIgnoreCase));
            if (match is not null)
                return match;
        }

        return design.LoadCases.FirstOrDefault()
            ?? ShipLoadCase.CreateBaseline(design.Environment)[0];
    }

    private sealed record MassResult(float TotalKg, float CgX, float CgY, float CgZ, IReadOnlyList<(DeckId Deck, float MassKg)> ByDeck);

    private static MassResult AnalyzeMass(ShipDesign design, List<AnalysisFinding> findings)
    {
        var L = design.Ship.LengthMeters;
        var B = design.Ship.BeamMeters;
        var H = design.Ship.HeightMeters;
        var thickness = ShipLengths.ToMeters(design.Hull.Thickness);
        var area = 2f * (L * B + L * H + B * H);
        var density = string.Equals(design.Hull.Material.Value, "aluminum", StringComparison.OrdinalIgnoreCase)
            ? 2700f
            : 8000f;
        var hullMass = SkinMassRollup.FromFacetAreas(
            area,
            new PlateMaterialSpec(
                Designation: design.Hull.Material.Value,
                Uns: "heuristic",
                DensityKgPerM3: density,
                ThicknessM: System.Math.Max(0.001f, thickness),
                StockSheetWidthM: 2f,
                StockSheetLengthM: 6f),
            note: "Heuristic OML envelope").MassKg;

        // Frames / longitudinals / decks as coarse steel volume proxies.
        var frameMass = design.Frames.Count * 800f;
        var longMass = design.Longitudinals.Count * 1200f;
        var deckMass = design.Decks.Count * L * B * 0.05f * density * 0.15f;
        var bulkheadMass = design.Bulkheads.Count * B * 3f * 0.05f * density;

        var equipMass = design.Equipment.Sum(e => e.MassKg);
        var total = hullMass + frameMass + longMass + deckMass + bulkheadMass + equipMass;

        float mx = 0, my = 0, mz = 0, w = 0;
        void Acc(float mass, float x, float y, float z)
        {
            if (mass <= 0)
                return;
            mx += mass * x;
            my += mass * y;
            mz += mass * z;
            w += mass;
        }

        Acc(hullMass + frameMass + longMass + bulkheadMass, 0f, H * 0.45f, 0f);
        foreach (var d in design.Decks)
            Acc(deckMass / System.Math.Max(1, design.Decks.Count), 0f, ShipLengths.ToMeters(d.Elevation), 0f);
        foreach (var e in design.Equipment)
        {
            var c = e.Geometry.Entities.FirstOrDefault()?.Center;
            Acc(e.MassKg, c is { Length: >= 1 } ? c[0] : 0f, c is { Length: >= 2 } ? c[1] : H * 0.5f, c is { Length: >= 3 } ? c[2] : 0f);
        }

        var cgX = w > 0 ? mx / w : 0f;
        var cgY = w > 0 ? my / w : H * 0.5f;
        var cgZ = w > 0 ? mz / w : 0f;

        if (System.Math.Abs(cgX) > B * 0.15f)
        {
            findings.Add(new AnalysisFinding(
                AnalysisCategory.Cg,
                System.Math.Abs(cgX) > B * 0.25f ? AnalysisSeverity.Red : AnalysisSeverity.Yellow,
                "CG_TRANSVERSE",
                $"Transverse CG offset {cgX:0.##} m exceeds {B * 0.15f:0.##} m band."));
        }

        if (System.Math.Abs(cgZ) > L * 0.2f)
        {
            findings.Add(new AnalysisFinding(
                AnalysisCategory.Cg,
                System.Math.Abs(cgZ) > L * 0.35f ? AnalysisSeverity.Red : AnalysisSeverity.Yellow,
                "CG_LONGITUDINAL",
                $"Longitudinal CG offset {cgZ:0.##} m is asymmetric."));
        }

        if (total <= 1f)
        {
            findings.Add(new AnalysisFinding(
                AnalysisCategory.Mass,
                AnalysisSeverity.Yellow,
                "MASS_LOW",
                "Total mass estimate is near zero — check hull/equipment data."));
        }

        var byDeck = design.Decks
            .Select(d => (d.Id, deckMass / System.Math.Max(1, design.Decks.Count)
                + design.Equipment.Where(e => EquipmentOnDeck(e, d)).Sum(e => e.MassKg)))
            .ToList();

        return new MassResult(total, cgX, cgY, cgZ, byDeck);
    }

    private static bool EquipmentOnDeck(EquipmentDesign e, DeckDesign deck)
    {
        var c = e.Geometry.Entities.FirstOrDefault()?.Center;
        if (c is not { Length: >= 2 })
            return false;
        var elev = ShipLengths.ToMeters(deck.Elevation);
        return System.Math.Abs(c[1] - elev) < 3f;
    }

    private static void AnalyzeGravityLoads(
        ShipDesign design,
        ShipLoadCase loadCase,
        MassResult mass,
        List<AnalysisFinding> findings)
    {
        var g = design.Environment.NominalGravityG * loadCase.ArtificialGravityFactor;
        if (g <= 0f)
            return;

        var deckArea = System.Math.Max(1f, design.Ship.LengthMeters * design.Ship.BeamMeters * 0.7f);
        foreach (var (deckId, deckMassKg) in mass.ByDeck)
        {
            var deck = design.Decks.FirstOrDefault(d => d.Id.Value == deckId.Value);
            if (deck is null)
                continue;
            var supportedTons = deckMassKg / 1000f * g;
            var avg = supportedTons / deckArea;
            var peak = avg * 4f; // concentration heuristic
            if (avg > 5f || peak > 20f)
            {
                findings.Add(new AnalysisFinding(
                    AnalysisCategory.GravityLoad,
                    peak > 25f || avg > 8f ? AnalysisSeverity.Red : AnalysisSeverity.Yellow,
                    "DECK_LOAD",
                    $"{deck.Name}: contained ~{deckMassKg / 1000f:0.#} t at {g:0.##} g → avg {avg:0.##} t/m², peak ~{peak:0.##} t/m².",
                    deck.Id.Value));
            }
        }
    }

    private static void AnalyzePressure(ShipDesign design, List<AnalysisFinding> findings)
    {
        if (design.Environment.External != ExternalEnvironmentKind.Vacuum)
            return;

        var flat = ShipCadProjector.ToCadDocument(design);
        var topo = ShipTopology.Analyze(flat);
        if (topo.VentingToExterior.Count > 0)
        {
            findings.Add(new AnalysisFinding(
                AnalysisCategory.Pressure,
                AnalysisSeverity.Red,
                "VENT_TO_VACUUM",
                $"{topo.VentingToExterior.Count} space(s) have an open path to exterior vacuum."));
        }

        foreach (var opening in design.Openings.Where(o => o.Kind == OpeningKind.Airlock))
        {
            if (opening.Geometry.Entities.Count == 0)
            {
                findings.Add(new AnalysisFinding(
                    AnalysisCategory.Pressure,
                    AnalysisSeverity.Yellow,
                    "AIRLOCK_GEOMETRY",
                    $"Airlock '{opening.Name}' has empty geometry.",
                    opening.Id.Value));
            }
        }

        if (design.Environment.NominalInternalPressureAtm < 0.5f)
        {
            findings.Add(new AnalysisFinding(
                AnalysisCategory.Pressure,
                AnalysisSeverity.Yellow,
                "LOW_CABIN_PRESSURE",
                $"Nominal cabin pressure {design.Environment.NominalInternalPressureAtm:0.##} atm is low for habitability."));
        }
    }

    private static IReadOnlyList<string> AnalyzeDecompression(
        ShipDesign design,
        Guid? breachId,
        List<AnalysisFinding> findings)
    {
        if (breachId is null)
            return [];

        var breach = design.Compartments.FirstOrDefault(c => c.Id.Value == breachId.Value);
        if (breach is null)
            return [];

        var cascade = new List<string> { breach.Name };
        // Heuristic: open doors/passages on same deck cascade next.
        foreach (var other in design.Compartments.Where(c =>
                     c.DeckId.Value == breach.DeckId.Value && c.Id.Value != breach.Id.Value))
            cascade.Add(other.Name);

        foreach (var passage in design.Passages.Where(p => p.DeckId.Value == breach.DeckId.Value))
            cascade.Add(passage.Name);

        findings.Add(new AnalysisFinding(
            AnalysisCategory.Pressure,
            cascade.Count > 3 ? AnalysisSeverity.Red : AnalysisSeverity.Yellow,
            "DECOMPRESSION_CASCADE",
            $"Breach '{breach.Name}' may expose: {string.Join(", ", cascade.Take(8))}.",
            breach.Id.Value));

        return cascade;
    }

    private static void AnalyzeStructure(ShipDesign design, List<AnalysisFinding> findings)
    {
        var byHost = design.Cutouts.GroupBy(c => c.HostId.Value);
        foreach (var g in byHost)
        {
            var frame = design.Frames.FirstOrDefault(f => f.Id.Value == g.Key);
            if (frame is null)
                continue;
            var removed = System.Math.Min(0.95f, g.Count() * 0.12f);
            var remaining = 1f - removed;
            if (removed >= 0.5f)
            {
                findings.Add(new AnalysisFinding(
                    AnalysisCategory.Structure,
                    removed >= 0.7f ? AnalysisSeverity.Red : AnalysisSeverity.Yellow,
                    "FRAME_CUTOUT",
                    $"Frame '{frame.Name}': ~{removed * 100:0}% web removed by {g.Count()} cutouts (remaining ~{remaining * 100:0}%).",
                    frame.Id.Value));
            }
        }

        var hullCuts = design.Cutouts.Count(c => c.HostId.Value == design.Hull.Id.Value);
        if (hullCuts >= 2)
        {
            findings.Add(new AnalysisFinding(
                AnalysisCategory.Structure,
                hullCuts >= 4 ? AnalysisSeverity.Red : AnalysisSeverity.Yellow,
                "HULL_OPENINGS",
                $"Hull has {hullCuts} structural cutouts — verify reinforcement."));
        }

        // Consecutive weakened frames
        var weak = design.Frames
            .Where(f => design.Cutouts.Count(c => c.HostId.Value == f.Id.Value) >= 4)
            .OrderBy(f => ShipLengths.ToMeters(f.Station))
            .ToList();
        for (var i = 0; i + 1 < weak.Count; i++)
        {
            var a = ShipLengths.ToMeters(weak[i].Station);
            var b = ShipLengths.ToMeters(weak[i + 1].Station);
            if (System.Math.Abs(b - a) <= design.Ship.FrameSpacingMeters * 1.1f)
            {
                findings.Add(new AnalysisFinding(
                    AnalysisCategory.Structure,
                    AnalysisSeverity.Red,
                    "CONSECUTIVE_WEAK_FRAMES",
                    $"Consecutive weakened frames {weak[i].Name} / {weak[i + 1].Name}.",
                    weak[i].Id.Value));
                break;
            }
        }
    }

    private static void AnalyzeClearance(
        ShipDesign design,
        ShipAnalysisContext context,
        List<AnalysisFinding> findings)
    {
        foreach (var passage in design.Passages)
        {
            var w = ShipLengths.ToMeters(passage.Width);
            if (string.Equals(passage.ClearanceClass, "personnel", StringComparison.OrdinalIgnoreCase)
                && w < ShipValidator.MinPersonnelClearWidthMeters)
            {
                findings.Add(new AnalysisFinding(
                    AnalysisCategory.Clearance,
                    AnalysisSeverity.Red,
                    "PASSAGE_WIDTH",
                    $"Passage '{passage.Name}' width {w:0.###} m < {ShipValidator.MinPersonnelClearWidthMeters:0.0} m.",
                    passage.Id.Value));
            }
        }

        // Equipment AABB overlap heuristic from centers/halfExtents.
        for (var i = 0; i < design.Equipment.Count; i++)
        {
            for (var j = i + 1; j < design.Equipment.Count; j++)
            {
                if (!TryBounds(design.Equipment[i], out var a) || !TryBounds(design.Equipment[j], out var b))
                    continue;
                if (Overlaps(a, b))
                {
                    findings.Add(new AnalysisFinding(
                        AnalysisCategory.Clearance,
                        AnalysisSeverity.Red,
                        "EQUIPMENT_COLLISION",
                        $"Equipment '{design.Equipment[i].Name}' intersects '{design.Equipment[j].Name}'.",
                        design.Equipment[i].Id.Value));
                }
            }
        }

        _ = context;
    }

    private static bool TryBounds(EquipmentDesign e, out (float MinX, float MinY, float MinZ, float MaxX, float MaxY, float MaxZ) b)
    {
        b = default;
        var ent = e.Geometry.Entities.FirstOrDefault();
        if (ent?.Center is not { Length: >= 3 } || ent.HalfExtents is not { Length: >= 3 })
            return false;
        var clear = ShipLengths.ToMeters(e.ServiceClearance);
        b = (
            ent.Center[0] - ent.HalfExtents[0] - clear,
            ent.Center[1] - ent.HalfExtents[1] - clear,
            ent.Center[2] - ent.HalfExtents[2] - clear,
            ent.Center[0] + ent.HalfExtents[0] + clear,
            ent.Center[1] + ent.HalfExtents[1] + clear,
            ent.Center[2] + ent.HalfExtents[2] + clear);
        return true;
    }

    private static bool Overlaps(
        (float MinX, float MinY, float MinZ, float MaxX, float MaxY, float MaxZ) a,
        (float MinX, float MinY, float MinZ, float MaxX, float MaxY, float MaxZ) b) =>
        a.MinX <= b.MaxX && a.MaxX >= b.MinX
        && a.MinY <= b.MaxY && a.MaxY >= b.MinY
        && a.MinZ <= b.MaxZ && a.MaxZ >= b.MinZ;
}
