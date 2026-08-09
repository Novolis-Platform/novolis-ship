using System.Text.Json;
using Novolis.Cad.Primitives;
using Novolis.Ship.Primitives;

namespace Novolis.Ship.Structure;

/// <summary>Attach / read structure material, BOM, and mass on <see cref="CadDocument.Properties"/>.</summary>
public static class ShipStructureDocument
{
    public static void Attach(CadDocument document, PlateMaterialSpec material, ShipBom bom, SkinMassResult mass)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(material);
        ArgumentNullException.ThrowIfNull(bom);
        ArgumentNullException.ThrowIfNull(mass);
        document.Properties ??= new Dictionary<string, JsonElement>();
        document.Properties[ShipPropertyKeys.StructureMaterial] = JsonSerializer.SerializeToElement(material);
        document.Properties[ShipPropertyKeys.StructureBom] = JsonSerializer.SerializeToElement(bom);
        document.Properties[ShipPropertyKeys.StructureMass] = JsonSerializer.SerializeToElement(mass);
    }

    public static bool TryGetMaterial(CadDocument document, out PlateMaterialSpec? material)
    {
        material = null;
        if (!TryGet(document, ShipPropertyKeys.StructureMaterial, out var el))
            return false;
        material = JsonSerializer.Deserialize<PlateMaterialSpec>(el.GetRawText());
        return material is not null;
    }

    public static bool TryGetBom(CadDocument document, out ShipBom? bom)
    {
        bom = null;
        if (!TryGet(document, ShipPropertyKeys.StructureBom, out var el))
            return false;
        bom = JsonSerializer.Deserialize<ShipBom>(el.GetRawText());
        return bom is not null;
    }

    public static bool TryGetMass(CadDocument document, out SkinMassResult? mass)
    {
        mass = null;
        if (!TryGet(document, ShipPropertyKeys.StructureMass, out var el))
            return false;
        mass = JsonSerializer.Deserialize<SkinMassResult>(el.GetRawText());
        return mass is not null;
    }

    private static bool TryGet(CadDocument document, string key, out JsonElement el)
    {
        el = default;
        if (document.Properties is null)
            return false;
        if (document.Properties.TryGetValue(key, out el))
            return true;
        foreach (var kv in document.Properties)
        {
            if (string.Equals(kv.Key, key, StringComparison.OrdinalIgnoreCase))
            {
                el = kv.Value;
                return true;
            }
        }

        return false;
    }
}
