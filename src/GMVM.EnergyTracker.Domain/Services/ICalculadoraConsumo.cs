using GMVM.EnergyTracker.Domain.Models;

namespace GMVM.EnergyTracker.Domain.Services;

/// <summary>
/// Calcula el consumo en kWh entre dos lecturas consecutivas de un mismo medidor.
/// </summary>
public interface ICalculadoraConsumo
{
    /// <summary>
    /// Calcula el consumo en kWh entre la lectura previa y la actual.
    /// </summary>
    /// <param name="previa">Lectura anterior (puede ser null si es la primera del medidor).</param>
    /// <param name="actual">Lectura actual recien tomada.</param>
    /// <returns>Consumo en kWh.</returns>
    decimal Calcular(Lectura? previa, Lectura actual);
}
