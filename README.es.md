# GMVM.EnergyTracker — Sandbox de Demo de Fixi

> Web API en ASP.NET Core 9 para lecturas de medidores de energia, **sembrado deliberadamente con 3 defectos intencionales** para que [Fixi](https://github.com/lotsofcontext/fixi) pueda demostrarse end-to-end contra un codebase .NET realista.

El dominio — lecturas de medidores para distribucion de energia — matchea el sector y los clientes reales de GlobalMVM (EPM, ISAGEN, XM, Veolia). Los defectos estan repartidos entre `Domain`, `Infrastructure` y `Api` para ejercitar la clasificacion, el analisis de causa raiz y la creacion de PRs de Fixi cruzando capas arquitectonicas.

**Also available in English: [README.md](README.md)**

---

## ⚠️ Lee esto primero

Este repositorio **incluye codigo roto a proposito**. Hay 3 bugs intencionales (uno `bug`, uno `performance`, uno `security`) y un set de tests rojos correspondiente. No los "arregles" a mano — eso rompe el demo. La idea es invocar a Fixi y verlo diagnosticar, ramificar, arreglar, testear y crear el PR para cada uno.

Los defectos estan documentados como work items estilo Azure DevOps en [`docs/issues/`](docs/issues/).

---

## Que es esto

Una Web API en ASP.NET Core 9 pequena pero realista:

- **Domain**: entidades `Medidor`, `Lectura`, `Usuario` + `CalculadoraConsumo`, `IMedidorService`
- **Infrastructure**: EF Core 9 + SQLite (zero setup), `EnergyTrackerDbContext`, seed deterministico (50 medidores, 5000 lecturas)
- **Api**: autenticacion JWT Bearer, controllers para `Medidores`, `Usuarios`, `Admin`, Swagger UI
- **Tests**: tests unitarios y de integracion con xUnit usando `WebApplicationFactory<Program>` y conexion SQLite `:memory:` aislada por test class

| Capa | Project |
|------|---------|
| Web API | `src/GMVM.EnergyTracker.Api` |
| Logica de negocio | `src/GMVM.EnergyTracker.Domain` |
| Acceso a datos | `src/GMVM.EnergyTracker.Infrastructure` |
| Tests | `tests/GMVM.EnergyTracker.Tests` |

---

## Los 3 defectos sembrados

| # | Tipo | Work Item | Donde vive | Test de aceptacion |
|---|------|-----------|------------|--------------------|
| 1 | `bug` | [WI-101](docs/issues/WI-101-bug-lectura-negativa.md) | `Domain/Services/CalculadoraConsumo.cs` | `CalculadoraConsumoTests.Calcular_DosLecturasMismoDia_NoDebeLanzarExcepcion` |
| 2 | `performance` | [WI-102](docs/issues/WI-102-perf-listado-medidores.md) | `Infrastructure/Services/MedidorService.cs` | `MedidoresEndpointTests.Listar_LatenciaP95_DebeSerMenorA500ms` |
| 3 | `security` | [WI-103](docs/issues/WI-103-security-endpoint-admin.md) | `Api/Controllers/AdminController.cs` | `AdminEndpointSecurityTests.ResetearLecturas_*` (3 tests) |

---

## Pre-requisitos

- **.NET 9 SDK** (`dotnet --version` debe reportar `9.0.x`)
- Una sesion de Claude Code con el [skill `fix-issue`](https://github.com/lotsofcontext/fixi) instalado en `.claude/skills/fix-issue/`
- Opcional para el path Azure DevOps: `az` CLI con la extension `azure-devops` y un proyecto ADO de sandbox

---

## Quick start

```bash
# Clonar
git clone https://github.com/lotsofcontext/fixi-demo-dotnet
cd fixi-demo-dotnet

# Restore y build
dotnet restore
dotnet build

# Correr la suite de tests (deberias ver 5 rojos, 3 verdes)
dotnet test
```

Output esperado de baseline:

```
Failed!  -  Failed:     5, Passed:     3, Skipped:     0, Total:     8
```

Los 5 failures son evidencia de los defectos sembrados. Fixi va a poner cada uno en verde.

---

## Corriendo Fixi contra los issues sembrados

Abre una sesion de Claude Code en la raiz del repo, luego invoca el skill `fix-issue` contra cada work item:

```text
/fix-issue docs/issues/WI-101-bug-lectura-negativa.md
```

Fixi va a:

1. **Safety Gate** (Paso 0) — verificar que esta en `fixi-demo-dotnet`, working tree limpio, branch actual `master`
2. **Parsear** (Paso 1) — extraer titulo, body, prioridad y tags del work item markdown
3. **Clasificar** (Paso 2) — `bug` (confianza alta — keywords: "500", "DivideByZero", "exception")
4. **Analisis de causa raiz** (Paso 4) — grep `CalculadoraConsumo`, leer el archivo, identificar la division entera con `.Days`
5. **Branch** (Paso 5) — `fix/WI-101-consumo-negativo-mismo-dia` desde `master`
6. **Implementar** (Paso 6) — cambio minimo: guard clause para el caso del mismo dia
7. **Test** (Paso 7) — `dotnet test` → 4 failures restantes (de 5)
8. **PR** (Paso 8) — `gh pr create` con template completo (o `az repos pr create` si corre contra un mirror en Azure Repos)
9. **Tracking** (Paso 9) — actualiza los destinos de tracking configurados

Repetir para `WI-102` (performance) y `WI-103` (security). Importante: **WI-103 va a forzar el modo GUIDED automaticamente** porque Fixi escala los issues de seguridad a revision humana en cada paso.

---

## Estructura del proyecto

```
fixi-demo-dotnet/
├── README.md                     # En ingles
├── README.es.md                  # Este archivo
├── CLAUDE.md                     # Convenciones que Fixi lee en el Paso 0
├── global.json                   # Pin de .NET 9 SDK
├── .editorconfig
├── .gitignore
├── GMVM.EnergyTracker.sln
│
├── src/
│   ├── GMVM.EnergyTracker.Api/
│   │   ├── Program.cs            # JWT auth, EF, Swagger, seed on startup
│   │   ├── Controllers/
│   │   │   ├── MedidoresController.cs    # [Authorize] (patron correcto)
│   │   │   ├── UsuariosController.cs     # [Authorize] (patron correcto)
│   │   │   └── AdminController.cs        # SIN [Authorize] — bug WI-103
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
│   │       ├── CalculadoraConsumo.cs    # bug WI-101 vive aqui
│   │       └── IMedidorService.cs
│   │
│   └── GMVM.EnergyTracker.Infrastructure/
│       ├── EnergyTrackerDbContext.cs
│       ├── Seed/SeedData.cs
│       └── Services/
│           └── MedidorService.cs        # N+1 WI-102 vive aqui
│
├── tests/
│   └── GMVM.EnergyTracker.Tests/
│       ├── Unit/
│       │   └── CalculadoraConsumoTests.cs       # regresion WI-101
│       └── Integration/
│           ├── TestWebApplicationFactory.cs     # setup SQLite :memory:
│           ├── JwtTokenHelper.cs                # JWTs firmados para tests
│           ├── MedidoresEndpointTests.cs        # latency guard WI-102
│           └── AdminEndpointSecurityTests.cs    # tests de seguridad WI-103
│
└── docs/
    └── issues/
        ├── WI-101-bug-lectura-negativa.md
        ├── WI-102-perf-listado-medidores.md
        └── WI-103-security-endpoint-admin.md
```

---

## Verificando el demo

Despues de que Fixi resuelva los 3 work items, deberias poder:

- Ver 3 PRs en este repo, uno por work item, cada uno con un diff limpio
- Correr `dotnet test` y ver 8 passed, 0 failed
- Leer los conventional commits en `git log` (`fix:`, `perf:`, `fix:` para security)
- Confirmar que `master` nunca fue tocado directamente — todos los cambios estan en branches `fix/`, `perf/` o `security/`

---

## Licencia

Proprietary — Lots of Context LLC · 2026
