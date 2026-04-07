# WI-102: [PERF] GET /api/medidores tarda >3s con pocos registros

| Campo          | Valor                                         |
| -------------- | --------------------------------------------- |
| Work Item ID   | 102                                           |
| Type           | Bug (Performance)                             |
| State          | Active                                        |
| Priority       | 1 - Alta                                      |
| Severity       | 3 - Medium                                    |
| Area Path      | EnergyTracker\Api\Medidores                   |
| Iteration Path | EnergyTracker\Sprint 12                       |
| Reported by    | Joaris Angulo (Arquitectura)                  |
| Assigned to    | Unassigned                                    |
| Reported on    | 2026-04-04 16:20 UTC-5                        |
| Environment    | QA (detectado en Application Insights)        |
| Cliente        | XM (Operador del Mercado Electrico)           |

## Descripcion

El endpoint `GET /api/medidores` que usa `MedidorService.ListarConResumen()` esta presentando un problema clasico de **N+1 queries**. El metodo primero trae todos los medidores con `await _db.Medidores.ToListAsync()` y luego itera sobre cada uno haciendo una query adicional para obtener la ultima lectura:

```csharp
foreach (var m in medidores)
{
    var ultima = await _db.Lecturas
        .Where(l => l.MedidorId == m.Id)
        .OrderByDescending(l => l.FechaLectura)
        .FirstOrDefaultAsync();
    // ...
}
```

Con los 50 medidores de seed en QA esto genera **~51 queries SQL por request** (1 para listar medidores + 50 para sus ultimas lecturas). En el ambiente de produccion del cliente XM, donde hay aproximadamente **4.000 medidores activos**, el endpoint hace timeout a los 30 segundos y la UI de monitoreo queda colgada.

El problema fue detectado durante una sesion de profiling en QA usando **Application Insights** y el perfilador de dependencias de SQL Server. El dependency map muestra claramente el patron de N+1 con 51 llamadas consecutivas a `dbo.Lecturas`.

## Metrica actual (QA)

| Metrica                 | Valor actual                                  |
| ----------------------- | --------------------------------------------- |
| Medidores en BD (QA)    | 50                                            |
| Queries por request     | ~51 (1 `SELECT` + 50 `SELECT TOP 1`)          |
| Latencia p50            | 1.850 ms                                      |
| Latencia p95            | 3.420 ms                                      |
| Latencia p99            | 4.110 ms                                      |
| CPU del SQL Server      | picos de 70% durante el request               |

## Metrica en produccion (XM)

| Metrica                 | Valor actual                                  |
| ----------------------- | --------------------------------------------- |
| Medidores en BD (prod)  | ~4.000                                        |
| Queries por request     | ~4.001                                        |
| Latencia p95            | **timeout a 30.000 ms** (el endpoint no responde) |
| Impacto usuario         | Dashboard de monitoreo de XM no carga         |

## Metrica objetivo

| Metrica                 | Valor objetivo                                |
| ----------------------- | --------------------------------------------- |
| Queries por request     | **<=2** (una sola query con join/group, o dos con un `IN`) |
| Latencia p95 en QA      | **<300 ms** con los 50 medidores seed         |
| Latencia p95 en prod    | **<1.500 ms** con 4.000 medidores             |
| Timeouts                | 0                                             |

## Pasos para reproducir

1. Levantar la API en QA con el seed de 50 medidores:
   ```bash
   dotnet run --project src/Api
   ```
2. Ejecutar el endpoint y medir con `curl`:
   ```bash
   curl -w "\nTotal: %{time_total}s\n" -o /dev/null -s \
     -H "Authorization: Bearer <TOKEN>" \
     https://localhost:5001/api/medidores
   ```
3. Observar tiempo total >1.8s para solo 50 medidores.
4. Abrir Application Insights → Performance → `GET /api/medidores` → Dependencies. Verificar ~51 llamadas SQL por request.

## Acceptance Criteria

- [ ] `MedidorService.ListarConResumen()` ejecuta **a lo sumo 2 queries SQL** por request, independientemente del numero de medidores.
- [ ] La implementacion usa una sola query con `GroupBy` + `Max(FechaLectura)` + `Join`, o alternativamente un `Select` con subquery proyectada (`Lecturas.OrderByDescending(...).FirstOrDefault()` dentro del `Select`), que EF Core traduzca a SQL.
- [ ] Se agrega un test de integracion que seedea 100 medidores con 10 lecturas cada uno y valida que el request complete en **<500ms** localmente.
- [ ] Se agrega un assertion (via `EF Core` query interceptor o contador manual) que verifica que el numero de queries SQL sea `<=2`.
- [ ] La respuesta JSON del endpoint es identica byte-a-byte a la version anterior (mismo contrato, mismos campos, mismo orden).
- [ ] Se verifica manualmente en Application Insights que p95 en QA baja de 3.4s a <300ms.
- [ ] No se rompen los tests existentes de `MedidorService`.

## Tags

`performance`, `n-plus-one`, `ef-core`, `sql-server`, `xm`, `application-insights`, `sprint-12`
