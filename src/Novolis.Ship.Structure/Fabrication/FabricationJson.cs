using System.Text.Json;

namespace Novolis.Ship.Structure;

public static class FabricationJson
{
    public static readonly JsonSerializerOptions Options = new() { WriteIndented = true };
}
