# Design

Avalonia-free ship domain — object-first design model, structure generators, topology graphs, and validation.

Published docs: [https://novolis-platform.github.io/.github/novolis-ship/](https://novolis-platform.github.io/.github/novolis-ship/)

## Layer placement

Ship domain DTOs and evaluation are Avalonia-free. UI chrome lives in `Novolis.Avalonia.Ship` / `Novolis.Avalonia.Ship.Design`.

## Package stack

```text
Novolis.Cad.* + Novolis.Math.Geometry / Measure + Novolis.3D.Scene
        │
        ▼
Novolis.Ship.Primitives   → ship kinds, metrics, cad helpers
        │
        ├──► Novolis.Ship.Structure   → plate BOM/mass + primary structure CadDocument builders
        ├──► Novolis.Ship.Topology    → airtight volume / hatch graph
        └──► Novolis.Ship.Validation  → rules + diagnostics
                │
                ▼
        Novolis.Ship.Design           → ShipDesign SoT (.shipjson), factory, cutouts, scene eval
```

Authoring SoT is **`novolis.ship` / `.shipjson`**. Flat `.cadjson` is import/export via `ShipCadProjector`.

Schema: `novolis-governance/schemas/ship/novolis.ship.schema.json`.

## Goals

- Object-first ship design (hull, decks, frames, longitudinals, bulkheads, compartments, passages, openings, equipment).
- Every geometric object owns a `CadDocument` construction graph.
- Structural cutouts are relationships, regenerated from functional sources.
- Evaluation produces meshes / `SceneDocument` without rendering in ship packages.

## Non-goals

- Avalonia or Raylib references in `Novolis.Ship.*`.
- Product hosts or LocalAppData paths in this library repo.
- Local NuGet folder feeds.
- Recreating `Novolis.3D.Modeling` (mesh ops live in Math.Geometry; scene in Novolis.3D.Scene).
