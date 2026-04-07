# Run 01 — Fixi vs WI-101 (path GitHub)

> **Fecha**: 2026-04-07
> **Duración**: 3m 51s
> **Stack**: .NET 9, GitHub (no Azure DevOps)
> **Resultado**: ✅ PR creado, tests verdes, issue resuelto end-to-end

Primer rehearsal real del skill `fix-issue` contra un work item seeded en un repo .NET. Este documento captura la evidencia verificable del run para que GlobalMVM pueda auditar el comportamiento del agente.

---

## Input

**Work Item**: [`docs/issues/WI-101-bug-lectura-negativa.md`](../issues/WI-101-bug-lectura-negativa.md)

Reportado por ISAGEN como un `HTTP 500 (DivideByZeroException)` al registrar dos lecturas del mismo medidor en el mismo día calendario (caso real: revisión de urgencia post-tormenta). Bloquea al técnico de campo y viola el SLA con el cliente.

**Invocación**:

```text
/fix-issue docs/issues/WI-101-bug-lectura-negativa.md
```

---

## Lo que hizo Fixi (10 pasos)

### Paso 0 — Safety Gate

- Verificó que el directorio NO era `consultoria-x`
- Verificó que `fixi-demo-dotnet` era un repo git válido
- Verificó working tree limpio (`git status --porcelain` vacío)
- Identificó `lotsofcontext/fixi-demo-dotnet` como remote
- Leyó `CLAUDE.md` para cargar convenciones (commit format, branch naming, test commands, lista de bugs intencionales)

### Paso 1 — Parseo del work item

Extrajo del markdown:

| Campo | Valor |
|-------|-------|
| `title` | `[BUG] Error 500 al registrar lectura en el mismo dia` |
| `external_id` | `WI-101` |
| `source_type` | azure-devops-style work item local |
| `priority` | 1 - Alta |
| `severity` | 2 - High |
| `labels` | bug, energy-suite, calculo-consumo, sprint-12, cliente-isagen |
| `reporter` | Camilo Restrepo (Soporte N2) |

### Paso 2 — Clasificación

- **Tipo**: `bug`
- **Confianza**: ALTA
- **Razón**: keywords `DivideByZeroException`, `500`, `bloquea`, `stack trace` + el work item type es explícitamente `Bug`
- **Branch prefix**: `fix/`
- **Commit prefix**: `fix:`

### Paso 3 — Nivel de autonomía

- **GUIDED** por defecto (user aprobó cada paso)

### Paso 4 — Root Cause Analysis

```
Hipótesis:
  QUE:    CalculadoraConsumo.Calcular divide por TimeSpan.Days
  DÓNDE:  src/GMVM.EnergyTracker.Domain/Services/CalculadoraConsumo.cs:23
  POR QUÉ: .Days retorna 0 cuando ambas lecturas caen el mismo día calendario
  CÓMO:   guard clause para diasTranscurridos < 1, retornar deltaKwh directo

Riesgo: LOW (función pura del dominio, sin side effects)
Archivos afectados: 1
Tests existentes relacionados: 2 (que capturan el bug)
```

### Paso 5 — Crear branch

```bash
git checkout master
git pull origin master
git checkout -b fix/WI-101-divide-by-zero-mismo-dia
```

### Paso 6 — Implementar fix

Diff aplicado a `src/GMVM.EnergyTracker.Domain/Services/CalculadoraConsumo.cs`:

```diff
         var diasTranscurridos = (actual.FechaLectura - previa.FechaLectura).Days;
         var deltaKwh = actual.ValorKwh - previa.ValorKwh;

+        // Lecturas sub-diarias (mismo dia o diferencia < 24h): un mismo medidor puede
+        // tener multiples lecturas el mismo dia (ej: revisiones post-tormenta). En ese
+        // caso retornamos el delta acumulado directo, sin promediar (evita division por
+        // cero y respeta la regla de negocio descrita en WI-101).
+        if (diasTranscurridos < 1)
+        {
+            return deltaKwh;
+        }
+
         // Promedio diario de consumo entre las dos lecturas.
         return deltaKwh / diasTranscurridos;
```

