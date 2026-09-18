# Design

Avalonia-free spacecraft domain — object-first design model, structure generators, topology, validation, and engineering plausibility analysis.

Published docs: [https://novolis-platform.github.io/.github/novolis-ship/](https://novolis-platform.github.io/.github/novolis-ship/)

## Package placement (product)

```text
Novolis.Math.Geometry
Novolis.ThreeD.Scene

Novolis.Cad.Primitives
Novolis.Cad.Evaluation

Novolis.Ship.Primitives
Novolis.Ship.Structure
Novolis.Ship.Topology
Novolis.Ship.Validation
Novolis.Ship.Design
Novolis.Ship.Analysis

Novolis.Avalonia.Ship.Design
```

- **Design** — semantic SoT (`.shipjson` v2): environment, load cases, per-object `CadDocument`s, cutout relationships
- **Analysis** — continuous GREEN/YELLOW/RED plausibility (mass, CG, pressure, structure, clearance, gravity loads)
- **Validation** — correctness rules (hosts, clearances, topology errors)

## Geometry architecture

```text
Ship object → CadDocument → Cad.Evaluation → Math.Geometry → Mesh → Analysis + ThreeD.Scene
```

CAD is never rendered directly. Persist intent only; regenerate meshes and analysis caches.

## Non-goals

- Avalonia in `Novolis.Ship.*`
- FEM certification / full CFD
- Local NuGet folder feeds
