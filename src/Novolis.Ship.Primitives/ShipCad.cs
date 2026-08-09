using System.Text.Json;
using Novolis.Cad.Primitives;

namespace Novolis.Ship.Primitives;

/// <summary>Pressure-volume view over a <c>pressureVolume</c> entity.</summary>
public sealed record PressureVolumeInfo(
    Guid Id,
    string? Name,
    string AtmosphereClass,
    float PressureKPa,
    IReadOnlyList<Guid> MemberSpaceIds,
    IReadOnlyList<Guid> HullEntityIds);

/// <summary>Airlock pair view over an <c>airlock</c> entity.</summary>
public sealed record AirlockInfo(
    Guid Id,
    string? Name,
    Guid VestibuleSpaceId,
    Guid OuterOpeningId,
    Guid InnerOpeningId);

/// <summary>Helpers to create and read ship semantic entities from Cad documents.</summary>
public static class ShipCad
{
    public static CadEntity CreatePressureVolume(
        string name,
        IEnumerable<Guid> memberSpaceIds,
        string atmosphereClass = "habitable",
        float pressureKPa = 101.3f,
        IEnumerable<Guid>? hullEntityIds = null)
    {
        var props = new Dictionary<string, JsonElement>
        {
            [ShipPropertyKeys.AtmosphereClass] = JsonSerializer.SerializeToElement(atmosphereClass),
            [ShipPropertyKeys.PressureKPa] = JsonSerializer.SerializeToElement(pressureKPa),
            [ShipPropertyKeys.MemberSpaceIds] = JsonSerializer.SerializeToElement(memberSpaceIds.Select(g => g.ToString()).ToArray()),
            [ShipPropertyKeys.HullEntityIds] = JsonSerializer.SerializeToElement(
                (hullEntityIds ?? []).Select(g => g.ToString()).ToArray()),
        };
        return new CadEntity
        {
            Kind = ShipEntityKinds.PressureVolume,
            Name = name,
            Properties = props,
        };
    }

    public static CadEntity CreateAirlock(
        string name,
        Guid vestibuleSpaceId,
        Guid outerOpeningId,
        Guid innerOpeningId)
    {
        var props = new Dictionary<string, JsonElement>
        {
            [ShipPropertyKeys.VestibuleSpaceId] = JsonSerializer.SerializeToElement(vestibuleSpaceId.ToString()),
            [ShipPropertyKeys.OuterOpeningId] = JsonSerializer.SerializeToElement(outerOpeningId.ToString()),
            [ShipPropertyKeys.InnerOpeningId] = JsonSerializer.SerializeToElement(innerOpeningId.ToString()),
        };
        return new CadEntity
        {
            Kind = ShipEntityKinds.Airlock,
            Name = name,
            Properties = props,
        };
    }

    public static void TagOpeningPressure(
        CadEntity opening,
        ShipPressureClass pressureClass,
        float clearWidth,
        float clearHeight,
        float sillHeight = 0.15f,
        bool airtightWhenClosed = true,
        ShipLeafState leafState = ShipLeafState.Closed)
    {
        ArgumentNullException.ThrowIfNull(opening);
        opening.Properties ??= new Dictionary<string, JsonElement>();
        opening.Properties[ShipPropertyKeys.PressureClass] = JsonSerializer.SerializeToElement(pressureClass.ToString());
        opening.Properties[ShipPropertyKeys.ClearWidth] = JsonSerializer.SerializeToElement(clearWidth);
        opening.Properties[ShipPropertyKeys.ClearHeight] = JsonSerializer.SerializeToElement(clearHeight);
        opening.Properties[ShipPropertyKeys.SillHeight] = JsonSerializer.SerializeToElement(sillHeight);
        opening.Properties[ShipPropertyKeys.AirtightWhenClosed] = JsonSerializer.SerializeToElement(airtightWhenClosed);
        opening.Properties[ShipPropertyKeys.LeafState] = JsonSerializer.SerializeToElement(leafState.ToString());
        if (opening.Height <= 0f)
            opening.Height = clearHeight;
    }

    public static bool TryReadPressureVolume(CadEntity entity, out PressureVolumeInfo info)
    {
        info = null!;
        if (!string.Equals(entity.Kind, ShipEntityKinds.PressureVolume, StringComparison.OrdinalIgnoreCase))
            return false;
        info = new PressureVolumeInfo(
            entity.Id,
            entity.Name,
            GetString(entity.Properties, ShipPropertyKeys.AtmosphereClass, "habitable"),
            GetFloat(entity.Properties, ShipPropertyKeys.PressureKPa, 101.3f),
            GetGuidList(entity.Properties, ShipPropertyKeys.MemberSpaceIds),
            GetGuidList(entity.Properties, ShipPropertyKeys.HullEntityIds));
        return true;
    }

    public static bool TryReadAirlock(CadEntity entity, out AirlockInfo info)
    {
        info = null!;
        if (!string.Equals(entity.Kind, ShipEntityKinds.Airlock, StringComparison.OrdinalIgnoreCase))
            return false;
        if (!TryGetGuid(entity.Properties, ShipPropertyKeys.VestibuleSpaceId, out var vestibule)
            || !TryGetGuid(entity.Properties, ShipPropertyKeys.OuterOpeningId, out var outer)
            || !TryGetGuid(entity.Properties, ShipPropertyKeys.InnerOpeningId, out var inner))
            return false;
        info = new AirlockInfo(entity.Id, entity.Name, vestibule, outer, inner);
        return true;
    }

