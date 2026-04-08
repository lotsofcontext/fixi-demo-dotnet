using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using GMVM.EnergyTracker.Api.Controllers;
using Microsoft.AspNetCore.Authorization;

namespace GMVM.EnergyTracker.Tests.Integration;

/// <summary>
/// Tests de seguridad para el endpoint <c>/api/admin/*</c>.
/// Capturan el bug WI-103 (OWASP A01 Broken Access Control) — deben FALLAR
/// antes del fix de Fixi y PASAR despues.
/// Cubren los 3 escenarios (anon → 401, user → 403, admin → 200/204)
/// para cada endpoint del controlador.
/// </summary>
public class AdminEndpointSecurityTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public AdminEndpointSecurityTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // ================================================================
    // Reflection: validar que el atributo existe (prevencion de regresion)
    // ================================================================

    /// <summary>
    /// Valida mediante reflection que <see cref="AdminController"/> tiene
    /// el atributo <c>[Authorize(Roles = "Admin")]</c> a nivel de clase,
    /// para prevenir regresion futura.
    /// </summary>
    [Fact]
    public void AdminController_DebeTenerAuthorizeConRolAdmin()
    {
        var authorizeAttr = typeof(AdminController)
            .GetCustomAttribute<AuthorizeAttribute>();

        Assert.NotNull(authorizeAttr);
        Assert.Equal("Admin", authorizeAttr.Roles);
    }

    // ================================================================
    // POST /api/admin/resetear-lecturas
    // ================================================================

    /// <summary>
    /// REGRESION DE WI-103: una request anonima a un endpoint admin
    /// debe responder 401 Unauthorized.
    /// </summary>
    [Fact]
    public async Task ResetearLecturas_SinAutenticacion_DebeRetornar401()
    {
        var client = _factory.CreateClient();

        var resp = await client.PostAsync("/api/admin/resetear-lecturas", content: null);

        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    /// <summary>
    /// REGRESION DE WI-103: una request autenticada con rol "User"
    /// (no Admin) debe responder 403 Forbidden.
    /// </summary>
    [Fact]
    public async Task ResetearLecturas_AutenticadoComoUser_DebeRetornar403()
    {
        var client = _factory.CreateClient();
        var token = JwtTokenHelper.GenerateToken(email: "user@gmvm.test", role: "User");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var resp = await client.PostAsync("/api/admin/resetear-lecturas", content: null);

        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    /// <summary>
    /// Una request autenticada con rol "Admin" debe preservar el
    /// comportamiento funcional (200 OK).
    /// </summary>
    [Fact]
    public async Task ResetearLecturas_AutenticadoComoAdmin_DebeRetornar200()
    {
        var client = _factory.CreateClient();
        var token = JwtTokenHelper.GenerateToken(email: "admin@gmvm.test", role: "Admin");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var resp = await client.PostAsync("/api/admin/resetear-lecturas", content: null);

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    // ================================================================
    // DELETE /api/admin/usuarios/{id}
    // ================================================================

    /// <summary>
    /// REGRESION DE WI-103: el endpoint <c>DELETE /api/admin/usuarios/{id}</c>
    /// tambien debe estar protegido contra acceso anonimo.
    /// </summary>
    [Fact]
    public async Task EliminarUsuario_SinAutenticacion_DebeRetornar401()
    {
        var client = _factory.CreateClient();

        var resp = await client.DeleteAsync("/api/admin/usuarios/1");

        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    /// <summary>
    /// El endpoint DELETE con rol "User" debe responder 403 Forbidden.
    /// </summary>
    [Fact]
    public async Task EliminarUsuario_AutenticadoComoUser_DebeRetornar403()
    {
        var client = _factory.CreateClient();
        var token = JwtTokenHelper.GenerateToken(email: "user@gmvm.test", role: "User");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var resp = await client.DeleteAsync("/api/admin/usuarios/999");

        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    /// <summary>
    /// El endpoint DELETE con rol "Admin" debe llegar al controlador
    /// (NotFound para un ID inexistente, NoContent si existe).
    /// </summary>
    [Fact]
    public async Task EliminarUsuario_AutenticadoComoAdmin_DebeAccederAlEndpoint()
    {
        var client = _factory.CreateClient();
        var token = JwtTokenHelper.GenerateToken(email: "admin@gmvm.test", role: "Admin");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // ID 999 no existe en seed data, pero el punto es que pasa autorizacion.
        var resp = await client.DeleteAsync("/api/admin/usuarios/999");

        // NotFound (404) confirma que la request llego al controlador (paso auth).
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }
}
