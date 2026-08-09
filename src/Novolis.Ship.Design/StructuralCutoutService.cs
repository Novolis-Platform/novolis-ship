namespace Novolis.Ship.Design;

/// <summary>Derives and maintains <see cref="StructuralCutout"/> relationships from functional sources.</summary>
public static class StructuralCutoutService
{
    public static ShipDesign Regenerate(ShipDesign design)
    {
        ArgumentNullException.ThrowIfNull(design);
        var cutouts = new List<StructuralCutout>();

        foreach (var passage in design.Passages)
        {
            var source = passage.Id.AsObject();
            foreach (var frame in design.Frames)
            {
                if (PassageIntersectsFrame(passage, frame))
                {
                    cutouts.Add(new StructuralCutout
                    {
                        Id = StructuralCutoutId.New(),
                        SourceId = source,
                        HostId = frame.Id.AsObject(),
                        Purpose = CutoutPurpose.Passage,
                    });
                }
            }

            foreach (var bh in design.Bulkheads)
            {
                cutouts.Add(new StructuralCutout
                {
                    Id = StructuralCutoutId.New(),
                    SourceId = source,
                    HostId = bh.Id.AsObject(),
                    Purpose = CutoutPurpose.Passage,
                });
            }

            if (passage.DeckId is { } deckId)
            {
                // Lift-style vertical passages also cut the deck plate.
                if (string.Equals(passage.ClearanceClass, "lift", StringComparison.OrdinalIgnoreCase))
                {
                    cutouts.Add(new StructuralCutout
                    {
                        Id = StructuralCutoutId.New(),
                        SourceId = source,
                        HostId = deckId.AsObject(),
                        Purpose = CutoutPurpose.Lift,
                    });
                }
            }
        }

        foreach (var opening in design.Openings)
        {
            var purpose = opening.Kind switch
            {
                OpeningKind.Door => CutoutPurpose.Door,
                OpeningKind.PressureHatch => CutoutPurpose.Hatch,
                OpeningKind.CargoDoor => CutoutPurpose.CargoDoor,
                OpeningKind.LiftOpening => CutoutPurpose.Lift,
                OpeningKind.LadderOpening => CutoutPurpose.Ladder,
                OpeningKind.Viewport => CutoutPurpose.Viewport,
                OpeningKind.DuctPenetration => CutoutPurpose.Duct,
                OpeningKind.PipePenetration => CutoutPurpose.Pipe,
                _ => CutoutPurpose.Hatch,
            };
            cutouts.Add(new StructuralCutout
            {
                Id = StructuralCutoutId.New(),
                SourceId = opening.Id.AsObject(),
                HostId = opening.HostId,
                Purpose = purpose,
            });

            if (opening.Kind is OpeningKind.CargoDoor)
            {
                foreach (var frame in design.Frames)
                {
                    cutouts.Add(new StructuralCutout
                    {
                        Id = StructuralCutoutId.New(),
                        SourceId = opening.Id.AsObject(),
                        HostId = frame.Id.AsObject(),
                        Purpose = CutoutPurpose.CargoDoor,
                    });
                }
            }
        }

        foreach (var equip in design.Equipment)
        {
            foreach (var frame in design.Frames)
            {
                cutouts.Add(new StructuralCutout
                {
                    Id = StructuralCutoutId.New(),
                    SourceId = equip.Id.AsObject(),
                    HostId = frame.Id.AsObject(),
                    Purpose = CutoutPurpose.EquipmentEnvelope,
                });
            }
        }

        return design with { Cutouts = cutouts, ModifiedAt = DateTimeOffset.UtcNow.ToString("O") };
    }

    private static bool PassageIntersectsFrame(PassageDesign passage, FrameDesign frame)
    {
        // Station proximity: if any passage box center Z is near the frame station, cut.
        var station = ShipLengths.ToMeters(frame.Station);
        foreach (var e in passage.Geometry.Entities)
        {
            if (e.Center is not { Length: >= 3 })
                continue;
            if (MathF.Abs(e.Center[2] - station) <= ShipLengths.ToMeters(passage.Width) + 0.5f)
                return true;
        }

        return passage.Geometry.Entities.Count > 0;
    }
}
