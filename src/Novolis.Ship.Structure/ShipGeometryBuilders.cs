using Novolis.Cad.Primitives;
using Novolis.Ship.Primitives;

namespace Novolis.Ship.Structure;

/// <summary>CadDocument construction helpers for primary ship structure.</summary>
public static class ShipGeometryBuilders
{
    public static CadDocument NewGeometryDoc(string name, string generator = "Novolis.Ship.Structure")
    {
        var now = DateTimeOffset.UtcNow.ToString("O");
        return new CadDocument
        {
            Name = name,
            CreatedAt = now,
            ModifiedAt = now,
            Generator = new CadGenerator { Name = generator, Version = "2026.1.0" },
            Layers = [new CadLayer { Name = "0" }],
        };
    }

    public static CadDocument BuildBoxHull(float lengthM, float beamM, float heightM, string material, bool exterior = true)
    {
        var doc = NewGeometryDoc("Hull");
        var box = new CadEntity
        {
            Kind = "box",
            Name = "hull-shell",
            Material = material,
            Center = [0f, heightM * 0.5f, 0f],
            HalfExtents = [beamM * 0.5f, heightM * 0.5f, lengthM * 0.5f],
        };
        if (exterior)
            TagExterior(box);
        doc.Entities.Add(box);
        return doc;
    }

    public static CadDocument BuildTaperedBoxHull(float lengthM, float beamM, float heightM, string material)
    {
        var doc = NewGeometryDoc("Hull");
        var mid = new CadEntity
        {
            Kind = "box",
            Name = "hull-mid",
            Material = material,
            Center = [0f, heightM * 0.5f, 0f],
            HalfExtents = [beamM * 0.5f, heightM * 0.5f, lengthM * 0.35f],
        };
        TagExterior(mid);
        var bow = new CadEntity
        {
            Kind = "box",
            Name = "hull-bow",
            Material = material,
            Center = [0f, heightM * 0.45f, lengthM * 0.35f],
            HalfExtents = [beamM * 0.35f, heightM * 0.4f, lengthM * 0.15f],
        };
        TagExterior(bow);
        var stern = new CadEntity
        {
            Kind = "box",
            Name = "hull-stern",
            Material = material,
            Center = [0f, heightM * 0.45f, -lengthM * 0.35f],
            HalfExtents = [beamM * 0.4f, heightM * 0.4f, lengthM * 0.15f],
        };
        TagExterior(stern);
        doc.Entities.AddRange([mid, bow, stern]);
        return doc;
    }

    public static CadDocument BuildDeckPlate(string name, float lengthM, float beamM, float elevationM, float thicknessM = 0.05f)
    {
        var doc = NewGeometryDoc(name);
        doc.Entities.Add(new CadEntity
        {
            Kind = "box",
            Name = name,
            Center = [0f, elevationM, 0f],
            HalfExtents = [beamM * 0.48f, thicknessM * 0.5f, lengthM * 0.48f],
            Material = "steel",
        });
        return doc;
    }

    public static CadDocument BuildFrameAtStation(string name, float stationZ, float beamM, float heightM, float webThicknessM, string material)
    {
        var doc = NewGeometryDoc(name);
        // Closed frame profile as a thin transverse plate (continuous structure).
        doc.Entities.Add(new CadEntity
        {
            Kind = "box",
            Name = name,
            Material = material,
            Center = [0f, heightM * 0.5f, stationZ],
            HalfExtents = [beamM * 0.48f, heightM * 0.48f, webThicknessM * 0.5f],
        });
        return doc;
    }

    public static CadDocument BuildLongitudinal(string name, float lengthM, float y, float x, float heightM, float thicknessM, string material)
    {
        var doc = NewGeometryDoc(name);
        doc.Entities.Add(new CadEntity
        {
            Kind = "box",
            Name = name,
            Material = material,
            Center = [x, y, 0f],
            HalfExtents = [thicknessM * 0.5f, heightM * 0.5f, lengthM * 0.48f],
        });
        return doc;
    }

