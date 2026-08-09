using Novolis.Cad.Primitives;
using Novolis.Ship.Primitives;
using Novolis.Ship.Structure;

namespace Novolis.Ship.Unit;

public sealed class SkinMassRollupTests
{
    /// <summary>Golden values from CAL-HULL-CAD-001.json massProperties (LOA 69 OML skin).</summary>
    private const float GoldenAreaM2 = 3796.055f;
    private const float GoldenMassKg = 242947.52f;
    private const float MassToleranceKg = 2f;

    [Test]
    public async Task Calypso_316L_skin_mass_matches_manufacturer_golden()
    {
        var spec = PlateMaterialSpec.Aisi316L_8mm;
        var mass = SkinMassRollup.FromFacetAreas(GoldenAreaM2, spec);

        await Assert.That(mass.ThicknessM).IsEqualTo(0.008f);
        await Assert.That(mass.ArealDensityKgPerM2).IsEqualTo(64f);
        await Assert.That(System.Math.Abs(mass.MassKg - GoldenMassKg)).IsLessThan(MassToleranceKg);
        await Assert.That(System.Math.Abs(mass.MassT - 242.948f)).IsLessThan(0.01f);
    }

    [Test]
    public async Task Attach_round_trips_on_cad_document()
    {
        var spec = PlateMaterialSpec.Aisi316L_8mm;
        var mass = SkinMassRollup.FromFacetAreas(GoldenAreaM2, spec);
        var bom = new ShipBom
        {
            Drawing = "CAL-HULL-CAD-001",
            Rev = "B",
            Material = spec,
            SkinMass = mass,
            Lines = [SkinMassRollup.ToBomLine(mass)],
        };

        var doc = new CadDocument { Name = "Calypso structure fixture" };
        ShipDocumentMetrics.SetShipEnvelope(doc, 69f, 20f, 12f, 4f);
        ShipStructureDocument.Attach(doc, spec, bom, mass);

        await Assert.That(ShipStructureDocument.TryGetMaterial(doc, out var m)).IsTrue();
        await Assert.That(m!.Designation).IsEqualTo("AISI 316L");
        await Assert.That(ShipStructureDocument.TryGetMass(doc, out var mass2)).IsTrue();
        await Assert.That(System.Math.Abs(mass2!.MassKg - GoldenMassKg)).IsLessThan(MassToleranceKg);
        await Assert.That(ShipStructureDocument.TryGetBom(doc, out var bom2)).IsTrue();
        await Assert.That(bom2!.Lines.Count).IsEqualTo(1);
        await Assert.That(bom2.TotalMassKg).IsGreaterThan(240_000f);
    }
}
