# Novolis.Ship.Design

Object-first ship design model for Novolis.

- `ShipDesign` semantic graph (`.shipjson` / `novolis.ship`)
- Per-object `CadDocument` construction intent
- Structure-first factory (hull, decks, frames, longitudinals, primary bulkheads)
- Structural cutout relationships
- Evaluation to meshes / `SceneDocument` via Cad.Evaluation + Math.Geometry + Cad.SceneBridge

Avalonia-free. UI lives in `Novolis.Avalonia.Ship.Design`.
