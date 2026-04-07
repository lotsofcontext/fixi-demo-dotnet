# WI-101: [BUG] Error 500 al registrar lectura en el mismo dia

| Campo          | Valor                                         |
| -------------- | --------------------------------------------- |
| Work Item ID   | 101                                           |
| Type           | Bug                                           |
| State          | Active                                        |
| Priority       | 1 - Alta                                      |
| Severity       | 2 - High                                      |
| Area Path      | EnergyTracker\Dominio\Calculo                 |
| Iteration Path | EnergyTracker\Sprint 12                       |
| Reported by    | Camilo Restrepo (Soporte N2)                  |
| Assigned to    | Unassigned                                    |
| Reported on    | 2026-04-05 05:42 UTC-5                        |
| Environment    | QA / Prod (reportado por cliente)             |
| Cliente        | ISAGEN                                        |

## Descripcion

El endpoint `POST /api/lecturas` esta devolviendo un `HTTP 500 Internal Server Error` cuando un operador de campo intenta registrar una segunda lectura para el mismo medidor en el mismo dia. La primera lectura se guarda correctamente, pero al crear la siguiente lectura (con misma `FechaLectura` que la anterior) el servicio truena con una `DivideByZeroException` en la capa de dominio.

El caso fue reportado por ISAGEN en la madrugada del 2026-04-05, cuando un tecnico de campo debio hacer una revision urgente post-tormenta y necesitaba capturar una nueva lectura del medidor tras el restablecimiento del servicio. La operacion es completamente valida desde el punto de vista del negocio (un mismo medidor puede tener multiples lecturas en un dia), pero el sistema no la soporta.

## Stack trace observado

```
System.DivideByZeroException: Attempted to divide by zero.
   at EnergyTracker.Dominio.Calculo.CalculadoraConsumo.Calcular(Lectura previa, Lectura actual) in /app/src/Dominio/Calculo/CalculadoraConsumo.cs:line 27
   at EnergyTracker.Application.Lecturas.RegistrarLecturaHandler.Handle(RegistrarLecturaCommand request, CancellationToken ct) in /app/src/Application/Lecturas/RegistrarLecturaHandler.cs:line 54
   at MediatR.Pipeline.RequestHandlerWrapperImpl`2.Handle(IRequest`1 request, CancellationToken cancellationToken, ServiceFactory serviceFactory)
   at EnergyTracker.Api.Controllers.LecturasController.Crear(LecturaDto dto) in /app/src/Api/Controllers/LecturasController.cs:line 38
   at Microsoft.AspNetCore.Mvc.Infrastructure.ActionMethodExecutor.TaskOfIActionResultExecutor.Execute(...)
   at Microsoft.AspNetCore.Mvc.Infrastructure.ControllerActionInvoker.<InvokeActionMethodAsync>g__Awaited|12_0(...)
```

Correlation ID observado en Application Insights: `c7f1a9e2-4b08-4d6b-9c3d-1f8e2a77b011`.

## Pasos para reproducir

1. Autenticarse y obtener token JWT con rol `Operador`.
2. Registrar una primera lectura para el medidor `MED-0001`:
   ```http
   POST /api/lecturas
   Authorization: Bearer <TOKEN>
   Content-Type: application/json

   {
     "medidorId": 1,
     "valorKwh": 12450.5,
     "fechaLectura": "2026-04-05T08:00:00-05:00"
   }
   ```
   Respuesta esperada: `201 Created`.
2. Registrar una segunda lectura para el **mismo medidor** en el **mismo dia**:
   ```http
   POST /api/lecturas
   Authorization: Bearer <TOKEN>
   Content-Type: application/json

   {
     "medidorId": 1,
     "valorKwh": 12460.0,
     "fechaLectura": "2026-04-05T14:30:00-05:00"
   }
   ```
3. Observar respuesta `500 Internal Server Error` con el stack trace arriba.

## Comportamiento esperado

- La segunda lectura del dia debe guardarse exitosamente (`201 Created`).
- El calculo de consumo entre dos lecturas del mismo dia debe usar horas (o una escala adecuada) en lugar de dias enteros, evitando division por cero.
- Si por regla de negocio el consumo diario no aplica cuando la diferencia de tiempo es menor a 24 horas, el servicio debe devolver el consumo acumulado sin calcular promedio diario, pero **nunca** tirar una excepcion.

## Acceptance Criteria

- [ ] `CalculadoraConsumo.Calcular` no lanza `DivideByZeroException` cuando `previa.FechaLectura.Date == actual.FechaLectura.Date`.
- [ ] Se agrega un test unitario en `tests/Dominio.Tests/CalculadoraConsumoTests.cs` que reproduce el escenario de dos lecturas en el mismo dia y valida el resultado.
- [ ] Se agrega un test de integracion en `tests/Api.Tests/LecturasControllerTests.cs` que hace dos `POST /api/lecturas` consecutivos con la misma fecha y valida que ambos devuelvan `201 Created`.
- [ ] El calculo de consumo para lecturas en el mismo dia devuelve un valor finito (no `NaN`, no `Infinity`, no excepcion).
- [ ] No se rompen los tests existentes del modulo `Dominio.Calculo`.
- [ ] Se agrega entrada en el CHANGELOG con referencia a este work item.

## Tags

`bug`, `dominio`, `calculo-consumo`, `divide-by-zero`, `isagen`, `hotfix-candidate`, `sprint-12`
