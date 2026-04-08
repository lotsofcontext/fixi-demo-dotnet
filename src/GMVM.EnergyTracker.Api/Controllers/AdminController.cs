using GMVM.EnergyTracker.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GMVM.EnergyTracker.Api.Controllers;

/// <summary>
/// Operaciones administrativas del sistema.
/// Requiere autenticacion JWT con rol Admin.
/// </summary>
[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly EnergyTrackerDbContext _db;
    private readonly ILogger<AdminController> _logger;

    public AdminController(EnergyTrackerDbContext db, ILogger<AdminController> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Borra TODAS las lecturas historicas del sistema. Operacion destructiva.
    /// </summary>
    [HttpPost("resetear-lecturas")]
    public async Task<IActionResult> ResetearLecturas()
    {
        var afectados = await _db.Lecturas.ExecuteDeleteAsync();
        _logger.LogWarning("Reset de lecturas ejecutado. Filas afectadas: {Afectados}", afectados);
        return Ok(new { message = "Lecturas reseteadas", afectados });
    }

    /// <summary>
    /// Elimina un usuario por ID. Operacion destructiva.
    /// </summary>
    [HttpDelete("usuarios/{id:int}")]
    public async Task<IActionResult> EliminarUsuario(int id)
    {
        var usuario = await _db.Usuarios.FindAsync(id);
        if (usuario is null)
        {
            return NotFound();
        }
        _db.Usuarios.Remove(usuario);
        await _db.SaveChangesAsync();
        _logger.LogWarning("Usuario {Id} eliminado", id);
        return NoContent();
    }
}
