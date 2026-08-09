# Design

Avalonia-free ship domain — object-first design model, structure generators, topology graphs, and validation.

Published docs: [https://novolis-platform.github.io/.github/novolis-ship/](https://novolis-platform.github.io/.github/novolis-ship/)

## Package placement (baseline)

```text
Novolis.Cad.Primitives
Novolis.Cad.Evaluation

Novolis.3D.Modeling          ← mesh ops façade over Math.Geometry
Novolis.3D.Scene

Novolis.Ship.Primitives
Novolis.Ship.Structure
Novolis.Ship.Topology
Novolis.Ship.Validation
Novolis.Ship.Design          ← Avalonia-free semantic SoT

Novolis.Avalonia.Ship.Design ← PLAN / MODEL / PRESENT interaction
```

## Geometry architecture

Every geometric ship object owns a `CadDocument` (construction intent).

```text
Ship object          what it is
CadDocument          how geometry is constructed
Cad.Evaluation       evaluates construction
Novolis.3D.Modeling  boolean / weld / split on evaluated meshes
Novolis.3D.Scene     presents evaluated meshes
```

CAD is never rendered directly. Rendering always consumes evaluated 3D meshes composed in `Novolis.Avalonia.Ship.Design` (`ShipDesignEvaluator`).

## Authoring SoT

- Format: **`novolis.ship` / `.shipjson`**
- Flat `.cadjson` is import/export via `ShipCadProjector` (Calypso bridge)
- Schema: `novolis-governance/schemas/ship/novolis.ship.schema.json`

## Core rules

1. User designs a ship, not a CAD document.
2. Primary structure (hull → decks → frames → longitudinals → primary bulkheads) is generated before layout.
3. Passages / openings / equipment create derived `StructuralCutout` relationships.
4. Cutouts are not independently authored geometry.
5. Bulkheads are path-based; compartments may share one physical boundary.
6. Validation is continuous on design change.

## Non-goals

- Avalonia or Raylib references in `Novolis.Ship.*`
- Product hosts or LocalAppData paths in this library repo
- Full BREP/NURBS loft (LoftedSections remains a generator stub)
- Duplicating mesh algorithms outside Math.Geometry / `Novolis.3D.Modeling`
