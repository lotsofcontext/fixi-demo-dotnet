using GMVM.EnergyTracker.Domain.Dtos;
using GMVM.EnergyTracker.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace GMVM.EnergyTracker.Infrastructure.Services;

/// <summary>
/// Implementacion de <see cref="IMedidorService"/> con EF Core.
/// </summary>
public class MedidorService : IMedidorService
{
    private readonly EnergyTrackerDbContext _db;

    public MedidorService(EnergyTrackerDbContext db)
    {
        _db = db;
    }

    public async Task<List<MedidorListItem>> ListarConResumenAsync()
    {
        // Single query: EF Core translates the correlated subqueries into SQL,
        // eliminating the N+1 pattern (WI-102).
        var resultado = await _db.Medidores
            .Select(m => new MedidorListItem
            {
                Id = m.Id,
                Serial = m.Serial,
                Ubicacion = m.Ubicacion,
                ClienteId = m.ClienteId,
                FechaUltimaLectura = m.Lecturas
                    .OrderByDescending(l => l.FechaLectura)
                    .Select(l => (DateTime?)l.FechaLectura)
                    .FirstOrDefault(),
                ValorUltimaLectura = m.Lecturas
                    .OrderByDescending(l => l.FechaLectura)
                    .Select(l => (decimal?)l.ValorKwh)
                    .FirstOrDefault()
            })
            .ToListAsync();

        return resultado;
    }
}
