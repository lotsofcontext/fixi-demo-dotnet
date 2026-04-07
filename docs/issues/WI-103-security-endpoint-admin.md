# WI-103: [SECURITY] Endpoints administrativos accesibles sin autenticacion

| Campo          | Valor                                                 |
| -------------- | ----------------------------------------------------- |
| Work Item ID   | 103                                                   |
| Type           | Bug (Security)                                        |
| State          | Active                                                |
| Priority       | 0 - Critica                                           |
| Severity       | 1 - Critical                                          |
| CVSS v3.1      | **9.1 (Critical)** — `AV:N/AC:L/PR:N/UI:N/S:U/C:H/I:H/A:L` |
| OWASP          | A01:2021 — Broken Access Control                      |
| Area Path      | EnergyTracker\Api\Admin                               |
| Iteration Path | EnergyTracker\Sprint 12 (hotfix)                      |
| Reported by    | Jefferson Acevedo (Hiperautomatizacion - Security Review) |
| Assigned to    | Unassigned                                            |
| Reported on    | 2026-04-05 09:15 UTC-5                                |
| Environment    | QA (verificado) / Prod (pendiente confirmar)          |
| Disclosure     | Interno — no se ha reportado externamente             |

## Descripcion

Durante una revision rutinaria de seguridad con **OWASP ZAP** contra el ambiente de QA se detecto que el controlador `AdminController` **NO tiene el atributo `[Authorize(Roles="Admin")]`** (ni ningun otro atributo de autorizacion), mientras que los otros controladores del proyecto (`UsuariosController`, `MedidoresController`, `LecturasController`) si lo tienen correctamente aplicado.

Esto significa que **cualquier usuario anonimo, sin token JWT ni credencial alguna**, puede invocar endpoints administrativos altamente destructivos:

- `POST /api/admin/resetear-lecturas` — borra **TODAS** las lecturas de la base de datos.
- `DELETE /api/admin/usuarios/{id}` — elimina cualquier usuario (incluido el super-admin) por ID.
- `POST /api/admin/recalcular-consumos` — dispara un job de recalculo sobre toda la base.

Esta vulnerabilidad corresponde a **OWASP Top 10 2021 — A01: Broken Access Control**, que es el riesgo numero 1 del ranking OWASP. El vector es trivial de explotar (no requiere autenticacion previa, no requiere interaccion de usuario, no requiere privilegios) y el impacto es de perdida total de datos operacionales.

## Evidencia

Request anonimo (sin cabecera `Authorization`) ejecutado con `curl` contra QA:

```bash
$ curl -i -X POST https://energytracker-qa.globalmvm.co/api/admin/resetear-lecturas

HTTP/1.1 200 OK
Date: Sun, 05 Apr 2026 14:17:02 GMT
Content-Type: application/json; charset=utf-8
Server: Microsoft-IIS/10.0
X-Powered-By: ASP.NET

{
  "ok": true,
  "mensaje": "Lecturas reseteadas exitosamente.",
  "registrosAfectados": 17842
}
```

Comparativa: el mismo request contra `DELETE /api/usuarios/5` (controlador correctamente protegido) devuelve:

```bash
$ curl -i -X DELETE https://energytracker-qa.globalmvm.co/api/usuarios/5

HTTP/1.1 401 Unauthorized
WWW-Authenticate: Bearer
```

Reporte ZAP (extracto):

```
Alert: Authorization Bypass [Confirmed]
Risk: High
Confidence: High
URL: https://energytracker-qa.globalmvm.co/api/admin/resetear-lecturas
Method: POST
Parameter: (none)
Evidence: Anonymous request returned HTTP 200 with business payload.
```

## Impacto

- **Confidencialidad**: Alta. Un atacante puede listar usuarios y metadatos de medidores via los endpoints GET del mismo controlador sin autenticacion.
- **Integridad**: Alta. Puede borrar usuarios, resetear lecturas y disparar recalculos que corrompen el historico de consumo facturado.
- **Disponibilidad**: Media-Alta. El endpoint `resetear-lecturas` es efectivamente un boton de denegacion de servicio para el dashboard de monitoreo.
- **Regulatorio**: Potencial incumplimiento de la **Ley 1581 de 2012** (Habeas Data Colombia) por exposicion de datos personales de usuarios del sistema.

## Acceptance Criteria

- [ ] `AdminController` declara a nivel de clase el atributo `[Authorize(Roles="Admin")]`.
- [ ] Un request anonimo (sin header `Authorization`) a **cualquier** endpoint bajo `/api/admin/*` devuelve `401 Unauthorized`.
- [ ] Un request autenticado con un rol distinto de `Admin` (por ejemplo `Operador` u `Lector`) a **cualquier** endpoint bajo `/api/admin/*` devuelve `403 Forbidden`.
- [ ] Un request autenticado con rol `Admin` preserva el comportamiento funcional actual de cada endpoint (respuestas y efectos identicos a la version anterior).
- [ ] Se agregan tests de integracion en `tests/Api.Tests/Security/AdminAuthorizationTests.cs` que cubren los tres casos (anon → 401, user → 403, admin → 200/204) para **cada** endpoint del controlador.
- [ ] Se agrega un test que valida mediante reflection que `AdminController` tiene el atributo `[Authorize]` con `Roles == "Admin"`, para prevenir regresion futura.
- [ ] Se ejecuta nuevamente OWASP ZAP contra QA y el alert `Authorization Bypass` ya no aparece para `/api/admin/*`.
- [ ] Se documenta el fix en el CHANGELOG bajo una seccion `## Security` con referencia a este work item y al CVSS.
- [ ] El JWT secret usado en los tests es leido desde configuracion/env (`<JWT_SECRET>`) y **nunca** esta hardcodeado en el codigo.
- [ ] Se publica una nota interna al canal `#security-globalmvm` confirmando el cierre del hallazgo.

## Tags

`security`, `owasp-a01`, `broken-access-control`, `critical`, `hotfix`, `authorization`, `admin-controller`, `cvss-9-1`, `sprint-12`
