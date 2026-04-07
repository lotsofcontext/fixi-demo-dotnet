# fixi-demo-dotnet

## Project Context

Demo sandbox for **Fixi** — an autonomous issue resolution agent. This repo is a deliberately buggy ASP.NET Core 8 Web API that Fixi can analyze, fix, and PR against, to validate its end-to-end workflow.

Domain: **Energy sector meter-reading service** (matches GlobalMVM's real clients — EPM, ISAGEN, XM, Veolia).

## Architecture

- `src/GMVM.EnergyTracker.Api/` — ASP.NET Core Web API (controllers, Program.cs, DI, auth, Swagger)
- `src/GMVM.EnergyTracker.Domain/` — Business logic (services, domain models)
- `src/GMVM.EnergyTracker.Infrastructure/` — EF Core + SQLite, DbContext, migrations, seed
- `tests/GMVM.EnergyTracker.Tests/` — xUnit tests (unit + integration + perf guards + security tests)

Solution file: `GMVM.EnergyTracker.sln`

## Stack

- **.NET 9** SDK (net9.0)
- **EF Core 9** with SQLite (in-file, zero setup)
- **JWT Bearer** authentication
- **Swagger** (Swashbuckle)
- **xUnit** + `Microsoft.AspNetCore.Mvc.Testing` for integration tests

## Conventions

### Language & style
- File-scoped namespaces
- 4-space indentation, CRLF line endings
- PascalCase for public members, camelCase for locals
- Use `var` when type is apparent, explicit type otherwise
- Braces required, even for single-line blocks

### Test commands
```bash
dotnet restore
dotnet build
dotnet test
```

### Branch naming
Conventional commits + external ID:
- `fix/WI-101-short-description`
- `perf/WI-102-short-description`
- `security/WI-103-short-description`
- `feat/WI-nnn-short-description`

### Commit format
```
{type}: {concise description}

{optional longer explanation}

Fixes: WI-{nnn}
```

Types: `fix`, `feat`, `perf`, `refactor`, `docs`, `test`, `chore`.

## Intentional Bugs (Do Not Fix Manually)

This repo **ships with 3 seeded defects** so that Fixi can be demonstrated against them. See `docs/issues/` for the work items:

- `WI-101` — Bug: `DivideByZeroException` in `CalculadoraConsumo.Calcular`
- `WI-102` — Performance: N+1 in `MedidorService.ListarConResumen`
- `WI-103` — Security: `AdminController` missing `[Authorize]`

**Fixi is the one that fixes these**, not a human. Do not pre-emptively correct them — the failing tests are evidence that Fixi's fix is provably correct.

## Running Fixi Against This Repo

From a Claude Code session with the `fix-issue` skill installed:

```
/fix-issue docs/issues/WI-101-bug-lectura-negativa.md
/fix-issue docs/issues/WI-102-perf-listado-medidores.md
/fix-issue docs/issues/WI-103-security-endpoint-admin.md
```

Fixi will:
1. Verify the Safety Gate (Paso 0)
2. Parse the work item
3. Classify the issue type
4. Analyze the codebase to find the root cause
5. Create a branch (`fix/WI-101-...`, `perf/WI-102-...`, `security/WI-103-...`)
6. Implement the minimal fix
7. Run `dotnet test` to verify
8. Create a PR for human review

Security issues (WI-103) automatically force GUIDED mode — Fixi will ask for confirmation before each step.

## Rules for Claude Code

- **DO NOT pre-fix the seeded bugs.** They exist for a reason.
- **DO NOT refactor unrelated code.** Minimum change only.
- **ALWAYS run `dotnet test` before creating a PR.**
- **NEVER commit to `master`** — always create a feature branch.
- **NEVER touch `appsettings.Production.json`, `.env`, or any `*.key` / `*.pfx` file.**
