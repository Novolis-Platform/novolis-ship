namespace Novolis.Ship.Primitives;

/// <summary>Cad entity kind constants for ship documents.</summary>
public static class ShipEntityKinds
{
    public const string PressureVolume = "pressureVolume";
    public const string Airlock = "airlock";
    public const string Space = "space";
    public const string Wall = "wall";
    public const string Opening = "opening";
}

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

/// <summary>Opening leaf open/closed for airtight graph edges.</summary>
public enum ShipLeafState
{
    Closed = 0,
    Open = 1,
}

/// <summary>Pressure rating class for hatches and bulkheads.</summary>
public enum ShipPressureClass
{
    NonPressure = 0,
    Habitable = 1,
    Vacuum = 2,
}

/// <summary>
/// How differential pressure assists hatch sealing.
/// <see cref="PressureAssist"/> seats the leaf harder onto the frame when the outboard side loses pressure.
/// </summary>
public enum ShipSealAssist
{
    None = 0,
    PressureAssist = 1,
}

/// <summary>Which way the leaf swings relative to the pressurized volume.</summary>
public enum ShipHingeBias
{
    Neutral = 0,
    /// <summary>Leaf opens into the higher-pressure (inboard) side.</summary>
    OpensInboard = 1,
}

/// <summary>Which face of the coaming carries the primary seal contact.</summary>
public enum ShipSealFace
{
    Neutral = 0,
    /// <summary>Seal contact on the vacuum / outboard side of the coaming.</summary>
    Outboard = 1,
    Inboard = 2,
}
