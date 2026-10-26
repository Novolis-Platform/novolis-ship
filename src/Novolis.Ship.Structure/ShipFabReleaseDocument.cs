using System.Text.Json;
using Novolis.Cad.Primitives;
using Novolis.Ship.Primitives;

namespace Novolis.Ship.Structure;

/// <summary>Attach / read fab release manifest on <see cref="CadDocument.Properties"/>.</summary>
public static class ShipFabReleaseDocument
{
    public static void Attach(CadDocument document, FabReleaseManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(manifest);
        document.Properties ??= new Dictionary<string, JsonElement>();
        document.Properties[ShipPropertyKeys.FabReleaseManifest] = JsonSerializer.SerializeToElement(manifest);
        document.Properties[ShipPropertyKeys.FabReleasePackageId] =
            JsonSerializer.SerializeToElement(manifest.PackageId);
    }

    public static bool TryGetManifest(CadDocument document, out FabReleaseManifest? manifest)
    {
        manifest = null;
        if (document.Properties is null)
            return false;
        if (!document.Properties.TryGetValue(ShipPropertyKeys.FabReleaseManifest, out var el))
        {
            foreach (var kv in document.Properties)
            {
                if (string.Equals(kv.Key, ShipPropertyKeys.FabReleaseManifest, StringComparison.OrdinalIgnoreCase))
                {
                    el = kv.Value;
                    break;
                }
            }
        }

        if (el.ValueKind == JsonValueKind.Undefined)
            return false;
        manifest = JsonSerializer.Deserialize<FabReleaseManifest>(el.GetRawText());
        return manifest is not null;
    }
}
