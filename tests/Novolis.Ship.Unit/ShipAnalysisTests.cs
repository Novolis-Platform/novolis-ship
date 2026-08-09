using Novolis.Ship.Analysis;
using Novolis.Ship.Design;
using Novolis.Ship.Structure;

namespace Novolis.Ship.Unit;

public sealed class ShipAnalysisTests
{
    private static ShipDefinition SampleDefinition() => new()
    {
        Name = "Analysis Freighter",
        Length = ShipLengths.FromMeters(90f),
        Beam = ShipLengths.FromMeters(20f),
        Height = ShipLengths.FromMeters(12f),
        DeckCount = 3,
        DeckSpacing = ShipLengths.FromMeters(4f),
        HullMaterial = MaterialId.Steel,
        HullThickness = ShipLengths.FromMeters(0.024f),
        FrameSpacing = ShipLengths.FromMeters(1.5f),
        PrimaryStructuralMaterial = MaterialId.Steel,
        HullGenerator = HullGeneratorKind.Faceted,
        GravitySystem = GravitySystemKind.Plating,
        NominalGravityG = 1f,
        NominalInternalPressureAtm = 1f,
        ExternalEnvironment = ExternalEnvironmentKind.Vacuum,
    };

    [Test]
    public async Task Factory_ship_analysis_has_mass_and_mostly_green_pressure()
    {
        var design = ShipFactory.Create(SampleDefinition());
        var report = ShipAnalyzer.Analyze(design);
        await Assert.That(report.TotalMassKg).IsGreaterThan(1000f);
        await Assert.That(report.Categories.Count).IsEqualTo(6);
        await Assert.That(report.StatusOf(AnalysisCategory.Pressure)).IsEqualTo(AnalysisSeverity.Green);
    }

    [Test]
    public async Task Narrow_passage_marks_clearance_red()
    {
        var design = ShipFactory.Create(SampleDefinition());
        var deck = design.Decks[1];
        design = ShipDesignMutations.AddPassage(
            design, deck.Id, "Tight", [[-2f, -5f], [-2f, 5f]], widthM: 0.7f, heightM: 2f);
        var report = ShipAnalyzer.Analyze(design);
        await Assert.That(report.StatusOf(AnalysisCategory.Clearance)).IsEqualTo(AnalysisSeverity.Red);
        await Assert.That(report.Findings.Any(f => f.Code == "PASSAGE_WIDTH")).IsTrue();
    }

    [Test]
    public async Task Heavy_frame_cutouts_mark_structure()
    {
        var design = ShipFactory.Create(SampleDefinition());
        var deck = design.Decks[1];
        // Many passages → many cutouts on frames.
        for (var i = 0; i < 8; i++)
        {
            design = ShipDesignMutations.AddPassage(
                design,
                deck.Id,
                $"P{i}",
                [[i - 4f, -20f], [i - 4f, 20f]],
                widthM: 1.2f,
                heightM: 2.2f);
        }

        var report = ShipAnalyzer.Analyze(design);
        await Assert.That(report.StatusOf(AnalysisCategory.Structure)).IsNotEqualTo(AnalysisSeverity.Green);
    }
}
