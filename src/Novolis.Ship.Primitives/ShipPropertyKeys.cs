namespace Novolis.Ship.Primitives;

/// <summary>Well-known property keys stored on entities / documents.</summary>
public static class ShipPropertyKeys
{
    public const string Exterior = "exterior";
    public const string PressureClass = "pressureClass";
    public const string ClearWidth = "clearWidth";
    public const string ClearHeight = "clearHeight";
    public const string SillHeight = "sillHeight";
    public const string AirtightWhenClosed = "airtightWhenClosed";
    public const string LeafState = "leafState";
    public const string SealAssist = "sealAssist";
    public const string HingeBias = "hingeBias";
    public const string SealFace = "sealFace";
    /// <summary>JSON string array of space names the opening connects, e.g. <c>["CORR_P","HOLD"]</c>.</summary>
    public const string Connects = "connects";
    /// <summary><c>standardHatch</c> or <c>vacuumHatch</c>.</summary>
    public const string HatchClass = "hatchClass";
    public const string AtmosphereClass = "atmosphereClass";
    public const string PressureKPa = "pressureKPa";
    public const string MemberSpaceIds = "memberSpaceIds";
    public const string HullEntityIds = "hullEntityIds";
    public const string VestibuleSpaceId = "vestibuleSpaceId";
    public const string OuterOpeningId = "outerOpeningId";
    public const string InnerOpeningId = "innerOpeningId";
    public const string ShipLoaMeters = "shipLoaMeters";
    public const string BeamMeters = "beamMeters";
    public const string HeightMeters = "heightMeters";
    public const string DeckSpacingMeters = "deckSpacingMeters";
    public const string ForwardPerpendicularZ = "forwardPerpendicularZ";
    public const string StructureMaterial = "structure.material";
    public const string StructureBom = "structure.bom";
    public const string StructureMass = "structure.mass";
}
