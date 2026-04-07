namespace GMVM.EnergyTracker.Domain.Dtos;

/// <summary>
/// DTO para listado de medidores con resumen de su ultima lectura.
/// </summary>
public class MedidorListItem
{
    public int Id { get; set; }
    public string Serial { get; set; } = string.Empty;
    public string Ubicacion { get; set; } = string.Empty;
    public string ClienteId { get; set; } = string.Empty;
    public DateTime? FechaUltimaLectura { get; set; }
    public decimal? ValorUltimaLectura { get; set; }
}
