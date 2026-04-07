namespace GMVM.EnergyTracker.Domain.Models;

/// <summary>
/// Lectura puntual del consumo de un medidor en un momento dado.
/// </summary>
public class Lectura
{
    public int Id { get; set; }

    public int MedidorId { get; set; }

    /// <summary>
    /// Fecha y hora exactas en que se tomo la lectura.
    /// Importante: puede haber multiples lecturas el mismo dia (revisiones de urgencia).
    /// </summary>
    public DateTime FechaLectura { get; set; }

    /// <summary>
    /// Valor acumulado del medidor en kWh al momento de la lectura.
    /// </summary>
    public decimal ValorKwh { get; set; }

    /// <summary>
    /// Consumo calculado entre esta lectura y la anterior, en kWh.
    /// Se calcula via <see cref="Services.ICalculadoraConsumo"/>.
    /// </summary>
    public decimal ConsumoCalculado { get; set; }

    public Medidor? Medidor { get; set; }
}
