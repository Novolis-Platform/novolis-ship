# Design

Ship domain libraries extend Cad documents with airtight volumes and hatch semantics. See governance `schemas/cad` and the Ship Designer canvas.

Stack: `Cad.Primitives` → `Ship.Primitives` → `Ship.Topology` → `Ship.Validation`. No Avalonia references in this repo.