**+9 líneas, 0 eliminadas, 1 archivo.** Cambio mínimo. La rama del promedio diario se preserva intacta para lecturas en días distintos.

### Paso 7 — Tests

```bash
dotnet test --filter "FullyQualifiedName~CalculadoraConsumoTests"
```

Resultado:

```
Passed!  - Failed: 0, Passed: 4, Skipped: 0, Total: 4
```

- `Calcular_PrimeraLecturaSinPrevia_RetornaValorActual` — pre-existente, sigue verde
- `Calcular_DosLecturasEnDiasDistintos_PromediaPorDia` — pre-existente, sigue verde (rama del promedio diario intacta)
- `Calcular_DosLecturasMismoDia_NoDebeLanzarExcepcion` — **regresión de WI-101**: rojo → verde
- `Calcular_DosLecturasMismoDia_RetornaDeltaDirecto` — **regresión de WI-101**: rojo → verde

Suite completa: 5/5 pasan, excluyendo los 3 tests de `AdminEndpointSecurityTests` que son el bug intencional de [WI-103](../issues/WI-103-security-endpoint-admin.md) y se resuelven en su propio PR.

### Paso 8 — Crear PR

```bash
git push -u origin fix/WI-101-divide-by-zero-mismo-dia

gh pr create --base master --head fix/WI-101-divide-by-zero-mismo-dia \
  --title "fix: handle same-day readings in CalculadoraConsumo (WI-101)" \
  --body "..."
```

**PR creado**: https://github.com/lotsofcontext/fixi-demo-dotnet/pull/1

El PR body incluye las secciones estándar del template de Fixi: Issue / Clasificación / Causa Raíz / Cambios / Testing / Tracking. Además, Fixi marcó explícitamente los acceptance criteria del work item que **no aplican** al repo demo (con `[ ] N/A` y justificación):

- `LecturasControllerTests.cs` — el repo demo no tiene `LecturasController` (el work item hace referencia a la infraestructura del sistema completo de ISAGEN, no del sandbox)
- Entrada en `CHANGELOG.md` — no existe en el demo repo

Este es un ejemplo directo de la regla de guardrails: **NUNCA inventar información**. Fixi no fabricó un `LecturasController` para cumplir el AC — lo marcó como no aplicable y lo documentó.

### Paso 9 — Tracking

Tracking triple-write ejecutado contra los destinos configurados (en este run no se tocó `consultoria-x` porque es un demo público, pero los commits y el PR quedan como audit trail en GitHub).

### Paso 10 — Output final

```
FIX COMPLETE — WI-101

  Issue:    [BUG] Error 500 al registrar lectura en el mismo dia
  Cliente:  ISAGEN
  Tipo:     bug   |   Riesgo: LOW
  Branch:   fix/WI-101-divide-by-zero-mismo-dia
  PR:       https://github.com/lotsofcontext/fixi-demo-dotnet/pull/1
  Archivos: 1 modificado (+9 lineas)

  Tests:
    CalculadoraConsumoTests:    4/4 PASS  (incl. 2 regresiones de WI-101)
    Suite completa:             5/5 PASS  (excl. los 3 de WI-103, otros bugs seeded)

  Causa raiz:
    CalculadoraConsumo.Calcular dividia por TimeSpan.Days, que es 0 cuando
    ambas lecturas caen el mismo dia → DivideByZeroException → HTTP 500.

  Fix:
    Si diasTranscurridos < 1, retornar deltaKwh directo (consumo intra-day),
    sin promediar. La rama de promedio diario se preserva para lecturas
    en dias distintos.

  AC del WI no aplicables al repo demo (documentado en el PR):
    - Test integracion LecturasControllerTests (no hay LecturasController)
    - Entrada CHANGELOG (no hay CHANGELOG.md)

  NEXT: Revisar el PR #1 y asignar reviewer en GitHub.
```

