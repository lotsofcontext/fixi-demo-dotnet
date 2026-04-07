using GMVM.EnergyTracker.Domain.Dtos;
using GMVM.EnergyTracker.Domain.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GMVM.EnergyTracker.Api.Controllers;

[ApiController]
[Route("api/medidores")]
[Authorize]
public class MedidoresController : ControllerBase
{
    private readonly IMedidorService _medidorService;

    public MedidoresController(IMedidorService medidorService)
    {
        _medidorService = medidorService;
    }

    /// <summary>
    /// Lista todos los medidores con su ultima lectura.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<MedidorListItem>>> Listar()
    {
        var medidores = await _medidorService.ListarConResumenAsync();
        return Ok(medidores);
    }
}