    /// <summary>Path-authored bulkhead: wall entities along polyline points (XZ), extruded by height/thickness.</summary>
    public static CadDocument BuildBulkheadPath(
        string name,
        IReadOnlyList<float[]> pathXz,
        float thicknessM,
        float heightM,
        float deckElevationM,
        string material,
        int deckIndex = 0)
    {
        var doc = NewGeometryDoc(name);
        if (pathXz.Count < 2)
            return doc;

        for (var i = 0; i < pathXz.Count - 1; i++)
        {
            var a = pathXz[i];
            var b = pathXz[i + 1];
            doc.Entities.Add(new CadEntity
            {
                Kind = "wall",
                Name = $"{name}-seg{i}",
                Material = material,
                Deck = deckIndex,
                A = [a[0], deckElevationM, a.Length > 1 ? a[1] : 0f],
                B = [b[0], deckElevationM, b.Length > 1 ? b[1] : 0f],
                Thickness = thicknessM,
                Height = heightM,
            });
        }

        return doc;
    }

    public static CadDocument BuildPassageVolume(
        string name,
        IReadOnlyList<float[]> pathXz,
        float widthM,
        float heightM,
        float deckElevationM,
        int deckIndex = 0)
    {
        var doc = NewGeometryDoc(name);
        if (pathXz.Count < 2)
            return doc;

        for (var i = 0; i < pathXz.Count - 1; i++)
        {
            var a = pathXz[i];
            var b = pathXz[i + 1];
            var ax = a[0];
            var az = a.Length > 1 ? a[1] : 0f;
            var bx = b[0];
            var bz = b.Length > 1 ? b[1] : 0f;
            var mx = (ax + bx) * 0.5f;
            var mz = (az + bz) * 0.5f;
            var dx = bx - ax;
            var dz = bz - az;
            var len = MathF.Sqrt(dx * dx + dz * dz);
            if (len < 1e-4f)
                continue;
            doc.Entities.Add(new CadEntity
            {
                Kind = "box",
                Name = $"{name}-seg{i}",
                Deck = deckIndex,
                Center = [mx, deckElevationM + heightM * 0.5f, mz],
                HalfExtents = [widthM * 0.5f, heightM * 0.5f, len * 0.5f],
                RotationY = MathF.Atan2(dx, dz),
            });
        }

        return doc;
    }

    public static CadDocument BuildCompartmentBoundary(
        string name,
        IReadOnlyList<float[]> closedPolyXz,
        float heightM,
        float deckElevationM,
        int deckIndex = 0)
    {
        var doc = NewGeometryDoc(name);
        var points = closedPolyXz.Select(p => new float[] { p[0], deckElevationM, p.Length > 1 ? p[1] : 0f }).ToList();
        doc.Entities.Add(new CadEntity
        {
            Kind = "space",
            Name = name,
            Deck = deckIndex,
            Height = heightM,
            Points = points,
            Flags = new CadSpaceFlags { Enclosed = true, Hollow = true },
        });
        return doc;
    }

    public static CadDocument BuildOpeningAperture(
        string name,
        float clearWidthM,
        float clearHeightM,
        float[] center,
        string openingType = "door")
    {
        var doc = NewGeometryDoc(name);
        doc.Entities.Add(new CadEntity
        {
            Kind = "box",
            Name = name,
            OpeningType = openingType,
            Center = center,
            HalfExtents = [clearWidthM * 0.5f, clearHeightM * 0.5f, 0.1f],
        });
        return doc;
    }

    public static CadDocument BuildEquipmentEnvelope(string name, float[] center, float[] halfExtents, float massKg = 0f)
    {
        var doc = NewGeometryDoc(name);
        var ent = new CadEntity
        {
            Kind = "box",
            Name = name,
            Center = center,
            HalfExtents = halfExtents,
        };
        ent.Properties ??= new Dictionary<string, System.Text.Json.JsonElement>();
        ent.Properties["massKg"] = System.Text.Json.JsonSerializer.SerializeToElement(massKg);
        doc.Entities.Add(ent);
        return doc;
    }

    private static void TagExterior(CadEntity entity)
    {
        entity.Properties ??= new Dictionary<string, System.Text.Json.JsonElement>();
        entity.Properties[ShipPropertyKeys.Exterior] = System.Text.Json.JsonSerializer.SerializeToElement(true);
        if (string.IsNullOrWhiteSpace(entity.Name) || !entity.Name.StartsWith("ext-", StringComparison.OrdinalIgnoreCase))
            entity.Name = "ext-" + (entity.Name ?? "hull");
    }
}
