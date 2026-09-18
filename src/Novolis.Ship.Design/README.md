<!-- novolis-pkg-brand:start -->
<p align="center">
  <a href="https://github.com/Novolis-Platform/novolis-ship">
    <img src="https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.svg" width="72" alt="Novolis"/>
  </a>
</p>
<!-- novolis-pkg-brand:end -->

# Novolis.Ship.Design

Object-first ship design model for Novolis.

- `ShipDesign` semantic graph (`.shipjson` / `novolis.ship`)
- Per-object `CadDocument` construction intent
- Structure-first factory (hull, decks, frames, longitudinals, primary bulkheads)
- Structural cutout relationships
Avalonia-free. UI + scene evaluation live in `Novolis.Avalonia.Ship.Design`.

## Install

```bash
dotnet add package Novolis.Ship.Design
```

**Prerequisites:** [.NET 10 SDK](https://dotnet.microsoft.com/download) (net10.0).