    public static float GetClearWidth(CadEntity opening, float fallback = 1f) =>
        GetFloat(opening.Properties, ShipPropertyKeys.ClearWidth, fallback);

    public static float GetClearHeight(CadEntity opening, float fallback = 2.2f) =>
        GetFloat(opening.Properties, ShipPropertyKeys.ClearHeight, fallback);

    public static bool IsAirtightWhenClosed(CadEntity opening) =>
        GetBool(opening.Properties, ShipPropertyKeys.AirtightWhenClosed, defaultValue: true);

    public static ShipLeafState GetLeafState(CadEntity opening)
    {
        var raw = GetString(opening.Properties, ShipPropertyKeys.LeafState, nameof(ShipLeafState.Closed));
        return Enum.TryParse<ShipLeafState>(raw, ignoreCase: true, out var state)
            ? state
            : ShipLeafState.Closed;
    }

    public static bool IsExteriorSolid(CadEntity entity)
    {
        if (TryGetProp(entity.Properties, ShipPropertyKeys.Exterior, out var el))
        {
            if (el.ValueKind == JsonValueKind.True)
                return true;
            if (el.ValueKind == JsonValueKind.String
                && string.Equals(el.GetString(), "true", StringComparison.OrdinalIgnoreCase))
                return true;
        }

        var name = entity.Name ?? "";
        return name.StartsWith("ext-", StringComparison.OrdinalIgnoreCase)
               || name.StartsWith("nacelle-", StringComparison.OrdinalIgnoreCase);
    }

    public static IEnumerable<CadEntity> Spaces(CadDocument document) =>
        document.Entities.Where(e => string.Equals(e.Kind, ShipEntityKinds.Space, StringComparison.OrdinalIgnoreCase));

    public static IEnumerable<CadEntity> Openings(CadDocument document) =>
        document.Entities.Where(e => string.Equals(e.Kind, ShipEntityKinds.Opening, StringComparison.OrdinalIgnoreCase));

    public static IEnumerable<CadEntity> Walls(CadDocument document) =>
        document.Entities.Where(e => string.Equals(e.Kind, ShipEntityKinds.Wall, StringComparison.OrdinalIgnoreCase));

    public static IEnumerable<PressureVolumeInfo> PressureVolumes(CadDocument document)
    {
        foreach (var e in document.Entities)
        {
            if (TryReadPressureVolume(e, out var info))
                yield return info;
        }
    }

    public static IEnumerable<AirlockInfo> Airlocks(CadDocument document)
    {
        foreach (var e in document.Entities)
        {
            if (TryReadAirlock(e, out var info))
                yield return info;
        }
    }

    private static bool TryGetProp(Dictionary<string, JsonElement>? props, string key, out JsonElement el)
    {
        el = default;
        if (props is null)
            return false;
        if (props.TryGetValue(key, out el))
            return true;
        foreach (var kv in props)
        {
            if (string.Equals(kv.Key, key, StringComparison.OrdinalIgnoreCase))
            {
                el = kv.Value;
                return true;
            }
        }

        return false;
    }

    private static string GetString(Dictionary<string, JsonElement>? props, string key, string fallback)
    {
        if (!TryGetProp(props, key, out var el))
            return fallback;
        return el.ValueKind == JsonValueKind.String ? el.GetString() ?? fallback : fallback;
    }

    private static float GetFloat(Dictionary<string, JsonElement>? props, string key, float fallback)
    {
        if (!TryGetProp(props, key, out var el))
            return fallback;
        return el.ValueKind switch
        {
            JsonValueKind.Number => el.GetSingle(),
            JsonValueKind.String when float.TryParse(el.GetString(), out var v) => v,
            _ => fallback,
        };
    }

    private static bool GetBool(Dictionary<string, JsonElement>? props, string key, bool defaultValue)
    {
        if (!TryGetProp(props, key, out var el))
            return defaultValue;
        return el.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.String when bool.TryParse(el.GetString(), out var b) => b,
            _ => defaultValue,
        };
    }

    private static bool TryGetGuid(Dictionary<string, JsonElement>? props, string key, out Guid id)
    {
        id = default;
        if (!TryGetProp(props, key, out var el))
            return false;
        var s = el.ValueKind == JsonValueKind.String ? el.GetString() : el.ToString();
        return Guid.TryParse(s, out id);
    }

    private static IReadOnlyList<Guid> GetGuidList(Dictionary<string, JsonElement>? props, string key)
    {
        if (!TryGetProp(props, key, out var el) || el.ValueKind != JsonValueKind.Array)
            return Array.Empty<Guid>();
        var list = new List<Guid>();
        foreach (var item in el.EnumerateArray())
        {
            var s = item.ValueKind == JsonValueKind.String ? item.GetString() : item.ToString();
            if (Guid.TryParse(s, out var g))
                list.Add(g);
        }

        return list;
    }
}
