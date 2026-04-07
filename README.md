# GMVM.EnergyTracker — Fixi Demo Sandbox

> ASP.NET Core 9 Web API for energy meter readings, **deliberately seeded with 3 intentional defects** so that [Fixi](https://github.com/lotsofcontext/fixi) can be demonstrated end-to-end against a realistic .NET codebase.

The domain — meter readings for energy distribution — matches GlobalMVM's actual sector and clients (EPM, ISAGEN, XM, Veolia). The defects are seeded across `Domain`, `Infrastructure`, and `Api` to exercise Fixi's classification, root-cause analysis, and PR creation across architectural layers.

**Disponible también en español: [README.es.md](README.es.md)**

---

## ⚠️ Read this first

This repository **ships with broken code on purpose**. There are 3 intentional bugs (one `bug`, one `performance`, one `security`) and a corresponding set of failing tests. Do not "fix" them by hand — that defeats the demo. The whole point is to invoke Fixi and watch it diagnose, branch, fix, test, and PR each one.

The defects are documented as Azure DevOps-style work items in [`docs/issues/`](docs/issues/).

---

## What this is

A small but realistic ASP.NET Core 9 Web API:

- **Domain**: `Medidor`, `Lectura`, `Usuario` entities + `CalculadoraConsumo`, `IMedidorService`
- **Infrastructure**: EF Core 9 + SQLite (zero setup), `EnergyTrackerDbContext`, deterministic seed (50 medidores, 5000 lecturas)
- **Api**: JWT Bearer auth, controllers for `Medidores`, `Usuarios`, `Admin`, Swagger UI
- **Tests**: xUnit unit + integration tests using `WebApplicationFactory<Program>` and an isolated SQLite `:memory:` connection per test class

| Layer | Project |
|-------|---------|
| Web API | `src/GMVM.EnergyTracker.Api` |
| Business logic | `src/GMVM.EnergyTracker.Domain` |
| Data access | `src/GMVM.EnergyTracker.Infrastructure` |
| Tests | `tests/GMVM.EnergyTracker.Tests` |

---

## The 3 seeded defects

| # | Type | Work Item | Where | Acceptance test |
|---|------|-----------|-------|-----------------|
| 1 | `bug` | [WI-101](docs/issues/WI-101-bug-lectura-negativa.md) | `Domain/Services/CalculadoraConsumo.cs` | `CalculadoraConsumoTests.Calcular_DosLecturasMismoDia_NoDebeLanzarExcepcion` |
| 2 | `performance` | [WI-102](docs/issues/WI-102-perf-listado-medidores.md) | `Infrastructure/Services/MedidorService.cs` | `MedidoresEndpointTests.Listar_LatenciaP95_DebeSerMenorA500ms` |
| 3 | `security` | [WI-103](docs/issues/WI-103-security-endpoint-admin.md) | `Api/Controllers/AdminController.cs` | `AdminEndpointSecurityTests.ResetearLecturas_*` (3 tests) |

---

## Prerequisites

- **.NET 9 SDK** (`dotnet --version` should report `9.0.x`)
- A Claude Code session with the [`fix-issue` skill](https://github.com/lotsofcontext/fixi) installed at `.claude/skills/fix-issue/`
- Optional for the Azure DevOps path: `az` CLI with the `azure-devops` extension and a sandbox ADO project

---

## Quick start

```bash
# Clone
git clone https://github.com/lotsofcontext/fixi-demo-dotnet
cd fixi-demo-dotnet

# Restore and build
dotnet restore
dotnet build

# Run the failing test suite — you should see 5 red, 3 green
dotnet test
```

Expected baseline output:

```
Failed!  -  Failed:     5, Passed:     3, Skipped:     0, Total:     8
```

The 5 failures are evidence of the seeded defects. Fixi will turn each one green.

---

## Running Fixi against the seeded issues

Open a Claude Code session in the repo root, then invoke the `fix-issue` skill against each work item:

```text
/fix-issue docs/issues/WI-101-bug-lectura-negativa.md
```

Fixi will:

1. **Safety Gate** (Step 0) — verify it is in `fixi-demo-dotnet`, working tree is clean, and `master` is the current branch
2. **Parse** (Step 1) — extract title, body, priority, and tags from the work item markdown
3. **Classify** (Step 2) — `bug` (high confidence — keywords: "500", "DivideByZero", "exception")
4. **Root cause analysis** (Step 4) — grep `CalculadoraConsumo`, read the file, identify the `.Days` integer division
5. **Branch** (Step 5) — `fix/WI-101-consumo-negativo-mismo-dia` off `master`
6. **Implement** (Step 6) — minimal change: guard for same-day case
7. **Test** (Step 7) — `dotnet test` → 4 failures left (down from 5)
8. **PR** (Step 8) — `gh pr create` with full template (or `az repos pr create` if running against an Azure Repos mirror)
9. **Tracking** (Step 9) — updates the configured tracking destinations

Repeat for `WI-102` (performance) and `WI-103` (security). Note that **WI-103 will force GUIDED mode automatically** because Fixi escalates security issues for human review at every step.

---

## Project structure

```
fixi-demo-dotnet/
├── README.md                     # This file (English)
├── README.es.md                  # Spanish version
├── CLAUDE.md                     # Conventions Fixi reads in Step 0
├── global.json                   # Pins .NET 9 SDK
├── .editorconfig
├── .gitignore
├── GMVM.EnergyTracker.sln
│
├── src/
│   ├── GMVM.EnergyTracker.Api/
│   │   ├── Program.cs            # JWT auth, EF, Swagger, seed on startup
│   │   ├── Controllers/
│   │   │   ├── MedidoresController.cs    # [Authorize] (correct pattern)
│   │   │   ├── UsuariosController.cs     # [Authorize] (correct pattern)
│   │   │   └── AdminController.cs        # NO [Authorize] — WI-103 bug
│   │   ├── appsettings.json
│   │   └── appsettings.Development.json
│   │
│   ├── GMVM.EnergyTracker.Domain/
│   │   ├── Models/
│   │   │   ├── Medidor.cs
│   │   │   ├── Lectura.cs
│   │   │   └── Usuario.cs
│   │   ├── Dtos/
│   │   │   └── MedidorListItem.cs
│   │   └── Services/
│   │       ├── ICalculadoraConsumo.cs
│   │       ├── CalculadoraConsumo.cs    # WI-101 bug lives here
│   │       └── IMedidorService.cs
│   │
│   └── GMVM.EnergyTracker.Infrastructure/
│       ├── EnergyTrackerDbContext.cs
│       ├── Seed/SeedData.cs
│       └── Services/
│           └── MedidorService.cs        # WI-102 N+1 lives here
│
├── tests/
│   └── GMVM.EnergyTracker.Tests/
│       ├── Unit/
│       │   └── CalculadoraConsumoTests.cs       # WI-101 regression
│       └── Integration/
│           ├── TestWebApplicationFactory.cs     # SQLite :memory: setup
│           ├── JwtTokenHelper.cs                # signed JWTs for tests
│           ├── MedidoresEndpointTests.cs        # WI-102 latency guard
│           └── AdminEndpointSecurityTests.cs    # WI-103 security tests
│
└── docs/
    └── issues/
        ├── WI-101-bug-lectura-negativa.md
        ├── WI-102-perf-listado-medidores.md
        └── WI-103-security-endpoint-admin.md
```

---

## Verifying the demo

After Fixi has resolved all 3 work items, you should be able to:

- See 3 PRs in this repo, one per work item, each with a clean diff
- Run `dotnet test` and see 8 passed, 0 failed
- Read the conventional commits in `git log` (`fix:`, `perf:`, `fix:` for security)
- Check that `master` was never touched directly — every change is on a `fix/`, `perf/`, or `security/` branch

---

## License

Proprietary — Lots of Context LLC · 2026
