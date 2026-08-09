# Design

Avalonia-free ship domain — primitives, topology graphs, and airtight validation for `.cadjson`.

Published docs: [https://novolis-platform.github.io/.github/novolis-ship/](https://novolis-platform.github.io/.github/novolis-ship/)

## Layer placement

CAD / ship domain DTOs and validation — Avalonia-free; UI chrome lives in `Novolis.Avalonia.*` (`Novolis.Avalonia.Ship`, existing `Novolis.Avalonia.Cad.Ship`).

## Package stack

```text
Novolis.Cad.* (schemas / primitives)
        │
        ▼
Novolis.Ship.Primitives   → ship kinds, metrics, cad document helpers
        │
        ▼
Novolis.Ship.Topology     → airtight volume / hatch graph
        │
        ▼
Novolis.Ship.Validation   → rules + diagnostics
```

Product hosts (Ship Designer) and Avalonia overlays compose these packages; they do not live in this repo.

## Goals

- Keep ship topology and airtight rules independent of UI frameworks.
- Extend `.cadjson` pressure / airlock semantics without forking Cad core.
- Pack `Novolis.Ship.*` to GitHub Packages for apps and Avalonia chrome.

## Non-goals

- Avalonia or Raylib references in `Novolis.Ship.*`.
- Product hosts or LocalAppData paths in this library repo.
- Local NuGet folder feeds.

## Topics

- `dotnet`
- `cad`
- `ship`
- `novolis`
