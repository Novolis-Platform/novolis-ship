using System.Text.Json;
using Novolis.Cad.Primitives;

namespace Novolis.Ship.Primitives;

/// <summary>Read/write ship metrics on <see cref="CadDocument.Properties"/>.</summary>
public static class ShipDocumentMetrics
{
    public static float GetLoaMeters(CadDocument document, float fallback = 69f) =>
        GetFloat(document.Properties, ShipPropertyKeys.ShipLoaMeters, fallback);

    public static float GetBeamMeters(CadDocument document, float fallback = 20f) =>
        GetFloat(document.Properties, ShipPropertyKeys.BeamMeters, fallback);

    public static float GetHeightMeters(CadDocument document, float fallback = 12f) =>
        GetFloat(document.Properties, ShipPropertyKeys.HeightMeters, fallback);

    public static float GetDeckSpacingMeters(CadDocument document, float fallback = 4f) =>
        GetFloat(document.Properties, ShipPropertyKeys.DeckSpacingMeters, fallback);

    public static void SetShipEnvelope(CadDocument document, float loa, float beam, float height, float deckSpacing)
    {
        ArgumentNullException.ThrowIfNull(document);
        document.Properties ??= new Dictionary<string, JsonElement>();
        document.Properties[ShipPropertyKeys.ShipLoaMeters] = JsonSerializer.SerializeToElement(loa);
        document.Properties[ShipPropertyKeys.BeamMeters] = JsonSerializer.SerializeToElement(beam);
        document.Properties[ShipPropertyKeys.HeightMeters] = JsonSerializer.SerializeToElement(height);
        document.Properties[ShipPropertyKeys.DeckSpacingMeters] = JsonSerializer.SerializeToElement(deckSpacing);
    }

    private static float GetFloat(Dictionary<string, JsonElement>? props, string key, float fallback)
    {
        if (props is null || !props.TryGetValue(key, out var el))
            return fallback;
        return el.ValueKind switch
        {
            JsonValueKind.Number => el.GetSingle(),
            JsonValueKind.String when float.TryParse(el.GetString(), out var v) => v,
            _ => fallback,
        };
    }
}
