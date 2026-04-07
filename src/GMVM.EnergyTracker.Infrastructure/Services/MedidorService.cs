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
        var medidores = await _db.Medidores.ToListAsync();
        var resultado = new List<MedidorListItem>(medidores.Count);

        foreach (var medidor in medidores)
        {
            // Obtener la ultima lectura de este medidor.
            var ultimaLectura = await _db.Lecturas
                .Where(l => l.MedidorId == medidor.Id)
                .OrderByDescending(l => l.FechaLectura)
                .FirstOrDefaultAsync();

            resultado.Add(new MedidorListItem
            {
                Id = medidor.Id,
                Serial = medidor.Serial,
                Ubicacion = medidor.Ubicacion,
                ClienteId = medidor.ClienteId,
                FechaUltimaLectura = ultimaLectura?.FechaLectura,
                ValorUltimaLectura = ultimaLectura?.ValorKwh
            });
        }

        return resultado;
    }
}
