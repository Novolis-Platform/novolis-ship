namespace Novolis.Ship.Design;

public enum HullGeneratorKind
{
    Box = 0,
    TaperedBox = 1,
    Faceted = 2,
    Cylinder = 3,
    Capsule = 4,
    LoftedSections = 5,
}

public enum LongitudinalKind
{
    Keel = 0,
    Stringer = 1,
    DeckGirder = 2,
    SideLongitudinal = 3,
    CargoDeckGirder = 4,
}

public enum CompartmentKind
{
    General = 0,
    Habitable = 1,
    Cargo = 2,
    Machinery = 3,
    Tank = 4,
    Void = 5,
}

public enum OpeningKind
{
    Door = 0,
    PressureHatch = 1,
    Airlock = 2,
    CargoDoor = 3,
    Viewport = 4,
    LiftOpening = 5,
    LadderOpening = 6,
    ServicePenetration = 7,
    DuctPenetration = 8,
    PipePenetration = 9,
}

public enum CutoutPurpose
{
    Passage = 0,
    Door = 1,
    Hatch = 2,
    CargoDoor = 3,
    Lift = 4,
    Ladder = 5,
    EquipmentEnvelope = 6,
    Duct = 7,
    Pipe = 8,
    Viewport = 9,
}

public enum ShipWorkspaceKind
{
    Plan = 0,
    Model = 1,
    Present = 2,
}
