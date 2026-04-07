using System.Net;
using System.Net.Http.Headers;

namespace GMVM.EnergyTracker.Tests.Integration;

/// <summary>
/// Tests de seguridad para el endpoint <c>/api/admin/*</c>.
/// Los tests <c>ResetearLecturas_*</c> capturan el bug WI-103 (OWASP A01
/// Broken Access Control) — deben FALLAR antes del fix de Fixi y PASAR despues.
/// </summary>
public class AdminEndpointSecurityTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public AdminEndpointSecurityTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

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
    /// REGRESION DE WI-103: el endpoint <c>DELETE /api/admin/usuarios/{id}</c>
    /// tambien debe estar protegido.
    /// </summary>
    [Fact]
    public async Task EliminarUsuario_SinAutenticacion_DebeRetornar401()
    {
        var client = _factory.CreateClient();

        var resp = await client.DeleteAsync("/api/admin/usuarios/1");

        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }
}
