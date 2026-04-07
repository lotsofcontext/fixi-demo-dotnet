using GMVM.EnergyTracker.Domain.Dtos;

namespace GMVM.EnergyTracker.Domain.Services;

public interface IMedidorService
{
    /// <summary>
    /// Lista todos los medidores con resumen de su ultima lectura.
    /// </summary>
    Task<List<MedidorListItem>> ListarConResumenAsync();
}
