<!-- novolis-pkg-brand:start -->
<p align="center">
  <a href="https://github.com/Novolis-Platform/novolis-ship">
    <img src="https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.svg" width="72" alt="Novolis"/>
  </a>
</p>
<!-- novolis-pkg-brand:end -->

# Novolis.Ship.Analysis

Engineering plausibility analysis for `ShipDesign`. Prevents implausible spacecraft layouts; does **not** certify vessels.

Categories: Mass, CG, Pressure, Structure, Clearance, GravityLoad — each GREEN / YELLOW / RED.

## Install

```bash
dotnet add package Novolis.Ship.Analysis
```

**Prerequisites:** [.NET 10 SDK](https://dotnet.microsoft.com/download). Depends on `Novolis.Ship.Design`.

## Quick start

```csharp
using Novolis.Ship.Analysis;
using Novolis.Ship.Design;

var design = ShipFactory.Create(/* definition */);
var report = ShipAnalyzer.Analyze(design);
Console.WriteLine(report.StatusOf(AnalysisCategory.Structure));
```

## Related

| Package | Notes |
|---|---|
| `Novolis.Ship.Design` | Semantic SoT |
| `Novolis.Ship.Validation` | Correctness rules (separate from analysis) |
| `Novolis.Avalonia.Ship.Design` | ANALYZE UX + status strip |
