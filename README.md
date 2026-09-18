<!-- novolis-package-index:start -->
> **GitHub Packages shows this repository README on every package page** (upstream limitation).
> Open the **package README** for install and quick start — embedded in each .nupkg and linked below.

## Published packages

| Package | Install | Package README |
|---------|---------|----------------|
| `Novolis.Ship.Analysis` | `dotnet add package Novolis.Ship.Analysis` | [README](https://github.com/Novolis-Platform/novolis-ship/blob/main/src/Novolis.Ship.Analysis/README.md) |
| `Novolis.Ship.Design` | `dotnet add package Novolis.Ship.Design` | [README](https://github.com/Novolis-Platform/novolis-ship/blob/main/src/Novolis.Ship.Design/README.md) |
| `Novolis.Ship.Primitives` | `dotnet add package Novolis.Ship.Primitives` | [README](https://github.com/Novolis-Platform/novolis-ship/blob/main/src/Novolis.Ship.Primitives/README.md) |
| `Novolis.Ship.Structure` | `dotnet add package Novolis.Ship.Structure` | [README](https://github.com/Novolis-Platform/novolis-ship/blob/main/src/Novolis.Ship.Structure/README.md) |
| `Novolis.Ship.Topology` | `dotnet add package Novolis.Ship.Topology` | [README](https://github.com/Novolis-Platform/novolis-ship/blob/main/src/Novolis.Ship.Topology/README.md) |
| `Novolis.Ship.Validation` | `dotnet add package Novolis.Ship.Validation` | [README](https://github.com/Novolis-Platform/novolis-ship/blob/main/src/Novolis.Ship.Validation/README.md) |

For NuGet.org and Visual Studio, the **embedded** README.md inside each package is authoritative.

<!-- novolis-package-index:end -->

<!-- novolis-marketing:start -->
<p align="center">
  <a href="https://github.com/Novolis-Platform">
    <img src="https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-brand-transparent.svg" width="360" alt="Novolis"/>
  </a>
</p>

<p align="center">
  <img src="https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/banners/novolis-ship.svg" width="100%" alt="novolis-ship"/>
</p>

<p align="center">
  <strong>Ship topology and airtight CAD</strong><br/>
  Avalonia-free ship domain — primitives, topology graphs, and airtight validation for .cadjson.
</p>

<p align="center">
  <a href="https://novolis-platform.github.io/.github/novolis-ship/"><img src="https://img.shields.io/badge/docs-portfolio-0a7ea3" alt="docs"/></a>
  <a href="https://github.com/Novolis-Platform/novolis-ship/actions"><img src="https://img.shields.io/github/actions/workflow/status/Novolis-Platform/novolis-ship/merge.yml?branch=main&label=merge&logo=github" alt="merge"/></a>
  <a href="https://github.com/orgs/Novolis-Platform/packages?repo_name=novolis-ship"><img src="https://img.shields.io/badge/packages-GitHub%20Packages-0a7ea3?logo=nuget" alt="packages"/></a>
  <a href="https://github.com/Novolis-Platform"><img src="https://img.shields.io/badge/org-Novolis--Platform-111827" alt="org"/></a>
</p>

<p align="center">
  <a href="https://novolis-platform.github.io/.github/novolis-ship/">Docs</a>
  ·
  <a href="https://nuget.pkg.github.com/Novolis-Platform/index.json"><code>https://nuget.pkg.github.com/Novolis-Platform/index.json</code></a>
  ·
  <a href="https://github.com/Novolis-Platform/.github/blob/main/profile/README.md">Org landing</a>
  ·
  <a href="https://github.com/Novolis-Platform/novolis-governance">Governance</a>
</p>

---
<!-- novolis-marketing:end -->
# Novolis Ship

Avalonia-free ship CAD domain for Novolis freighters and decked vessels.

| Package | Role |
|---------|------|
| `Novolis.Ship.Primitives` | Pressure volumes, airlocks, hatch property helpers (incl. vacuum-assisted seals) |
| `Novolis.Ship.Topology` | Compartment airtightness graph |
| `Novolis.Ship.Validation` | Clearance / airlock / envelope rules |
| `Novolis.Ship.Structure` | Plate material specs, BOM lines, outer-skin mass rollup |

Product host: **Ship Designer** in `novolis-apps`. UI chrome: `Novolis.Avalonia.Ship`. Cad interchange: `Novolis.Cad.Primitives` + governance schemas.

```powershell
dotnet test d:\novolis\novolis-ship\tests\Novolis.Ship.Unit\Novolis.Ship.Unit.csproj -p:NovolisUseProjectReferences=true
```