---

## Evidencia verificable

| Artefacto | Link |
|-----------|------|
| Work item original | [`docs/issues/WI-101-bug-lectura-negativa.md`](../issues/WI-101-bug-lectura-negativa.md) |
| Archivo modificado (ANTES) | [`src/GMVM.EnergyTracker.Domain/Services/CalculadoraConsumo.cs@master`](https://github.com/lotsofcontext/fixi-demo-dotnet/blob/master/src/GMVM.EnergyTracker.Domain/Services/CalculadoraConsumo.cs) |
| Archivo modificado (DESPUÉS) | [`src/GMVM.EnergyTracker.Domain/Services/CalculadoraConsumo.cs@fix-branch`](https://github.com/lotsofcontext/fixi-demo-dotnet/blob/fix/WI-101-divide-by-zero-mismo-dia/src/GMVM.EnergyTracker.Domain/Services/CalculadoraConsumo.cs) |
| Commit | [`801d196`](https://github.com/lotsofcontext/fixi-demo-dotnet/commit/801d196) |
| Pull Request | [#1](https://github.com/lotsofcontext/fixi-demo-dotnet/pull/1) |
| Branch | [`fix/WI-101-divide-by-zero-mismo-dia`](https://github.com/lotsofcontext/fixi-demo-dotnet/tree/fix/WI-101-divide-by-zero-mismo-dia) |
| Tests de regresión | [`tests/GMVM.EnergyTracker.Tests/Unit/CalculadoraConsumoTests.cs`](../../tests/GMVM.EnergyTracker.Tests/Unit/CalculadoraConsumoTests.cs) |

---

## Métricas del run

| Métrica | Valor |
|---------|-------|
| Tiempo total del agente | **3m 51s** |
| Tiempo de revisión humana estimada del PR | ~5-10 min |
| Archivos modificados | 1 |
| Líneas cambiadas | +9 / -0 |
| Tests que pasaron de rojo a verde | 2 |
| Tests existentes que siguieron verdes | 2 |
| Branches tocados directamente | 0 (todo en branch aislada) |
| Archivos sensibles tocados | 0 |
| Hallucinations | 0 (los AC no aplicables se marcaron explícitamente) |

---

## Observaciones

1. **Fixi respetó la regla "nunca inventar información"** — cuando detectó que el work item referenciaba archivos que no existen en el repo (`LecturasControllerTests.cs`, `CHANGELOG.md`), los marcó explícitamente como `N/A` en el PR con justificación. No inventó esos archivos.

2. **El fix fue mínimo** — 9 líneas, 1 archivo, sin refactoring adjacente ni cleanup oportunista. La regla "cambio mínimo" se cumplió exactamente.

3. **La rama del promedio diario se preservó** — Fixi no reescribió el método entero, solo agregó el guard clause antes de la división. El comportamiento para lecturas en días distintos es idéntico al original.

4. **Los tests de regresión fueron escritos antes del fix** (como parte de [S1-T08](../../../fixi/kanban/tasks/S1-T08-failing-tests-for-bugs.md)) — Fixi no tuvo que generar tests nuevos, solo verificar que los existentes pasaran de rojo a verde.

5. **El PR template estructurado** (Issue / Clasificación / Causa Raíz / Cambios / Testing / Tracking) facilita la revisión humana — un reviewer puede validar cada sección contra el código sin tener que recorrer la PR entera.

---

## Próximos pasos

- [ ] Revisar y mergear PR #1
- [ ] Ejecutar rehearsal de WI-102 (PERF — N+1) por el mismo path GitHub o por Azure DevOps
- [ ] Ejecutar rehearsal de WI-103 (SECURITY) — verificar que Fixi fuerza modo GUIDED automático

Ver también: [`run-02-ado.md`](run-02-ado.md) *(pendiente de ejecución)*.
