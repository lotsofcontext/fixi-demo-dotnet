using GMVM.EnergyTracker.Domain.Models;
using GMVM.EnergyTracker.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GMVM.EnergyTracker.Api.Controllers;

/// <summary>
/// CRUD de usuarios. Requiere autenticacion JWT.
/// Sirve como ejemplo del patron correcto de autorizacion en el sistema.
/// </summary>
[ApiController]
[Route("api/usuarios")]
[Authorize]
public class UsuariosController : ControllerBase
{
    private readonly EnergyTrackerDbContext _db;

    public UsuariosController(EnergyTrackerDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<Usuario>>> Listar()
    {
        var usuarios = await _db.Usuarios
            .Select(u => new Usuario
            {
                Id = u.Id,
                Email = u.Email,
                Rol = u.Rol,
                CreadoEn = u.CreadoEn
                // Nota: PasswordHash NO se devuelve.
            })
            .ToListAsync();
        return Ok(usuarios);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Usuario>> ObtenerPorId(int id)
    {
        var usuario = await _db.Usuarios.FindAsync(id);
        if (usuario is null)
        {
            return NotFound();
        }
        usuario.PasswordHash = string.Empty;
        return Ok(usuario);
    }
}
